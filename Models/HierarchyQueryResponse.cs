using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Dashboard.Models
{
    public class HierarchyQueryResponse
    {
        [JsonPropertyName("dataProviders")]
        public DataProviders DataProviders { get; set; }
    }

    public class Identity
    {
        [JsonPropertyName("subjectKind")]
        public string SubjectKind { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("mailAddress")]
        public string MailAddress { get; set; }
    }

    public class ProjectAdmins
    {
        [JsonPropertyName("identities")]
        public List<Identity> Identities { get; set; }

        [JsonPropertyName("totalIdentityCount")]
        public int TotalIdentityCount { get; set; }
    }

    public class MsVssAdminWeb
    {
        [JsonPropertyName("projectAdmins")]
        public ProjectAdmins ProjectAdmins { get; set; }
    }

    public class DataProviders
    {
        [JsonPropertyName("ms.vss-admin-web.project-admin-overview-delay-load-data-provider")]
        public MsVssAdminWeb MsVssAdminWeb { get; set; }
    }

    


}
