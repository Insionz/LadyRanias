using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace LadyRanias.App.Components.CatImages.Impl;

internal sealed class BlobImageStore : IImageStore
{
	private BlobContainerClient _blobContainerClient;

	public BlobImageStore(BlobServiceClient blobServiceClient) : this(blobServiceClient, "cat-images")
	{
	}

	internal BlobImageStore(BlobServiceClient blobServiceClient, string blobContainerName)
	{
		_blobContainerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
		_blobContainerClient.CreateIfNotExists();
	}

	public async Task SaveAsync(string name, Stream content, string contentType, CancellationToken cancellationToken)
	{
		var blob = _blobContainerClient.GetBlobClient(name);

		var options = new BlobUploadOptions
		{
			HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
		};
		await blob.UploadAsync(content, options, cancellationToken);
	}

	public async Task<StoredImage?> OpenAsync(string name, CancellationToken cancellationToken)
	{
		var blob = _blobContainerClient.GetBlobClient(name);

		try
		{
			var content = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
			return new StoredImage(content.Value.Content, content.Value.Details.ContentType);
		}
		catch (RequestFailedException ex) when (ex.Status == 404)
		{
			return null;
		}
	}

	public async Task<bool> DeleteAsync(string name, CancellationToken cancellationToken)
	{
		var blob = _blobContainerClient.GetBlobClient(name);

		var response =await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);

		return response.Value;
	}
}
