using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using BaGet.Core;

namespace BaGet.Azure
{
    // Refactored to use Azure.Storage.Blobs SDK  
    public class BlobStorageService : IStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(BlobContainerClient containerClient)
        {
            _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
        }

        public async Task<Stream> GetAsync(string path, CancellationToken cancellationToken)
        {
            var blobClient = _containerClient.GetBlobClient(path);
            var response = await blobClient.DownloadAsync(cancellationToken: cancellationToken);
            return response.Value.Content;
        }

        public Task<Uri> GetDownloadUriAsync(string path, CancellationToken cancellationToken)
        {
            var blobClient = _containerClient.GetBlobClient(path);
            var expirationTime = DateTimeOffset.UtcNow.AddMinutes(10); // Expiry time can be configured as necessary  
            var sasBuilder = new BlobSasBuilder
            {
                ExpiresOn = expirationTime
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(
                _containerClient.AccountName, // assuming you store account name with the container client  
                "<your-account-key-here>")) // secure handling of the account key needed  
                .ToString();

            return Task.FromResult(new Uri($"{blobClient.Uri}?{sasToken}"));
        }

        public async Task<StoragePutResult> PutAsync(
            string path,
            Stream content,
            string contentType,
            CancellationToken cancellationToken)
        {
            var blobClient = _containerClient.GetBlobClient(path);
            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

            try
            {
                await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, conditions: null, cancellationToken: cancellationToken);
                return StoragePutResult.Success;
            }
            catch (RequestFailedException e) when (e.ErrorCode == "BlobAlreadyExists")
            {
                // This means the blob already exists, check contents for conflicts  
                var existingBlobStream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
                content.Position = 0; // Reset the stream position for comparison  

                return content.Matches(existingBlobStream) ? StoragePutResult.AlreadyExists : StoragePutResult.Conflict;
            }
        }

        public async Task DeleteAsync(string path, CancellationToken cancellationToken)
        {
            var blobClient = _containerClient.GetBlobClient(path);
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
    }
}
