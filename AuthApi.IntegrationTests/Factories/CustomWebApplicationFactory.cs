using AuthApi.Data;
using AuthApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace AuthApi.IntegrationTests.Factories;

public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Override JWT configuration for tests
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testSettings = new Dictionary<string, string>
            {
                ["JwtSettings:SecretKey"] = "THIS_IS_A_TEST_SECRET_KEY_123456789",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience"
            };

            config.AddInMemoryCollection(testSettings!);
        });

        builder.ConfigureTestServices(services =>
        {
            // Relax JWT validation so tests don’t fail on token checks
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters.ValidateIssuer = false;
                    options.TokenValidationParameters.ValidateAudience = false;
                    options.TokenValidationParameters.ValidateLifetime = false;
                    options.TokenValidationParameters.NameClaimType = ClaimTypes.NameIdentifier;
                    options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes("THIS_IS_A_TEST_SECRET_KEY_123456789"));
                });

            // Remove existing DbContext (PostgreSQL)
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // Use a stable in-memory database for all tests
            services.AddDbContext<AuthDbContext>(options =>
                options.UseInMemoryDatabase("IntegrationTestsDb"));

            // Seed user once
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            if (!db.Users.Any())
            {
                db.Users.Add(new User
                {
                    FirstName = "Test",
                    LastName = "User",
                    Email = "testuser@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1"),
                    CreatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
            }
        });
    }
}