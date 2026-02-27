using AuthApi.Data;
using AuthApi.Dtos;
using AuthApi.Models;
using AuthApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;

namespace AuthApi.Tests.ServiceTests
{
    public class UserServiceTests
    {
        private AuthDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AuthDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AuthDbContext(options);
        }

        private UserService CreateService(AuthDbContext? context = null)
        {
            context ??= GetInMemoryDbContext();

            var config = Substitute.For<IConfiguration>();
            config["JwtSettings:SecretKey"].Returns("supersecretkeysupersecretkey1234");
            config["JwtSettings:Issuer"].Returns("TestIssuer");
            config["JwtSettings:Audience"].Returns("TestAudience");

            return new UserService(context, config);
        }

        // -----------------------------
        // RegisterAsync Tests
        // -----------------------------

        [Fact]
        public async Task RegisterAsync_ShouldAddUser_WhenEmailIsUnique()
        {
            var context = GetInMemoryDbContext();
            var service = CreateService(context);

            var dto = new RegisterUserDto
            {
                FirstName = "Tinus",
                LastName = "Taljaard",
                Email = "tinus@example.com",
                Password = "Password123!"
            };

            await service.RegisterAsync(dto);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "tinus@example.com");

            user.ShouldNotBeNull();
            user.FirstName.ShouldBe("Tinus");
            user.PasswordHash.ShouldNotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrow_WhenEmailExists()
        {
            var context = GetInMemoryDbContext();

            context.Users.Add(new User
            {
                Email = "existing@example.com",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "User"
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new RegisterUserDto
            {
                FirstName = "New",
                LastName = "User",
                Email = "existing@example.com",
                Password = "Password123!"
            };

            await Should.ThrowAsync<InvalidOperationException>(() =>
                service.RegisterAsync(dto));
        }

        // -----------------------------
        // LoginAsync Tests
        // -----------------------------

        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsValid()
        {
            var context = GetInMemoryDbContext();

            var password = "Password123!";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            context.Users.Add(new User
            {
                Id = 1,
                Email = "tinus@example.com",
                PasswordHash = hash,
                FirstName = "Tinus",
                LastName = "Taljaard"
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new LoginUserDto
            {
                Email = "tinus@example.com",
                Password = password
            };

            var result = await service.LoginAsync(dto);

            result.ShouldNotBeNull();
            result.Token.ShouldNotBeNullOrWhiteSpace();
            result.User.Email.ShouldBe("tinus@example.com");
        }

        [Fact]
        public async Task LoginAsync_ShouldThrow_WhenPasswordInvalid()
        {
            var context = GetInMemoryDbContext();

            context.Users.Add(new User
            {
                Email = "tinus@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
                FirstName = "Test",
                LastName = "User"
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new LoginUserDto
            {
                Email = "tinus@example.com",
                Password = "WrongPassword"
            };

            await Should.ThrowAsync<UnauthorizedAccessException>(() =>
                service.LoginAsync(dto));
        }

        // -----------------------------
        // GetUserDetailsAsync Tests
        // -----------------------------

        [Fact]
        public async Task GetUserDetailsAsync_ShouldReturnUser_WhenExists()
        {
            var context = GetInMemoryDbContext();

            context.Users.Add(new User
            {
                Id = 1,
                FirstName = "Tinus",
                LastName = "Taljaard",
                Email = "tinus@example.com"
            });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetUserDetailsAsync(1);

            result.ShouldNotBeNull();
            result.Email.ShouldBe("tinus@example.com");
        }

        [Fact]
        public async Task GetUserDetailsAsync_ShouldReturnNull_WhenNotFound()
        {
            var service = CreateService();

            var result = await service.GetUserDetailsAsync(999);

            result.ShouldBeNull();
        }
    }
}