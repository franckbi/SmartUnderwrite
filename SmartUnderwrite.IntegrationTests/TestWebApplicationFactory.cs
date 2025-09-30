using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartUnderwrite.Infrastructure.Data;

namespace SmartUnderwrite.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Override configuration to prevent PostgreSQL connection attempts
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft"] = "Warning",
                ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Information"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove ALL database-related services
            var descriptorsToRemove = new List<ServiceDescriptor>();
            
            foreach (var service in services)
            {
                if (service.ServiceType.Name.Contains("DbContext") ||
                    service.ServiceType.Name.Contains("Database") ||
                    service.ServiceType.Name.Contains("Npgsql") ||
                    service.ServiceType == typeof(DbContextOptions<SmartUnderwriteDbContext>) ||
                    service.ServiceType == typeof(SmartUnderwriteDbContext) ||
                    (service.ServiceType.IsGenericType && 
                     service.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)) ||
                    service.ImplementationType?.Name.Contains("DbContext") == true ||
                    service.ImplementationType?.Name.Contains("Database") == true ||
                    service.ImplementationType?.Name.Contains("Npgsql") == true)
                {
                    descriptorsToRemove.Add(service);
                }
            }

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<SmartUnderwriteDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString());
                options.EnableSensitiveDataLogging();
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        
        // Ensure the database is created after the host is built
        try
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SmartUnderwriteDbContext>();
            context.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            // Log the exception but don't fail the host creation
            Console.WriteLine($"Failed to create test database: {ex.Message}");
        }
        
        return host;
    }
}