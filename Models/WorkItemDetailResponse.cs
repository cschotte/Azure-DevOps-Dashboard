using System;
using System.Text.Json.Serialization;

namespace Dashboard.Models
{
    public class WorkItemDetailResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("rev")]
        public int Rev { get; set; }

        [JsonPropertyName("fields")]
        public Fields Fields { get; set; }

        [JsonPropertyName("url")]
        public Uri Url { get; set; }
    }

    public class Fields
    {
        [JsonPropertyName("System.CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("System.ChangedDate")]
        public DateTime ChangedDate { get; set; }
    }
}
