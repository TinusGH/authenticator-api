using AuthApi.Dtos;
using AuthApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;
using System.Security.Claims;

namespace AuthApi.Tests.ControllerTests
{
    public class UsersControllerTests
    {
        private readonly IUserService _userService;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _userService = Substitute.For<IUserService>();
            _controller = new UsersController(_userService);
        }

        // ----------------------------
        // Register Tests
        // ----------------------------

        [Fact]
        public async Task Register_ShouldReturnOk_WhenSuccessful()
        {
            var dto = new RegisterUserDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                Password = "Password123!"
            };

            await _userService.RegisterAsync(dto);

            var result = await _controller.Register(dto);

            result.ShouldBeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenEmailExists()
        {
            var dto = new RegisterUserDto();

            _userService.RegisterAsync(dto)
                .Returns(Task.FromException(new InvalidOperationException("Email already registered.")));

            var result = await _controller.Register(dto);

            result.ShouldBeOfType<BadRequestObjectResult>();
        }

        // ----------------------------
        // Login Tests
        // ----------------------------

        [Fact]
        public async Task Login_ShouldReturnOk_WhenSuccessful()
        {
            var dto = new LoginUserDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var response = new LoginResponseDto
            {
                Message = "Login successful",
                Token = "fake-jwt-token"
            };

            _userService.LoginAsync(dto).Returns(response);

            var result = await _controller.Login(dto);

            result.ShouldBeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenInvalidCredentials()
        {
            var dto = new LoginUserDto();

            _userService.LoginAsync(dto)
                .Returns(Task.FromException<LoginResponseDto>(
                    new UnauthorizedAccessException("Invalid credentials")));

            var result = await _controller.Login(dto);

            result.ShouldBeOfType<UnauthorizedObjectResult>();
        }

        // ----------------------------
        // GetUserDetails Tests
        // ----------------------------

        [Fact]
        public async Task GetUserDetails_ShouldReturnOk_WhenUserExists()
        {
            var user = new UserInfoDto
            {
                Id = 1,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            _userService.GetUserDetailsAsync(1).Returns(user);

            // Fake authenticated user
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var result = await _controller.GetUserDetails();

            result.ShouldBeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetUserDetails_ShouldReturnNotFound_WhenUserMissing()
        {
            _userService.GetUserDetailsAsync(1).Returns((UserInfoDto?)null);

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var result = await _controller.GetUserDetails();

            result.ShouldBeOfType<NotFoundResult>();
        }
    }
}