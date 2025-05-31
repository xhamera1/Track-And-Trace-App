// PackageSize.cs
using System.Text.Json.Serialization;

namespace _10.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PackageSize
    {
        Small,
        Medium,
        Large
    }
}
