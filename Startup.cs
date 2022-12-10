using System;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Review;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Review;
public class Startup : FunctionsStartup
{
    private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
        .SetBasePath(Environment.CurrentDirectory)
        .AddJsonFile("appsettings.json", true)
        .AddEnvironmentVariables()
        .Build();

    public override void Configure(IFunctionsHostBuilder builder)
    {
        var connString = Environment.GetEnvironmentVariable("CosmosDBConnectionString");

        builder.Services.AddSingleton(s =>
        {

            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException(
                    "Please specify a valid CosmosDBConnection in the appSettings.json file or your Azure Functions Settings.");
            }

            CosmosClientBuilder cosmosClientBuilder = new CosmosClientBuilder(connString);

            return cosmosClientBuilder.WithConnectionModeDirect()
                .WithBulkExecution(true)
                .Build();
        });
    }
}