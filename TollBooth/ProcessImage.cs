using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using TollBooth.Models;

namespace TollBooth
{
	public static class ProcessImage
	{
		public static HttpClient Client;

		[FunctionName("ProcessImage")]
		public static async Task Run([BlobTrigger("images/{name}", Connection = "blobStorageConnection")]
			Stream incomingPlate, string name, TraceWriter log)
		{
			var licensePlateText = string.Empty;
			// Reuse the HttpClient across calls as much as possible so as not to exhaust all available sockets on the server on which it runs.
			Client = Client ?? new HttpClient();

			log.Info($"Processing {name}");

			try
			{
				byte[] licensePlateImage;
				// Convert the incoming image stream to a byte array.
				using (var br = new BinaryReader(incomingPlate))
				{
					licensePlateImage = br.ReadBytes((int) incomingPlate.Length);
				}

				licensePlateText = await new FindLicensePlateText(log, Client).GetLicensePlate(licensePlateImage);


				// Send the details to Event Grid.
				await new SendToEventGrid(log, Client).SendLicensePlateData(new LicensePlateData
				{
					FileName = name,
					LicensePlateText = licensePlateText,
					TimeStamp = DateTime.UtcNow
				});
			}
			catch (Exception e)
			{
				log.Error(e.Message);
			}

			log.Info($"Finished processing. Detected the following license plate: {licensePlateText}");
		}
	}
}