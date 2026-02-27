using AuthApi.Data;
using AuthApi.Dtos;
using AuthApi.Helpers;
using AuthApi.IntegrationTests.Factories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AuthApi.IntegrationTests.ControllerTests;

public class UsersControllerTests
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;
    private const string TestJwtSecret = "THIS_IS_A_TEST_SECRET_KEY_123456789";
    private const string TestJwtIssuer = "TestIssuer";
    private const string TestJwtAudience = "TestAudience";

    public UsersControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private class SimpleMessageResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    [Fact]
    public async Task Register_ShouldReturnSuccess()
    {
        // Arrange (use a NEW email, not the seeded one)
        var registerDto = new RegisterUserDto
        {
            FirstName = "New",
            LastName = "User",
            Email = "newuser@example.com",
            Password = "Password1"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content
            .ReadFromJsonAsync<SimpleMessageResponse>();

        content.Should().NotBeNull();
        content!.Message.Should().Be("User registered successfully");
    }

    [Fact]
    public async Task Login_ShouldReturnToken_WhenCredentialsValid()
    {
        // Arrange (seeded user)
        var loginDto = new LoginUserDto
        {
            Email = "testuser@example.com",
            Password = "Password1"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<LoginResponseDto>();

        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrWhiteSpace();
        result.User.Email.Should().Be("testuser@example.com");
    }

    [Fact]
    public async Task GetUserDetails_ShouldReturnUser_WhenAuthenticated()
    {
        // Arrange — get the actual seeded user from the in-memory database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var seededUser = db.Users.First(u => u.Email == "testuser@example.com");

        // Generate JWT using the actual user ID
        var token = JwtHelper.GenerateToken(
            userId: seededUser.Id,
            email: seededUser.Email,
            secretKey: TestJwtSecret,
            issuer: TestJwtIssuer,
            audience: TestJwtAudience
        );

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act — call /me endpoint
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content
            .ReadFromJsonAsync<UserInfoDto>();

        user.Should().NotBeNull();
        user!.Email.Should().Be("testuser@example.com");
        user.Id.Should().Be(seededUser.Id); // optional extra check
    }
}