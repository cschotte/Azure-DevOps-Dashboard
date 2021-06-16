/*
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using Dashboard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dashboard.Controllers
{
    [Authorize]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;

        public ApiController(ILogger<ApiController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("api/data")]
        public async Task<List<DataModel>> DataAsync()
        {
            try
            {
                var result = await ReadDataAsync("data.json");

                return JsonSerializer.Deserialize<List<DataModel>>(result);
            }
            catch
            {
                return new List<DataModel>();
            }
        }

        [HttpGet]
        [Route("api/status")]
        public async Task<StatusModel> StatusAsync()
        {
            try
            {
                var result = await ReadDataAsync("status.json");

                return JsonSerializer.Deserialize<StatusModel>(result);
            }
            catch
            {
                return new StatusModel
                {
                    Error = true,
                    Message = "No data found. Did you run the webjob first?"
                };
            }
        }

        private static async Task<string> ReadDataAsync(string filename)
        {
            try
            {
                var homepath = Environment.GetEnvironmentVariable("HOME");

                if (!string.IsNullOrWhiteSpace(homepath))
                    filename = $"{homepath}{Path.DirectorySeparatorChar}{filename}";

                return await System.IO.File.ReadAllTextAsync(filename);
            }
            catch
            {
                return null;
            }
        }

    }
}
