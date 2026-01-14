namespace NGA.UI.Model.Request
{
    public class LoginRequest
    {
        public required string Phone { get; set; }

        public required string Code { get; set; }
    }
}
