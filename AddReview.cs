using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using System.Net.Http;

namespace Review
{
    public class AddReview
    {
        private CosmosClient _cosmosClient;
        private Database _database;
        private Container _container;

        public AddReview(
        CosmosClient cosmosClient
        )
        {
            _cosmosClient = cosmosClient;

            _database = _cosmosClient.GetDatabase("Restaurants");
            _container = _database.GetContainer("RestaurantItems");
        }

        [FunctionName("AddReview")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "AddReview/{rid}")] HttpRequest req, string rid,
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
            IActionResult returnValue = null;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var input = JsonConvert.DeserializeObject<Review>(requestBody);

                var index = restaurant.Reviews.Length.ToString();

                var newReview = new Review
                {
                    id = index,
                    Date = input.Date,
                    Rating = input.Rating,
                    Text = input.Text,
                    Answer = input.Answer
                };

                var path = "/Reviews/" + index;
                log.LogInformation("Path: " + path);
                List<PatchOperation> patchOperations = new List<PatchOperation>()
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

                try
                {
                    HttpClient httpClient = new();
                    await httpClient.GetAsync("https://review-func-app.azurewebsites.net/api/CalcRating/" + rid);
                    log.LogInformation($"CalcRating successfully executed.");
                }catch(Exception ex){
                    log.LogError($"Exception thrown while executing CalcRating: {ex.Message}");
                }
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
