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
        const string _version = "6.0-preview.1";

        static readonly HttpClient _httpClient = new();

        // Read Azure DevOps credentials from application settings on WebJob startup
        // https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate
        static readonly string _azDevOpsPat = Environment.GetEnvironmentVariable("azDevOpsPat");
        static readonly string _azDevOpsUri = Environment.GetEnvironmentVariable("azDevOpsUri");

        static async Task Main()
        {
            Initialize();

            var data = await GetDevOpsData();

            await File.WriteAllTextAsync("data.json", JsonConvert.SerializeObject(data, Formatting.Indented));

            Console.WriteLine($"{data.Count} Projects Done.");
        }

        private static void Initialize()
        {
            if (string.IsNullOrWhiteSpace(_azDevOpsPat) || string.IsNullOrWhiteSpace(_azDevOpsPat))
                throw new ArgumentException("Missing Azure DevOps Uri (azDevOpsUri) and personal-access-token (azDevOpsPat) Environment Variables");

            var authentication = Convert.ToBase64String(
                ASCIIEncoding.ASCII.GetBytes(
                    string.Format($"{string.Empty}:{_azDevOpsPat}")));

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authentication);
        }

        private static async Task<List<DataModel>> GetDevOpsData()
        {
            var db = new List<DataModel>();

            // https://docs.microsoft.com/en-us/rest/api/azure/devops/core/projects/list
            var projects = await GetJsonAsync("/_apis/projects");
            foreach (var project in projects.value)
            {
                Console.WriteLine($"Processing project: {project.name}");

                var data = new DataModel
                {
                    ProjectId = project.id,
                    Name = project.name,
                    Description = project.description,
                    Url = project.url,
                    ProjectLastUpdateTime = project.lastUpdateTime
                };

                // https://docs.microsoft.com/en-us/rest/api/azure/devops/core/projects/get%20project%20properties
                var properties = await GetJsonAsync($"/_apis/projects/{project.id}/properties?keys=System.CurrentProcessTemplateId,System.Process%20Template");
                foreach (var propertie in properties.value)
                {
                    if (propertie.name == "System.Process Template")
                        data.ProcessTemplate = propertie.value;
                }

                // https://docs.microsoft.com/en-us/rest/api/azure/devops/wit/work%20items/get%20work%20item%20template
                //var workitems = await GetJsonAsync($"/_apis/Contribution/HierarchyQuery/project/{project.id}");
                //foreach (var workitem in workitems.value)
                //{
                //    //data.LastCommitDate = commit.committer.date;
                //}

                // https://docs.microsoft.com/en-us/rest/api/azure/devops/git/repositories/list
                var repositories = await GetJsonAsync($"/{project.id}/_apis/git/repositories");
                foreach (var repositorie in repositories.value)
                {
                    // https://docs.microsoft.com/en-us/rest/api/azure/devops/git/commits/get%20commits
                    var commits = await GetJsonAsync($"/{project.id}/_apis/git/repositories/{repositorie.id}/commits?searchCriteria.$top=1");
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
            var version = action.Contains('?') ? $"&api-version={_version}" : $"?api-version={_version}";

            using var response = await _httpClient.GetAsync(_azDevOpsUri + action + version);
 
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
    }
}
