using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GuildManagerApi.Infrastructure.Auth;


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
public record WclActor(int Id, string Name, string Type, string? SubType, string? Server);
public record WclRankingsResponse(WclReportData ReportData);
public record WclRankingsFight(List<WclPlayerRanking> Rankings);
public record WclPlayerRanking(
    string Name,
    string Class,
    string Spec,
    string Role,
    double Amount,
    double? RankPercent,
    int? TotalParses,
    double? BestPercent);


public interface IWclGraphQLClient
{
    Task<WclReport> GetReportAsync(string reportCode, CancellationToken cancellationToken);
    Task<Dictionary<int, List<WclPlayerRanking>>> GetRankingsAsync(string reportCode, IEnumerable<int> fightIds, string metric, CancellationToken ct);
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

    public async Task<WclReport> GetReportAsync(string reportCode, CancellationToken ct)
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
                          fights(killType: All) {
                            id name startTime endTime kill difficulty
                          }
                          masterData(translate: true) {
                            actors { id name type subType server }
                          }
                        }
                      }
                    }
                    """;


        var response = await ExecuteQueryAsync<WclReportResponse>(query, new { code = reportCode }, ct);
        return response.ReportData.Report;
    }

    public async Task<Dictionary<int, List<WclPlayerRanking>>> GetRankingsAsync(string reportCode, IEnumerable<int> fightIds, string metric = "dps", CancellationToken ct = default)
    {
        var fightIdsArray = fightIds.ToArray();
        LogFetchRankings(reportCode, fightIdsArray.Length, metric);

        var results = new Dictionary<int, List<WclPlayerRanking>>();

        // Wcl API: Search ranking for each fight and store in results dictionary
        foreach (var fightId in fightIdsArray)
        {
            const string query = """
                           query GetRankings($code: String!, $fightIds: [Int!]!, $metric: DPSMetric) {
                             reportData {
                               report(code: $code) {
                                 rankings(fightIDs: $fightIds, playerMetric: $metric) {
                                   data {
                                     name class spec role amount rankPercent totalParses bestPercent
                                   }
                                 }
                               }
                             }
                           }
                           """;

            try
            {

                var response = await ExecuteQueryAsync<JsonElement>(query, new
                {
                    code = reportCode,
                    fightIds = new[] { fightId },
                    metric
                }, ct);

                var data = response
                    .GetProperty("reportData")
                    .GetProperty("report")
                    .GetProperty("rankings")
                    .GetProperty("data");

#pragma warning disable CA1869
                var rankings = JsonSerializer.Deserialize<List<WclPlayerRanking>>(data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
#pragma warning restore CA1869

                results[fightId] = rankings;
            }
            catch (Exception ex)
            {
                LogFetchRankingsFail(reportCode, fightId, ex.Message);
                results[fightId] = [];
            }

        }

        throw new NotImplementedException();
    }


    private async Task<T> ExecuteQueryAsync<T>(object query, object variables, CancellationToken ct)
    {
        var token = await _tokenService.GetAccessTokenAsync(ct);
        var payload = JsonSerializer.Serialize(new { query, variables });
        var request = new HttpRequestMessage(HttpMethod.Post, _opts.GraphQlEndpoint)
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

    [LoggerMessage(LogLevel.Debug, Message = "WCL response: {Json}")]
    private partial void LogWclResponse(string json);

    [LoggerMessage(LogLevel.Information, Message = "Fetching rankings for report {ReportCode}, {FightIdsCount} fights, metric {Metric}")]
    private partial void LogFetchRankings(string reportCode, int fightIdsCount, string metric);

    [LoggerMessage(LogLevel.Warning, Message = "Could not fetch rankings for fight {FightId} in report {ReportCode}. Reason : {Message}")]
    private partial void LogFetchRankingsFail(string reportCode, int FightId, string message);
}
