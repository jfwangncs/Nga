using System.Collections.Generic;
using Newtonsoft.Json;
namespace NGA.Models.Models.RefreshCode
{
    public class QianWenRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("messages")]
        public List<QianWenRequestMessage> Messages { get; set; }
    }

    public class QianWenRequestMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public List<Content> Content { get; set; }
    }

    public class Content
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("image_url")]
        public ImageUrl ImageUrl { get; set; }
    }

    public class ImageUrl
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
