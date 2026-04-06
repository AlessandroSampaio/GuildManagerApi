
using System.Text;
using AspNetCoreRateLimit;
using GuildManagerApi.Api.Middleware;
using GuildManagerApi.Application.Auth;
using GuildManagerApi.Application.GraphQL;
using GuildManagerApi.Application.Services;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Auth;
using GuildManagerApi.Infrastructure.Data;
using GuildManagerApi.Infrastructure.Encryption;
using GuildManagerApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

var logDirectory = OperatingSystem.IsLinux()
    ? "/var/log/GuildManagerApi"
    : Path.Combine(AppContext.BaseDirectory, "logs");

builder.Host.UseSerilog((ctx, _, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
        {
            // Always include errors with full detail
            if (e.Level >= LogEventLevel.Error) return true;
            // Include Information+ only from the sync worker (start/success/periodic)
            if (!e.Properties.TryGetValue("SourceContext", out var sc)) return false;
            return sc.ToString().Contains("RaiderIoSyncWorker")
                && e.Level >= LogEventLevel.Information;
        })
        .WriteTo.File(
            path: Path.Combine(logDirectory, "raiderio-sync-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")));

// Configuration
builder.Services.Configure<WclAuthOptions>(
    builder.Configuration.GetSection(WclAuthOptions.Section));

builder.Services.Configure<BNetAuthOptions>(
    builder.Configuration.GetSection(BNetAuthOptions.Section));
builder.Services.Configure<RaiderIoOptions>(
    builder.Configuration.GetSection(RaiderIoOptions.Section));
builder.Services.Configure<RaiderIoSyncOptions>(
    builder.Configuration.GetSection(RaiderIoSyncOptions.Section));

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.Section));

builder.Services.Configure<EncryptionOptions>(
    builder.Configuration.GetSection(EncryptionOptions.Section));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.Configure<IpRateLimitOptions>(
    builder.Configuration.GetSection("IpRateLimiting"));

builder.Services.Configure<ClientRateLimitOptions>(
    builder.Configuration.GetSection("ClientRateLimiting"));

// Context
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    npsql => npsql.EnableRetryOnFailure(3)
        .MigrationsAssembly("GuildManagerApi.Api")
));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "GuildManagerApi:RateLimit:";
});

// JWT Authentication
var jwtSection = builder.Configuration.GetSection(JwtOptions.Section);
var secretKey = jwtSection["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey is not configured");

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSection["Issuer"] ?? "GuildManagerApi",
        ValidateAudience = true,
        ValidAudience = jwtSection["Audience"] ?? "GuildManagerApi",
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    opt.Events = new JwtBearerEvents
    {
        OnChallenge = ctx =>
        {
            ctx.HandleResponse();
            ctx.Response.StatusCode = 401;
            ctx.Response.ContentType = "application/problem+json";
            return ctx.Response.WriteAsync("{\"status\":401,\"title\":\"Unauthorized\",\"detail\":\"A valid Bearer token is required.\"}");
        },
        OnForbidden = ctx =>
        {
            ctx.Response.StatusCode = 403;
            ctx.Response.ContentType = "application/problem+json";
            return ctx.Response.WriteAsync("{\"status\":403,\"title\":\"Forbidden\",\"detail\":\"You do not have permission to access this resource.\"}");
        }
    };
});


builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"))
    .AddPolicy("MemberOrAdmin", p => p.RequireRole("Member", "Admin"));


// HTTP Clients
builder.Services.AddScoped<IWclTokenService, WclTokenService>();
builder.Services.AddHttpClient<IWclTokenService, WclTokenService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddScoped<IBNetTokenService, BattleNetTokenService>();
builder.Services.AddHttpClient<IBNetTokenService, BattleNetTokenService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddHttpClient<IWclGraphQLClient, WclGraphQLClient>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

// Application Services
builder.Services.AddScoped<IImportReportService, ImportReportService>();
builder.Services.AddScoped<IGuildSyncService, GuildSyncService>();
builder.Services.AddScoped<IRaiderIoSyncService, RaiderIoSyncService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWclCredentialService, WclCredentialService>();
builder.Services.AddScoped<IBattleNetCredentialService, BattleNetCredentialService>();
builder.Services.AddScoped<IRaiderIoCredentialService, RaiderIoCredentialService>();
builder.Services.AddScoped<IRaiderIoProfileRepository, RaiderIoProfileRepository>();
builder.Services.AddScoped<IRaiderIoService, RaiderIoService>();
builder.Services.AddHttpClient<IRaiderIoService, RaiderIoService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));
builder.Services.AddScoped<IScoringSettingsRepository, ScoringSettingsRepository>();
builder.Services.AddScoped<ICoreRepository, CoreRepository>();
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IRaidWeekRepository, RaidWeekRepository>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IFieldEncryptionService, AesGcmFieldEncryptionService>();
builder.Services.AddSingleton<IImportQueue, ImportQueue>();
builder.Services.AddSingleton<ImportProgressHub>();

builder.Services.AddSingleton<IImportProgressHub>(sp =>
    sp.GetRequiredService<ImportProgressHub>());

builder.Services.AddSingleton<IGuildSyncQueue, GuildSyncQueue>();
builder.Services.AddSingleton<GuildSyncHub>();
builder.Services.AddSingleton<RaidWeekHub>();
builder.Services.AddSingleton<IRaiderIoSyncQueue, RaiderIoSyncQueue>();
builder.Services.AddSingleton<RaiderIoSyncHub>();

// Repositories
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
builder.Services.AddScoped<IGuildRepository, GuildRepository>();
builder.Services.AddScoped<IPerformanceRepository, PerformanceRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPenaltyRepository, PenaltyRepository>();

builder.Services.AddHostedService<ImportWorker>();
builder.Services.AddHostedService<GuildSyncWorker>();
builder.Services.AddHostedService<RaiderIoSyncWorker>();

// API
// In-memory cache for OAuth state nonces (anti-CSRF)
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

//Rate Limiting
builder.Services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
builder.Services.AddSingleton<IClientPolicyStore, DistributedCacheClientPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore,
    DistributedCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();



builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "WarcraftLogs Integration API",
        Version = "v1",
        Description = "API para importar e consultar dados de reports do WarcraftLogs. Autenticacao via JWT Bearer."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Insira o token JWT obtido em POST /api/auth/login"
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("TauriClients", policy =>
        {
            policy
                .WithOrigins(
		    "http://localhost:1420",
                    "tauri://localhost",
                    "https://tauri.localhost",
                    "http://tauri.localhost")
                .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
                .WithHeaders("Authorization", "Content-Type", "X-Requested-With")
                .AllowCredentials();
        });
});

// Build
var app = builder.Build();

app.UseForwardedHeaders();
app.UseIpRateLimiting();
app.UseMiddleware<ClientIdInjectionMiddleware>();
app.UseClientRateLimiting();
app.UseCors("TauriClients");

// Auto-run migrations on startup
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.MigrateAsync();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    Console.WriteLine("Development environment detected. Swagger UI will be available at /api/v1/docs");
    app.UseSwagger();
    app.UseSwaggerUI();

}


if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
