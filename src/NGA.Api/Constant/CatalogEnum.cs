using System.Text.Json.Serialization;

namespace NGA.Api.Constant
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CatalogEnum
    { 
        All = 0, 
        DaXuanWo = -7, 
        Cosplay = 472
    }
}
