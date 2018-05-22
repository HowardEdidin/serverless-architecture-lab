using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs.Host;
using TollBooth.Models;

namespace TollBooth
{
	internal class DatabaseMethods
	{
		private readonly string authorizationKey = ConfigurationManager.AppSettings["cosmosDBAuthorizationKey"];
		private readonly string collectionId = ConfigurationManager.AppSettings["cosmosDBCollectionId"];
		private readonly string databaseId = ConfigurationManager.AppSettings["cosmosDBDatabaseId"];
		private readonly string endpointUrl = ConfigurationManager.AppSettings["cosmosDBEndPointUrl"];

		private readonly TraceWriter log;

		// Reusable instance of DocumentClient which represents the connection to a Cosmos DB endpoint.
		private DocumentClient client;

		public DatabaseMethods(TraceWriter log)
		{
			this.log = log;
		}

		/// <summary>
		///     Retrieves all license plate records (documents) that have not yet been exported.
		/// </summary>
		/// <returns></returns>
		public List<LicensePlateDataDocument> GetLicensePlatesToExport()
		{
			log.Info("Retrieving license plates to export");
			var exportedCount = 0;
			var collectionLink = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
			List<LicensePlateDataDocument> licensePlates;

			using (client = new DocumentClient(new Uri(endpointUrl), authorizationKey))
			{
				// MaxItemCount value tells the document query to retrieve 100 documents at a time until all are returned.
				licensePlates = client.CreateDocumentQuery<LicensePlateDataDocument>(collectionLink,
					new FeedOptions {MaxItemCount = 100}).Where(l => l.Exported == false).ToList();

			}

			exportedCount = licensePlates.Count();
			log.Info($"{exportedCount} license plates found that are ready for export");
			return licensePlates;
		}

		/// <summary>
		///     Updates license plate records (documents) as exported. Call after successfully
		///     exporting the passed in license plates.
		///     In a production environment, it would be best to create a stored procedure that
		///     bulk updates the set of documents, vastly reducing the number of transactions.
		/// </summary>
		/// <param name="licensePlates"></param>
		/// <returns></returns>
		public async Task MarkLicensePlatesAsExported(IEnumerable<LicensePlateDataDocument> licensePlates)
		{
			log.Info("Updating license plate documents exported values to true");
			var collectionLink = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);

			using (client = new DocumentClient(new Uri(endpointUrl), authorizationKey))
			{
				foreach (var licensePlate in licensePlates)
				{
					licensePlate.Exported = true;
					var response =
						await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(databaseId, collectionId, licensePlate.Id),
							licensePlate);

					var updated = response.Resource;
					//_log.Info($"Exported value of updated document: {updated.GetPropertyValue<bool>("exported")}");
				}
			}
		}
	}
}