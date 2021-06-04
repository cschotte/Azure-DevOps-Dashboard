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
        // https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate
        static readonly string azDevOpsPat = Environment.GetEnvironmentVariable("azDevOpsPat");
        static readonly string azDevOpsUri = Environment.GetEnvironmentVariable("azDevOpsUri");

        static async Task Main()
        {
            var db = new List<DataModel>();

            if (string.IsNullOrWhiteSpace(azDevOpsPat) || string.IsNullOrWhiteSpace(azDevOpsPat))
                throw new ArgumentException("Missing Azure DevOps Uri and personal-access-token Environment Variables");

            // https://docs.microsoft.com/en-us/rest/api/azure/devops/core/projects/list
            var projects = await GetJson("/_apis/projects");
            
            if (null == projects)
                throw new ArgumentException("Your personal-access-token to Azure DevOps is expired or not valid");

            foreach (var project in projects.value)
            {
                var data = new DataModel
                {
                    ProjectId = project.id,
                    Name = project.name,
                    Description = project.description,
                    Url = project.url,
                    LastKnownActivity = project.lastUpdateTime
                };

                Console.WriteLine();
                Console.WriteLine("project: " + project.name);
                Console.WriteLine("last project update date: " + project.lastUpdateTime);

                // https://docs.microsoft.com/en-us/rest/api/azure/devops/git/repositories/list
                var repositories = await GetJson($"/{project.id}/_apis/git/repositories");

                foreach (var repositorie in repositories.value)
                {
                    Console.WriteLine(" - repositorie: " + repositorie.name);

                    // https://docs.microsoft.com/en-us/rest/api/azure/devops/git/commits/get%20commits
                    var commits = await GetJson($"/{project.id}/_apis/git/repositories/{repositorie.id}/commits?searchCriteria.$top=1");

                    foreach (var commit in commits.value)
                    {
                        Console.WriteLine("  - last commit date: " + commit.committer.date);
                    }
                }

                db.Add(data);
            }

            File.WriteAllText("data.json", JsonConvert.SerializeObject(db, Formatting.Indented));

            Console.WriteLine("Done.");
        }

        private static async Task<dynamic> GetJson(string action)
        {
            var authentication = Convert.ToBase64String(
                ASCIIEncoding.ASCII.GetBytes(
                    string.Format("{0}:{1}", string.Empty, azDevOpsPat)));

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authentication);

            var version = action.Contains('?') ? "&api-version=6.0" : "?api-version=6.0";

            using var response = await client.GetAsync(azDevOpsUri + action + version);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                return (dynamic)JsonConvert.DeserializeObject(result);
            }

            // No results (or Exception)
            return new { value = new List<string>() };
        }
    }
}
