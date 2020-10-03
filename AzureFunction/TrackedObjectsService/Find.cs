using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace TrackedObjectsService
{
    public static class Find
    {
        [FunctionName("Find")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            string name = req.Query["name"];
            var request = await DataService.Find(name);
            var response = JsonConvert.SerializeObject(request);

            return new OkObjectResult(response);
        }
    }
}
