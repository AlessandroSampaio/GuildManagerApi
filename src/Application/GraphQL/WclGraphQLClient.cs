using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GuildManagerApi.Infrastructure.Auth;
using System.Text.Json.Serialization;
using GuildManagerApi.Application.DTOs.WclRanking;


namespace GuildManagerApi.Application.GraphQL;

// Response models
public record WclReportResponse(WclReportData ReportData);
public record WclReportData(WclReport Report);
public record WclReport(string Title, long StartTime, long EndTime, WclGuild? Guild, List<WclFight> Fights, WclMasterData MasterData);
public record WclGuild(string Name, WclServer Server);
public record WclServer(string Name, WclRegion Region);
public record WclRegion(string Name);
public record WclFight(
    int Id,
    string Name,
    long StartTime,
    long EndTime,
    bool? Kill,
    int Difficulty);
public record WclMasterData(List<WclActor> Actors);
public record WclActor(int Id, long GameId, string Name, string Type, string? SubType, string? Server);
public record WclPlayerRanking(
    int WclCharacterId,
    string Name,
    string ServerName,
    string ServerRegion,
    string Class,
    string Spec,
    string Role,           // "tanks" | "healers" | "dps"
    double Amount,
    double RankPercent,
    double BracketPercent,
    int TotalParses
);

public interface IWclGraphQLClient
{
    Task<WclReport> GetReportAsync(string reportCode, Guid? userId = null, CancellationToken ct = default);
    Task<Dictionary<int, List<WclPlayerRanking>>> GetRankingsAsync(string reportCode, IEnumerable<int> fightIds, Guid? userId = null, CancellationToken ct = default);
}

public partial class WclGraphQLClient(
        HttpClient httpClient,
        IWclTokenService tokenService,
        IOptions<WclAuthOptions> opts,
        ILogger<WclGraphQLClient> logger) : IWclGraphQLClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IWclTokenService _tokenService = tokenService;
    private readonly WclAuthOptions _opts = opts.Value;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<WclReport> GetReportAsync(string reportCode, Guid? userId = null, CancellationToken ct = default)
    {
        const string query = """
                    query GetReport($code: String!) {
                      reportData {
                        report(code: $code) {
                          title
                          startTime
                          endTime
                          guild {
                            name
                            server { name region { name } }
                          }
                          fights(killType: Kills) {
                            id name startTime endTime kill difficulty
                          }
                          masterData(translate: true) {
                            actors { id gameID server name type subType server }
                          }
                        }
                      }
                    }
                    """;


        var response = await ExecuteQueryAsync<WclReportResponse>(query, new { code = reportCode }, userId, ct);
        return response.ReportData.Report;
    }

    public async Task<Dictionary<int, List<WclPlayerRanking>>> GetRankingsAsync(string reportCode, IEnumerable<int> fightIds, Guid? userId = null, CancellationToken ct = default)
    {
        var fightIdsArray = fightIds.ToArray();
        LogFetchRankings(reportCode, fightIdsArray.Length);

        var results = new Dictionary<int, List<WclPlayerRanking>>();

        // Wcl API: Search ranking for each fight and store in results dictionary
        foreach (var fightId in fightIdsArray)
        {
            try
            {
                // Execute both metric queries in parallel.
                // dpsTask  → Amount = DPS for tanks and dps players
                // hpsTask  → Amount = HPS for healers
                var dpsTask = FetchRankingsRawAsync(reportCode, fightId, "dps", userId, ct);
                var hpsTask = FetchRankingsRawAsync(reportCode, fightId, "hps", userId, ct);

                await Task.WhenAll(dpsTask, hpsTask);

                var dpsFight = dpsTask.Result;
                var hpsFight = hpsTask.Result;

                if (dpsFight is null && hpsFight is null)
                {
                    logger.LogWarning(
                        "Both dps and hps ranking queries returned null for fight {FightId}", fightId);
                    results[fightId] = [];
                    continue;
                }

                // Merge: tanks+dps come from the dps query (correct DPS amount),
                //        healers come from the hps query (correct HPS amount).
                results[fightId] = MergeRoles(dpsFight, hpsFight);

            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not fetch rankings for fight {FightId}", fightId);
                results[fightId] = [];
            }
        }

        return results;
    }

    private async Task<(string endpoint, string token)> ResolveEndpointAndTokenAsync(
        Guid? userId, CancellationToken ct)
    {
        if (userId.HasValue && await _tokenService.IsUserAuthorizedAsync(userId.Value, ct))
        {
            var userToken = await _tokenService.GetUserTokenAsync(userId.Value, ct);
            return (_opts.PrivateGraphQlEndpoint, userToken);
        }

        var appToken = await _tokenService.GetAppTokenAsync(ct);
        return (_opts.PublicGraphQlEndpoint, appToken);
    }


    private async Task<T> ExecuteQueryAsync<T>(object query, object variables, Guid? userId = null, CancellationToken ct = default)
    {
        var (endpoint, token) = await ResolveEndpointAndTokenAsync(userId, ct);
        var payload = JsonSerializer.Serialize(new { query, variables });
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
        };

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        LogWclResponse(json[..Math.Min(json.Length, 200)]);

        var doc = JsonSerializer.Deserialize<JsonElement>(json);

        if (doc.TryGetProperty("errors", out var errors))
            throw new InvalidOperationException($"GraphQL error: {errors.GetRawText()}");

        var data = doc.GetProperty("data");
#pragma warning disable CA1869
        return JsonSerializer.Deserialize<T>(data.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Null response from WCL");
#pragma warning restore CA1869
    }

    private async Task<WclRankingsFight?> FetchRankingsRawAsync(
        string reportCode, int fightId, string metric, Guid? userId, CancellationToken ct)
    {
        // `rankings` é um escalar JSON no schema do WCL — NÃO aceita subcampos.
        // Solicitamos o campo diretamente; o servidor retorna o JSON como string
        // embutida no campo GraphQL escalar, que depois deserializamos manualmente.
        const string query = """
            query GetRankings($code: String!, $fightIds: [Int!]!, $metric: ReportRankingMetricType) {
              reportData {
                report(code: $code) {
                  rankings(fightIDs: $fightIds, playerMetric: $metric)
                }
              }
            }
            """;

        var resp = await ExecuteQueryAsync<JsonElement>(
            query,
            new { code = reportCode, fightIds = new[] { fightId }, metric },
            userId, ct);

        var rankingsElement = resp
            .GetProperty("reportData")
            .GetProperty("report")
            .GetProperty("rankings");

        WclRankingsRoot? root = rankingsElement.ValueKind switch
        {
            // Escalar retornado como string JSON embutida
            JsonValueKind.String => JsonSerializer.Deserialize<WclRankingsRoot>(
                rankingsElement.GetString()!, _jsonOpts),

            // Objeto JSON direto
            JsonValueKind.Object => JsonSerializer.Deserialize<WclRankingsRoot>(
                rankingsElement.GetRawText(), _jsonOpts),

            _ => null
        };

        if (root is null)
        {
            logger.LogWarning(
                "rankings field was null or unexpected kind ({Kind}) for fight {FightId}, metric={Metric}",
                rankingsElement.ValueKind, fightId, metric);
            return null;
        }

        // Localizar o fight correspondente dentro do array `data[]`
        // (a API pode retornar dados de múltiplos fights mesmo quando pedimos um)
        var fightData = root.Data.FirstOrDefault(f => f.FightId == fightId)
                     ?? root.Data.FirstOrDefault();

        if (fightData is null)
        {
            logger.LogWarning(
                "No fight data found in rankings response for fight {FightId}, metric={Metric}",
                fightId, metric);
        }

        return fightData;
    }

    private static List<WclPlayerRanking> MergeRoles(
        WclRankingsFight? dpsFight, WclRankingsFight? hpsFight)
    {
        var result = new List<WclPlayerRanking>();

        if (dpsFight is not null)
        {
            AddGroup(result, dpsFight.Roles.Tanks, "tanks");
            AddGroup(result, dpsFight.Roles.Dps, "dps");
        }

        if (hpsFight is not null)
        {
            AddGroup(result, hpsFight.Roles.Healers, "healers");
        }

        return result;
    }

    private static void AddGroup(
        List<WclPlayerRanking> list,
        WclRoleGroup group,
        string roleName)
    {
        foreach (var c in group.Characters)
        {
            list.Add(new WclPlayerRanking(
                WclCharacterId: c.Id,
                Name: c.Name,
                ServerName: c.Server?.Name ?? string.Empty,
                ServerRegion: c.Server?.Region ?? string.Empty,
                Class: c.Class,
                Spec: c.Spec,
                Role: roleName,
                Amount: c.Amount,
                RankPercent: c.RankPercent,
                BracketPercent: c.BracketPercent,
                TotalParses: c.TotalParses
            ));
        }
    }

    [LoggerMessage(LogLevel.Debug, Message = "WCL response: {Json}")]
    private partial void LogWclResponse(string json);

    [LoggerMessage(LogLevel.Information, Message = "Fetching rankings for report {ReportCode}, {FightIdsCount} fights")]
    private partial void LogFetchRankings(string reportCode, int fightIdsCount);
}
