using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Dashboard.Models
{
    public class RepositoryResponse
    {
        public RepositoryResponse()
        {
            Repositories = new List<Repository>();
        }

        [JsonPropertyName("value")]
        public List<Repository> Repositories { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class Repository
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public Uri Url { get; set; }
    }
}
