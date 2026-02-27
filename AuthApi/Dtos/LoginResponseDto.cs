namespace AuthApi.Dtos
{
    public class LoginResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public UserInfoDto User { get; set; } = null!;
    }
}
