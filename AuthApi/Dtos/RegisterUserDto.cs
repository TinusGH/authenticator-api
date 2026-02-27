using System.ComponentModel.DataAnnotations;

namespace AuthApi.Dtos
{
    public class RegisterUserDto
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required, MinLength(6)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$",
            ErrorMessage = "Password must contain uppercase, lowercase, and a number")]
        public string Password { get; set; } = null!;
    }
}
