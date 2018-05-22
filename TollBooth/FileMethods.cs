using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using TollBooth.Models;

namespace TollBooth
{
	internal class FileMethods
	{
		private readonly CloudBlobClient blobClient;
		private readonly string blobStorageConnection = ConfigurationManager.AppSettings["blobStorageConnection"];
		private readonly string containerName = ConfigurationManager.AppSettings["exportCsvContainerName"];
		private readonly TraceWriter log;

		public FileMethods(TraceWriter log)
		{
			this.log = log;
			// Retrieve storage account information from connection string.
			var storageAccount = CloudStorageAccount.Parse(blobStorageConnection);

			// Create a blob client for interacting with the blob service.
			blobClient = storageAccount.CreateCloudBlobClient();
		}

		public async Task<bool> GenerateAndSaveCsv(IEnumerable<LicensePlateDataDocument> licensePlates)
		{
			bool successful;

			log.Info("Generating CSV file");
			var blobName = $"{DateTime.UtcNow:s}.csv";

			using (var stream = new MemoryStream())
			{
				using (var textWriter = new StreamWriter(stream))
				{
					using (var csv = new CsvWriter(textWriter))
					{
						csv.Configuration.Delimiter = ",";
						csv.WriteRecords(licensePlates.Select(ToLicensePlateData));
						await textWriter.FlushAsync();

						log.Info($"Beginning file upload: {blobName}");
						try
						{
							var container = blobClient.GetContainerReference(containerName);

							// Retrieve reference to a blob.
							var blob = container.GetBlockBlobReference(blobName);
							await container.CreateIfNotExistsAsync();

							// Upload blob.
							stream.Position = 0;

							await blob.UploadFromStreamAsync(stream);

							successful = true;
						}
						catch (Exception e)
						{
							log.Error($"Could not upload CSV file: {e.Message}", e);
							successful = false;
						}
					}
				}
			}

			return successful;
		}

		/// <summary>
		///     Used for mapping from a LicensePlateDataDocument object to a LicensePlateData object.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		private static LicensePlateData ToLicensePlateData(LicensePlateDataDocument source)
		{
			return new LicensePlateData
			{
				FileName = source.FileName,
				LicensePlateText = source.LicensePlateText,
				TimeStamp = source.Timestamp
			};
		}
	}
}