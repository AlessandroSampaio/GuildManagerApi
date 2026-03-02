using System.Text;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var MyAllowSpecificOrigins = "AllowAll";

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<WclAuthOptions>(
    builder.Configuration.GetSection(WclAuthOptions.Section));

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.Section));

builder.Services.Configure<EncryptionOptions>(
    builder.Configuration.GetSection(EncryptionOptions.Section));


// Context
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    npsql => npsql.EnableRetryOnFailure(3)
        .MigrationsAssembly("GuildManagerApi.Api")
));

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
        ValidIssuer = jwtSection["Issuer"] ?? "WarcraftLogsApi",
        ValidateAudience = true,
        ValidAudience = jwtSection["Audience"] ?? "WarcraftLogsApi",
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
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"));


// HTTP Clients
builder.Services.AddScoped<IWclTokenService, WclTokenService>();
builder.Services.AddHttpClient<IWclTokenService, WclTokenService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddHttpClient<IWclGraphQLClient, WclGraphQLClient>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

// Application Services
builder.Services.AddScoped<IImportReportService, ImportReportService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWclCredentialService, WclCredentialService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IFieldEncryptionService, AesGcmFieldEncryptionService>();

// Repositories
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
builder.Services.AddScoped<IGuildRepository, GuildRepository>();
builder.Services.AddScoped<IPerformanceRepository, PerformanceRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// API
// In-memory cache for OAuth state nonces (anti-CSRF)
builder.Services.AddMemoryCache();

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

    // c.AddSecurityRequirement(new OpenApiSecurityRequirement
    // {
    //     {
    //         new OpenApiSecurityScheme
    //         {
    //             Scheme = new OpenApiSchemaReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    //         },
    //         Array.Empty<string>()
    //     }
    // });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Build
var app = builder.Build();

app.UseCors(MyAllowSpecificOrigins);

// Auto-run migrations on startup
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.MigrateAsync();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/api/v1/docs", "GuildManager API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
