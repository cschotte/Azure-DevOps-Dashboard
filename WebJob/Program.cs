using System;
using System.Collections.Generic;
using System.IO;
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
        private static readonly HttpClient httpClient = new();

        // Read Azure DevOps credentials from application settings on WebJob startup
        // https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate
        private static readonly string azDevOpsPat = Environment.GetEnvironmentVariable("azDevOpsPat");
        private static readonly string azDevOpsUri = Environment.GetEnvironmentVariable("azDevOpsUri");

        static async Task Main()
        {
            Startup();

            var data = await GetDevOpsData();

            await File.WriteAllTextAsync("data.json", JsonConvert.SerializeObject(data, Formatting.Indented));

            Console.WriteLine($"{data.Count} Projects Done.");
        }

        private static async Task<List<DataModel>> GetDevOpsData()
        {
            var db = new List<DataModel>();

            // https://docs.microsoft.com/en-us/rest/api/azure/devops/core/projects/list
            var projects = await GetJsonAsync("/_apis/projects");

            if (null == projects)
                throw new ArgumentException("Your personal-access-token to Azure DevOps is expired or not valid");

            foreach (var project in projects.value)
            {
                Console.WriteLine("Processing project: " + project.name);

                var data = new DataModel
                {
                    ProjectId = project.id,
                    Name = project.name,
                    Description = project.description,
                    Url = project.url,
                    LastKnownActivity = project.lastUpdateTime
                };

                // https://docs.microsoft.com/en-us/rest/api/azure/devops/git/repositories/list
                var repositories = await GetJsonAsync($"/{project.id}/_apis/git/repositories");
                foreach (var repositorie in repositories.value)
                {
                    //Console.WriteLine(" - repositorie: " + repositorie.name);

                    // https://docs.microsoft.com/en-us/rest/api/azure/devops/git/commits/get%20commits
                    var commits = await GetJsonAsync($"/{project.id}/_apis/git/repositories/{repositorie.id}/commits?searchCriteria.$top=1");
                    foreach (var commit in commits.value)
                    {
                        //Console.WriteLine("  - last commit date: " + commit.committer.date);
                    }
                }

                db.Add(data);
            }

            return db;
        }

        private static async Task<dynamic> GetJsonAsync(string action)
        {
            var version = action.Contains('?') ? "&api-version=6.0" : "?api-version=6.0";

            using var response = await httpClient.GetAsync(azDevOpsUri + action + version);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                return (dynamic)JsonConvert.DeserializeObject(result);
            }

            // If no results (or Exception) retrun empty list
            return new { value = new List<string>() };
        }

        private static void Startup()
        {
            if (string.IsNullOrWhiteSpace(azDevOpsPat) || string.IsNullOrWhiteSpace(azDevOpsPat))
                throw new ArgumentException("Missing Azure DevOps Uri and personal-access-token Environment Variables");

            var authentication = Convert.ToBase64String(
                ASCIIEncoding.ASCII.GetBytes(
                    string.Format("{0}:{1}", string.Empty, azDevOpsPat)));

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authentication);
        }
    }
}
