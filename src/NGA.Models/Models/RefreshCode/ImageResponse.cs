using System.Text.Json.Serialization;

namespace NGA.Models.Models.RefreshCode
{
    public class ImageResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public DataModel Data { get; set; }
    }

    public class DataModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("pathname")]
        public string Pathname { get; set; }

        [JsonPropertyName("origin_name")]
        public string OriginName { get; set; }

        [JsonPropertyName("size")]
        public string Size { get; set; }

        [JsonPropertyName("mimetype")]
        public string MimeType { get; set; }

        [JsonPropertyName("md5")]
        public string Md5 { get; set; }

        [JsonPropertyName("sha1")]
        public string Sha1 { get; set; }

        [JsonPropertyName("links")]
        public LinksModel Links { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    public class LinksModel
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("html")]
        public string Html { get; set; }

        [JsonPropertyName("bbcode")]
        public string BbCode { get; set; }

        [JsonPropertyName("markdown")]
        public string Markdown { get; set; }

        [JsonPropertyName("markdown_with_link")]
        public string MarkdownWithLink { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        [JsonPropertyName("delete_url")]
        public string DeleteUrl { get; set; }
    }
}
