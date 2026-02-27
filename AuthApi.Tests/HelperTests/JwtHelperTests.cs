using AuthApi.Helpers;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace AuthApi.Tests.HelperTests
{
    public class JwtHelperTests
    {
        private const string SecretKey = "THIS_IS_A_SUPER_SECRET_TEST_KEY_123456";
        private const string Issuer = "TestIssuer";
        private const string Audience = "TestAudience";

        [Fact]
        public void GenerateToken_ShouldReturnTokenString()
        {
            var token = JwtHelper.GenerateToken(
                1,
                "test@example.com",
                SecretKey,
                Issuer,
                Audience,
                60);

            token.ShouldNotBeNullOrWhiteSpace();
        }

        [Fact]
        public void GenerateToken_ShouldContainCorrectClaims()
        {
            var tokenString = JwtHelper.GenerateToken(
                42,
                "tinus@example.com",
                SecretKey,
                Issuer,
                Audience,
                60);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            var subClaim = token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub);
            var emailClaim = token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email);

            subClaim.Value.ShouldBe("42");
            emailClaim.Value.ShouldBe("tinus@example.com");
        }

        [Fact]
        public void GenerateToken_ShouldHaveCorrectIssuerAndAudience()
        {
            var tokenString = JwtHelper.GenerateToken(
                1,
                "test@example.com",
                SecretKey,
                Issuer,
                Audience,
                60);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            token.Issuer.ShouldBe(Issuer);
            token.Audiences.First().ShouldBe(Audience);
        }

        [Fact]
        public void GenerateToken_ShouldExpireWithinExpectedTime()
        {
            var expireMinutes = 5;

            var tokenString = JwtHelper.GenerateToken(
                1,
                "test@example.com",
                SecretKey,
                Issuer,
                Audience,
                expireMinutes);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            var expectedExpiry = DateTime.UtcNow.AddMinutes(expireMinutes);

            token.ValidTo.ShouldBeLessThanOrEqualTo(expectedExpiry.AddSeconds(5));
            token.ValidTo.ShouldBeGreaterThan(DateTime.UtcNow);
        }

        [Fact]
        public void GeneratedToken_ShouldValidateSuccessfully_WithCorrectKey()
        {
            var tokenString = JwtHelper.GenerateToken(
                1,
                "test@example.com",
                SecretKey,
                Issuer,
                Audience,
                60);

            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey))
            };

            var principal = tokenHandler.ValidateToken(
                tokenString,
                validationParams,
                out var validatedToken);

            principal.ShouldNotBeNull();
            validatedToken.ShouldNotBeNull();
        }

        [Fact]
        public void GeneratedToken_ShouldFailValidation_WithWrongKey()
        {
            var tokenString = JwtHelper.GenerateToken(
                1,
                "test@example.com",
                SecretKey,
                Issuer,
                Audience,
                60);

            var tokenHandler = new JwtSecurityTokenHandler();

            var wrongValidationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("WRONG_SECRET_KEY_123456789012345"))
            };

            Should.Throw<SecurityTokenInvalidSignatureException>(() =>
            {
                tokenHandler.ValidateToken(
                    tokenString,
                    wrongValidationParams,
                    out _);
            });
        }
    }
}