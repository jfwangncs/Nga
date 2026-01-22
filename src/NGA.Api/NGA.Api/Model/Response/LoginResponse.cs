namespace NGA.Api.Model.View
{
    public class LoginResponse
    {
        public int Id { get; set; }

        public required string Username { get; set; }

        public required string Token { get; set; }

        public int ExpiresIn { get; set; }
    }
}
