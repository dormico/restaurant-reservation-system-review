using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;

namespace Review
{
    public class CalcRating
    {
        private CosmosClient _cosmosClient;
        private Database _database;
        private Container _container;

        public CalcRating(
        CosmosClient cosmosClient
        )
        {
            _cosmosClient = cosmosClient;

            _database = _cosmosClient.GetDatabase("Restaurants");
            _container = _database.GetContainer("RestaurantItems");
        }
        [FunctionName("CalcRating")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "CalcRating/{rid}")] HttpRequest req, string rid,
            [CosmosDB(
            databaseName: "Restaurants",
            collectionName: "RestaurantItems",
            ConnectionStringSetting = "CosmosDBConnectionString",
            Id = "{rid}",
            PartitionKey = "{rid}")]RestaurantItem restaurant,
            [CosmosDB(
            databaseName: "Restaurants",
            collectionName: "RestaurantItems",
            ConnectionStringSetting = "CosmosDBConnectionString")]IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {
            log.LogInformation("CalcRating trigger function processed a request.");

            IActionResult returnValue = null;
            try
            {
                int sum = 0;
                foreach (var review in restaurant.Reviews)
                {
                    sum += review.Rating;
                }
                var newRating = sum / restaurant.Reviews.Length;

                var path = "/Rating";
                log.LogInformation("Path: " + path);
                List<PatchOperation> patchOperations = new List<PatchOperation>()
                {
                    PatchOperation.Set(path: path, value: newRating)
                };

                ItemResponse<dynamic> item = await _container.PatchItemAsync<dynamic>(
                    id: rid,
                    partitionKey: new PartitionKey(rid),
                    patchOperations: patchOperations
                    );

                log.LogInformation("Item inserted");
                log.LogInformation($"This query cost: {item.RequestCharge} RU/s");

                returnValue = new OkObjectResult(restaurant.Rating);
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
