using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;

namespace Review
{
    public class UpdateReview
    {
        private CosmosClient _cosmosClient;
        private Database _database;
        private Container _container;

        public UpdateReview(
        CosmosClient cosmosClient
        )
        {
            _cosmosClient = cosmosClient;

            _database = _cosmosClient.GetDatabase("Restaurants");
            _container = _database.GetContainer("RestaurantItems");
        }

        [FunctionName("UpdateReview")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "UpdateReview/{rid}")] HttpRequest req, string rid,
            [CosmosDB(
            databaseName: "Restaurants",
            collectionName: "RestaurantItems",
            ConnectionStringSetting = "CosmosDBConnectionString")]IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            IActionResult returnValue = null;

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                var input = JsonConvert.DeserializeObject<Review>(requestBody);                

                var newReview = new Review
                {
                    id = input.id,
                    Date = input.Date,
                    Rating = input.Rating,
                    Text = input.Text,
                    Answer = input.Answer
                };

                var path = "/Reviews/" + input.id;
                log.LogInformation("Path: " + path);
                List<PatchOperation> patchOperations = new()
                {
                    PatchOperation.Set(path: path, value: newReview)
                };

                ItemResponse<dynamic> item = await _container.PatchItemAsync<dynamic>(
                    id: rid,
                    partitionKey: new PartitionKey(rid),
                    patchOperations: patchOperations
                    );

                log.LogInformation("Item inserted");
                log.LogInformation($"This query cost: {item.RequestCharge} RU/s");
                returnValue = new OkObjectResult(newReview.id);
            }
            catch (Exception ex)
            {
                log.LogError($"Could not insert review. Exception thrown: {ex.Message}");
                returnValue = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return returnValue;
        }
    }
}
