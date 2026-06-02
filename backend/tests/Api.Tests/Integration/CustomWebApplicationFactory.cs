using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Api.Data;

namespace Api.Tests.Integration;

/// <summary>
/// Custom web application factory for integration testing.
/// Configures the test server with InMemory database.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder webHostBuilder)
    {
        webHostBuilder.UseEnvironment("Testing");

        webHostBuilder.ConfigureServices(serviceCollection =>
        {
            // Remove real DbContext registration
            var existingDbDescriptor = serviceCollection.SingleOrDefault(
                descriptor => descriptor.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (existingDbDescriptor != null)
                serviceCollection.Remove(existingDbDescriptor);

            // Add InMemory DbContext for tests with a shared name per factory instance
            var testDbName = $"TestDb_{Guid.NewGuid()}";
            serviceCollection.AddDbContext<AppDbContext>(dbOptions =>
                dbOptions.UseInMemoryDatabase(testDbName));

            // Ensure the database is created and seeded
            var serviceProvider = serviceCollection.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }
}
