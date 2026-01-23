namespace NGA.Api.Model.Request
{
    public class LoginRequest
    {
        public required string Phone { get; set; }

        public required string Code { get; set; }
    }
}
