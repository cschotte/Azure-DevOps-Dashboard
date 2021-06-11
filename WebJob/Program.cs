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

                await SaveDevOpsData(JsonConvert.SerializeObject(data, Formatting.Indented));

                Console.WriteLine($"{data.Count} Projects Done.");
            }
            catch(Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        private static void Initialize()
        {
            Console.WriteLine($"Starting...");

            if (string.IsNullOrWhiteSpace(_azDevOpsUri) || string.IsNullOrWhiteSpace(_azDevOpsPat))
                throw new ArgumentException("Missing Azure DevOps Uri (azDevOpsUri) and personal-access-token (azDevOpsPat) Environment Variables");

            // remove last '/' if any
            _azDevOpsUri = _azDevOpsUri.TrimEnd(new[] { '/' });

            var authentication = Convert.ToBase64String(
                ASCIIEncoding.ASCII.GetBytes(
                    string.Format($"{string.Empty}:{_azDevOpsPat}")));

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authentication);
        }

        private static async Task SaveDevOpsData(string data)
        {
            Console.WriteLine($"Writing results to file...");

            var filename = "data.json";
            var homepath = Environment.GetEnvironmentVariable("HOME"); // When running on Azure
            
            if (!string.IsNullOrWhiteSpace(homepath))
                filename = $"{homepath}{Path.DirectorySeparatorChar}{filename}";

            await File.WriteAllTextAsync(filename, data);
        }

        private static async Task<List<DataModel>> GetDevOpsData()
        {
            var db = new List<DataModel>();

            // https://docs.microsoft.com/en-us/rest/api/azure/devops/core/projects/list
            var projects = await GetJsonAsync(
                $"/_apis/projects?$top={max}&api-version=6.1-preview.4");
            foreach (var project in projects.value)
            {
                Console.WriteLine($"Processing: {project.name}");

                var data = new DataModel
                {
                    ProjectId = project.id,
                    Name = project.name,
                    Description = project.description,
                    Url = $"{_azDevOpsUri}/{project.name}",
                    LastProjectUpdateTime = project.lastUpdateTime
                };

                // https://docs.microsoft.com/en-us/rest/api/azure/devops/core/projects/get%20project%20properties
                var properties = await GetJsonAsync(
                    $"/_apis/projects/{project.id}/properties?keys=System.CurrentProcessTemplateId,System.Process%20Template&api-version=6.1-preview.1");
                foreach (var propertie in properties.value)
                {
                    if (propertie.name == "System.Process Template")
                        data.ProcessTemplate = propertie.value;
                }

                // https://docs.microsoft.com/en-us/rest/api/azure/devops/wit/wiql/query%20by%20wiql
                var items = await PostJsonAsync(
                    $"/{project.id}/_apis/wit/wiql?$top=1&api-version=6.1-preview.2",
                    $"SELECT [System.Id] FROM workitems WHERE [System.WorkItemType] <> '' AND [System.State] <> '' AND [System.TeamProject] = @project ORDER BY [System.ChangedDate] DESC");
                foreach (var item in items.workItems)
                {
                    // https://docs.microsoft.com/en-us/rest/api/azure/devops/wit/work%20items/get%20work%20item
                    var workitem = await GetJsonAsync(
                        $"/{project.id}/_apis/wit/workitems/{item.id}?api-version=6.1-preview.3");
                    foreach (var field in workitem.fields)
                    {
                        if (field.Name == "System.ChangedDate" && data.LastWorkItemDate < (DateTime)field.Value)
                            data.LastWorkItemDate = field.Value;
                    }
                }

                // https://docs.microsoft.com/en-us/rest/api/azure/devops/git/repositories/list
                var repositories = await GetJsonAsync(
                    $"/{project.id}/_apis/git/repositories?api-version=6.1-preview.1");
                foreach (var repositorie in repositories.value)
                {
                    // https://docs.microsoft.com/en-us/rest/api/azure/devops/git/commits/get%20commits
                    var commits = await GetJsonAsync(
                        $"/{project.id}/_apis/git/repositories/{repositorie.id}/commits?searchCriteria.$top=1&api-version=6.1-preview.1");
                    foreach (var commit in commits.value)
                    {
                        data.LastCommitDate = commit.committer.date;
                    }
                }

                db.Add(data);
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

            // If no results (or other Exception) we ignor and retrun an empty list
            return new { value = new List<string>() };
        }

        private static async Task<dynamic> PostJsonAsync(string action, string query)
        {
            var content = new StringContent("{ \"query\" : \"" + query + "\" }", Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(_azDevOpsUri + action, content);

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NonAuthoritativeInformation)
                throw new ArgumentException("Your personal-access-token to Azure DevOps is expired or not valid");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                return (dynamic)JsonConvert.DeserializeObject(result);
            }

            // If no results (or other Exception) we ignor and retrun an empty list
            return new { workItems = new List<string>() };
        }
    }
}
