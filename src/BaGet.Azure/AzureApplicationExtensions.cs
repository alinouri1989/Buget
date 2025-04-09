using System;
using Azure.Data.Tables;
using Azure.Search.Documents;
using Azure.Storage.Blobs;
using BaGet.Azure;
using BaGet.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace BaGet
{
    public static class AzureApplicationExtensions
    {
        public static BaGetApplication AddAzureTableDatabase(this BaGetApplication app)
        {
            app.Services.AddBaGetOptions<AzureTableOptions>(nameof(BaGetOptions.Database));

            app.Services.AddTransient<TablePackageDatabase>();
            app.Services.AddTransient<TableOperationBuilder>();
            app.Services.AddTransient<TableSearchService>();
            app.Services.TryAddTransient<IPackageDatabase>(provider => provider.GetRequiredService<TablePackageDatabase>());
            app.Services.TryAddTransient<ISearchService>(provider => provider.GetRequiredService<TableSearchService>());
            app.Services.TryAddTransient<ISearchIndexer>(provider => provider.GetRequiredService<NullSearchIndexer>());

            app.Services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<AzureTableOptions>>().Value;
                return new TableServiceClient(options.ConnectionString);
            });

            app.Services.AddTransient(provider =>
            {
                var client = provider.GetRequiredService<TableServiceClient>();
                return client.GetTableClient("YourTableName"); // Specify your table name here  
            });

            app.Services.AddProvider<IPackageDatabase>((provider, config) =>
            {
                if (!config.HasDatabaseType("AzureTable")) return null;
                return provider.GetRequiredService<TablePackageDatabase>();
            });

            app.Services.AddProvider<ISearchService>((provider, config) =>
            {
                if (!config.HasSearchType("Database")) return null;
                if (!config.HasDatabaseType("AzureTable")) return null;
                return provider.GetRequiredService<TableSearchService>();
            });

            app.Services.AddProvider<ISearchIndexer>((provider, config) =>
            {
                if (!config.HasSearchType("Database")) return null;
                if (!config.HasDatabaseType("AzureTable")) return null;
                return provider.GetRequiredService<NullSearchIndexer>();
            });

            return app;
        }

        public static BaGetApplication AddAzureTableDatabase(
            this BaGetApplication app,
            Action<AzureTableOptions> configure)
        {
            app.AddAzureTableDatabase();
            app.Services.Configure(configure);
            return app;
        }

        public static BaGetApplication AddAzureBlobStorage(this BaGetApplication app)
        {
            app.Services.AddBaGetOptions<AzureBlobStorageOptions>(nameof(BaGetOptions.Storage));
            app.Services.AddTransient<BlobStorageService>();
            app.Services.TryAddTransient<IStorageService>(provider => provider.GetRequiredService<BlobStorageService>());

            app.Services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<AzureBlobStorageOptions>>().Value;
                return new BlobServiceClient(options.ConnectionString);
            });

            app.Services.AddTransient(provider =>
            {
                var client = provider.GetRequiredService<BlobServiceClient>();
                var options = provider.GetRequiredService<IOptions<AzureBlobStorageOptions>>().Value;
                return client.GetBlobContainerClient(options.Container);
            });

            app.Services.AddProvider<IStorageService>((provider, config) =>
            {
                if (!config.HasStorageType("AzureBlobStorage")) return null;
                return provider.GetRequiredService<BlobStorageService>();
            });

            return app;
        }

        public static BaGetApplication AddAzureBlobStorage(
            this BaGetApplication app,
            Action<AzureBlobStorageOptions> configure)
        {
            app.AddAzureBlobStorage();
            app.Services.Configure(configure);
            return app;
        }

        public static BaGetApplication AddAzureSearch(this BaGetApplication app)
        {
            app.Services.AddBaGetOptions<AzureSearchOptions>(nameof(BaGetOptions.Search));

            app.Services.AddTransient<AzureSearchBatchIndexer>();
            app.Services.AddTransient<AzureSearchService>();
            app.Services.AddTransient<AzureSearchIndexer>();
            app.Services.AddTransient<IndexActionBuilder>();
            app.Services.TryAddTransient<ISearchService>(provider => provider.GetRequiredService<AzureSearchService>());
            app.Services.TryAddTransient<ISearchIndexer>(provider => provider.GetRequiredService<AzureSearchIndexer>());

            app.Services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<AzureSearchOptions>>().Value;
                return new SearchClient(options.Endpoint, options.IndexName, new Azure.AzureKeyCredential(options.ApiKey));
            });

            app.Services.AddProvider<ISearchService>((provider, config) =>
            {
                if (!config.HasSearchType("AzureSearch")) return null;
                return provider.GetRequiredService<AzureSearchService>();
            });

            app.Services.AddProvider<ISearchIndexer>((provider, config) =>
            {
                if (!config.HasSearchType("AzureSearch")) return null;
                return provider.GetRequiredService<AzureSearchIndexer>();
            });

            return app;
        }

        public static BaGetApplication AddAzureSearch(
            this BaGetApplication app,
            Action<AzureSearchOptions> configure)
        {
            app.AddAzureSearch();
            app.Services.Configure(configure);
            return app;
        }
    }
}
