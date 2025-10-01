using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Minio;
using Serilog;
using Serilog.Events;
using SmartUnderwrite.Api.Authorization;
using SmartUnderwrite.Api.Constants;
using SmartUnderwrite.Api.Middleware;
using SmartUnderwrite.Api.Models;
using SmartUnderwrite.Api.Services;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Infrastructure.Data;
using System.Text;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "SmartUnderwrite")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/smartunderwrite-.log", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(Environment.GetCommandLineArgs());

// Add Serilog
builder.Host.UseSerilog();

// Add Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=smartunderwrite;Username=postgres;Password=postgres";

builder.Services.AddDbContext<SmartUnderwriteDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Identity
builder.Services.AddIdentity<User, Role>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<SmartUnderwriteDbContext>()
.AddDefaultTokenProviders();

// Configure JWT settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings not found in configuration");

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute tolerance
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("JWT Authentication failed: {Exception}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Log.Debug("JWT Token validated for user: {User}", context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        }
    };
});

// Add Authorization with policies
builder.Services.AddAuthorization(options =>
{
    // Admin only policy
    options.AddPolicy(Policies.AdminOnly, policy =>
        policy.RequireRole(Roles.Admin));

    // Underwriter or Admin policy
    options.AddPolicy(Policies.UnderwriterOrAdmin, policy =>
        policy.RequireRole(Roles.Admin, Roles.Underwriter));

    // Affiliate access policy (includes data segregation)
    options.AddPolicy(Policies.AffiliateAccess, policy =>
        policy.Requirements.Add(new AffiliateAccessRequirement()));

    // All roles policy
    options.AddPolicy(Policies.AllRoles, policy =>
        policy.RequireRole(Roles.Admin, Roles.Underwriter, Roles.Affiliate));
});

// Register authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, AffiliateAccessHandler>();

// Configure MinIO
builder.Services.AddMinio(configureClient => configureClient
    .WithEndpoint(builder.Configuration.GetValue<string>("Storage:Endpoint") ?? "localhost:9000")
    .WithCredentials(
        builder.Configuration.GetValue<string>("Storage:AccessKey") ?? "minioadmin",
        builder.Configuration.GetValue<string>("Storage:SecretKey") ?? "minioadmin")
    .WithSSL(builder.Configuration.GetValue<bool>("Storage:UseSSL")));

// Register application services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IStorageService, MinioStorageService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDecisionService, DecisionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IReportsService, ReportsService>();

// Register Rules Engine services
builder.Services.AddScoped<SmartUnderwrite.Core.RulesEngine.Interfaces.IRulesEngine, SmartUnderwrite.Core.RulesEngine.Engine.RulesEngine>();
builder.Services.AddScoped<SmartUnderwrite.Core.RulesEngine.Interfaces.IRuleRepository, SmartUnderwrite.Infrastructure.Repositories.RuleRepository>();
builder.Services.AddScoped<SmartUnderwrite.Core.RulesEngine.Interfaces.IRuleVersionRepository, SmartUnderwrite.Infrastructure.Repositories.RuleVersionRepository>();
builder.Services.AddScoped<SmartUnderwrite.Core.RulesEngine.Interfaces.IRuleParser, SmartUnderwrite.Core.RulesEngine.Parsing.RuleParser>();
builder.Services.AddScoped<SmartUnderwrite.Core.RulesEngine.Interfaces.IExpressionCompiler, SmartUnderwrite.Core.RulesEngine.Compilation.ExpressionCompiler>();
builder.Services.AddScoped<SmartUnderwrite.Core.RulesEngine.Interfaces.IRuleService, SmartUnderwrite.Core.RulesEngine.Services.RuleService>();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Swagger UI for development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

// Handle command line arguments for seeding
var commandLineArgs = Environment.GetCommandLineArgs();
if (commandLineArgs.Contains("--seed"))
{
    await SeedDatabaseAsync(app.Services);
    return;
}

// Seed database in development (skip in testing)
if (app.Environment.IsDevelopment())
{
    await SeedDatabaseAsync(app.Services);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add correlation ID middleware first
app.UseMiddleware<CorrelationIdMiddleware>();

// Add audit middleware before authentication
app.UseMiddleware<AuditMiddleware>();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Add Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) => ex != null
        ? LogEventLevel.Error
        : httpContext.Response.StatusCode > 499
            ? LogEventLevel.Error
            : LogEventLevel.Information;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("CorrelationId", httpContext.Items["CorrelationId"]?.ToString() ?? httpContext.TraceIdentifier ?? Guid.NewGuid().ToString());
        diagnosticContext.Set("UserId", httpContext.User?.FindFirst("sub")?.Value ?? "Anonymous");
    };
});

app.MapControllers();

app.Run();

// Ensure to flush and stop internal timers/threads before application-exit
Log.CloseAndFlush();

// Database seeding helper method
static async Task SeedDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SmartUnderwriteDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    
    try
    {
        Log.Information("Starting database migration and seeding...");
        
        // Ensure database is created and migrated
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database creation completed");
        
        // Seed data
        await SmartUnderwrite.Infrastructure.Data.SeedData.SeedAsync(context, userManager, roleManager);
        Log.Information("Database seeded successfully");
        
        // Log seeded data summary
        var affiliateCount = await context.Affiliates.CountAsync();
        var userCount = await context.Users.CountAsync();
        var applicationCount = await context.LoanApplications.CountAsync();
        var ruleCount = await context.Rules.CountAsync();
        
        Log.Information("Seeding Summary: {AffiliateCount} affiliates, {UserCount} users, {ApplicationCount} applications, {RuleCount} rules", 
            affiliateCount, userCount, applicationCount, ruleCount);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while seeding the database");
        throw;
    }
}

// Make Program class accessible for testing
public partial class Program { }