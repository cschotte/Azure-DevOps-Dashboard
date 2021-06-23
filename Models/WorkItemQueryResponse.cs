using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Dashboard.Models
{
    public class WorkItemQueryResponse
    {
        public WorkItemQueryResponse()
        {
            WorkItems = new List<WorkItem>();
        }

        [JsonPropertyName("queryType")]
        public string QueryType { get; set; }

        [JsonPropertyName("queryResultType")]
        public string QueryResultType { get; set; }

        [JsonPropertyName("asOf")]
        public DateTime AsOf { get; set; }

        [JsonPropertyName("workItems")]
        public List<WorkItem> WorkItems { get; set; }
    }

    public class WorkItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("url")]
        public Uri Url { get; set; }
    }
}
