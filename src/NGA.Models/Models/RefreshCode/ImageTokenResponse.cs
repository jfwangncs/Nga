using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NGA.Models.Models.RefreshCode
{
    public class TokenResponse
    {
        public bool Status { get; set; }

        public string Message { get; set; }

        public TokenData Data { get; set; }
    }

    public class TokenData
    {
        public List<TokenInfo> Tokens { get; set; }
    }

    public class TokenInfo
    {
        public string Token { get; set; }

        [JsonPropertyName("expired_at")]
        public string ExpiredAt { get; set; }
    }
}
