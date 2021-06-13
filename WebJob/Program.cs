/*
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebJob.Models;

namespace WebJob
{
    class Program
    {
        // max projects to process
        const int max = 750;

        static readonly HttpClient _httpClient = new();

        // Read Azure DevOps credentials from application settings on WebJob startup
        // https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate
        static string _azDevOpsPat = Environment.GetEnvironmentVariable("azDevOpsPat");
        static string _azDevOpsUri = Environment.GetEnvironmentVariable("azDevOpsUri");

        static async Task Main()
        {
            try
            {
                Initialize();

                var data = await GetDevOpsData();
                await SaveData("data.json", data);

                var status = new StatusModel { Message = $"{data.Count} Projects processed." };
                await SaveData("status.json", status);

                Console.WriteLine(status.Message);
            }
            catch(Exception e)
            {
                var status = new StatusModel { Error = true, Message = e.Message };
                await SaveData("status.json", status);

                Console.WriteLine($"Error: {status.Message}");
            }
        }

        private static void Initialize()
        {
            Console.WriteLine($"Starting...");

            if (string.IsNullOrWhiteSpace(_azDevOpsUri) || string.IsNullOrWhiteSpace(_azDevOpsPat))
                throw new ArgumentException("Set Azure DevOps Uri (azDevOpsUri) and personal-access-token (azDevOpsPat) Environment Variables first");

            // remove last '/' if any
            _azDevOpsUri = _azDevOpsUri.TrimEnd(new[] { '/' });

            var authentication = Convert.ToBase64String(
                ASCIIEncoding.ASCII.GetBytes(
                    string.Format($"{string.Empty}:{_azDevOpsPat}")));

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authentication);
        }

        private static async Task SaveData(string filename, object data)
        {
            Console.WriteLine($"Writing file '{filename}'");

            // When running on Azure we have a HOME
            var homepath = Environment.GetEnvironmentVariable("HOME");
            
            if (!string.IsNullOrWhiteSpace(homepath))
                filename = $"{homepath}{Path.DirectorySeparatorChar}{filename}";

            await File.WriteAllTextAsync(filename,
                JsonConvert.SerializeObject(data));
        }

        private static async Task<List<DataModel>> GetDevOpsData()
        {
            var db = new List<DataModel>();

            // Get projects
            // https://docs.microsoft.com/en-us/rest/api/azure/devops/core/projects/list
            var projects = await GetJsonAsync(
                $"/_apis/projects?$top={max}&api-version=6.1-preview.4");
            if (projects != null)
            {
                foreach (var project in projects.value)
                {
                    Console.WriteLine($"Processing: {project.name}");

                    var data = new DataModel
                    {
                        ProjectId = project.id,
                        Name = project.name,
                        Description = project.description,
                        Url = new Uri($"{_azDevOpsUri}/{project.name}"),
                        LastProjectUpdateTime = project.lastUpdateTime
                    };

                    // Get projects properties
                    // https://docs.microsoft.com/en-us/rest/api/azure/devops/core/projects/get%20project%20properties
                    var properties = await GetJsonAsync(
                        $"/_apis/projects/{project.id}/properties?keys=System.CurrentProcessTemplateId,System.Process%20Template&api-version=6.1-preview.1");
                    if (properties != null)
                    {
                        foreach (var propertie in properties.value)
                        {
                            if (propertie.name == "System.Process Template")
                                data.ProcessTemplate = propertie.value;
                        }
                    }

                    // Get last updated workitem id
                    // https://docs.microsoft.com/en-us/rest/api/azure/devops/wit/wiql/query%20by%20wiql
                    var items = await PostJsonAsync(
                        $"/{project.id}/_apis/wit/wiql?$top=1&api-version=6.1-preview.2",
                        "{ \"query\" : \"SELECT [System.Id] FROM workitems WHERE [System.WorkItemType] <> '' AND [System.State] <> '' AND [System.TeamProject] = @project ORDER BY [System.ChangedDate] DESC\" }");
                    if (items != null)
                    {
                        foreach (var item in items.workItems)
                        {
                            // Get workitem details
                            // https://docs.microsoft.com/en-us/rest/api/azure/devops/wit/work%20items/get%20work%20item
                            var workitem = await GetJsonAsync(
                                $"/{project.id}/_apis/wit/workitems/{item.id}?api-version=6.1-preview.3");
                            if (workitem != null)
                            {
                                foreach (var field in workitem.fields)
                                {
                                    if (field.Name == "System.ChangedDate" && data.LastWorkItemDate < (DateTime)field.Value)
                                        data.LastWorkItemDate = field.Value;
                                }
                            }
                        }
                    }

                    // Get project owners
                    var owners = await PostJsonAsync(
                        "/_apis/Contribution/HierarchyQuery?api-version=5.0-preview.1",
                        "{ \"contributionIds\": [ \"ms.vss-admin-web.project-admin-overview-delay-load-data-provider\" ], \"dataProviderContext\": { \"properties\": { \"projectId\": \"" + project.id + "\" } } }");
                    if (owners != null)
                    {
                        foreach(var owner in owners["dataProviders"]["ms.vss-admin-web.project-admin-overview-delay-load-data-provider"]["projectAdmins"]["identities"])
                        {
                            if(owner.subjectKind == "user")
                            {
                                var admin = new UserModel
                                {
                                    DisplayName = owner.displayName,
                                    MailAddress = owner.mailAddress
                                };

                                data.Owners.Add(admin);
                            }
                        }
                    }

                    // Get repositories
                    // https://docs.microsoft.com/en-us/rest/api/azure/devops/git/repositories/list
                    var repositories = await GetJsonAsync(
                        $"/{project.id}/_apis/git/repositories?api-version=6.1-preview.1");
                    if (repositories != null)
                    {
                        foreach (var repositorie in repositories.value)
                        {
                            // Get last commit
                            // https://docs.microsoft.com/en-us/rest/api/azure/devops/git/commits/get%20commits
                            var commits = await GetJsonAsync(
                                $"/{project.id}/_apis/git/repositories/{repositorie.id}/commits?searchCriteria.$top=1&api-version=6.1-preview.1");
                            if (commits != null)
                            {
                                foreach (var commit in commits.value)
                                {
                                    data.LastCommitDate = commit.committer.date;
                                }
                            }
                        }
                    }

                    db.Add(data);
                }
            }

            return db;
        }

        private static async Task<dynamic> GetJsonAsync(string action)
        {
            using var response = await _httpClient.GetAsync(_azDevOpsUri + action);

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NonAuthoritativeInformation)
                throw new ArgumentException("Your personal-access-token to Azure DevOps is expired or not valid");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                return (dynamic)JsonConvert.DeserializeObject(result);
            }

            // If no results (or other Exception) we ignor and retrun null
            return new { value = new List<string>() };
        }

        private static async Task<dynamic> PostJsonAsync(string action, string content)
        {
            var data = new StringContent(content, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(_azDevOpsUri + action, data);

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NonAuthoritativeInformation)
                throw new ArgumentException("Your personal-access-token to Azure DevOps is expired or not valid");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                return (dynamic)JsonConvert.DeserializeObject(result);
            }

            // If no results (or other Exception) we ignor and retrun null
            return null;
        }
    }
}
