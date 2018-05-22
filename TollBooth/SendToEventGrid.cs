using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using TollBooth.Models;

namespace TollBooth
{
	public class SendToEventGrid
	{
		private readonly HttpClient client;
		private readonly TraceWriter log;

		public SendToEventGrid(TraceWriter log, HttpClient client)
		{
			this.log = log;
			this.client = client;
		}

		public async Task SendLicensePlateData(LicensePlateData data)
		{
			// Will send to one of two routes, depending on success.
			// Event listeners will filter and act on events they need to
			// process (save to database, move to manual checkup queue, etc.)
			if (data.LicensePlateFound)
				await Send("savePlateData", "TollBooth/CustomerService", data);
			else
				await Send("queuePlateForManualCheckup", "TollBooth/CustomerService", data);
		}

		private async Task Send(string eventType, string subject, LicensePlateData data)
		{
			// Get the API URL and the API key from settings.
			var uri = ConfigurationManager.AppSettings["eventGridTopicEndpoint"];
			var key = ConfigurationManager.AppSettings["eventGridTopicKey"];

			log.Info($"Sending license plate data to the {eventType} Event Grid type");

			var events = new List<Event<LicensePlateData>>
			{
				new Event<LicensePlateData>
				{
					Data = data,
					EventTime = DateTime.UtcNow,
					EventType = eventType,
					Id = Guid.NewGuid().ToString(),
					Subject = subject
				}
			};

			client.DefaultRequestHeaders.Clear();
			client.DefaultRequestHeaders.Add("aeg-sas-key", key);
			await client.PostAsJsonAsync(uri, events);

			log.Info($"Sent the following to the Event Grid topic: {events[0]}");
		}
	}
}