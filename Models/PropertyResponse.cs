using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Dashboard.Models
{
    public class PropertyResponse
    {
        public PropertyResponse()
        {
            Properties = new List<Property>();
        }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("value")]
        public List<Property> Properties { get; set; }
    }

    public class Property
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
