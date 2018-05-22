using System;
using Microsoft.Azure.Documents;

namespace TollBooth.Models
{
	/// <summary>
	///     LicensePlateDataDocument extends the Microsoft.Azure.Documents.Resource class,
	///     providing us access to internal properties of a Resource such as ETag, SelfLink, Id etc.
	///     When working with objects extending from Resource you get the benefit of not having to
	///     dynamically cast between Document and your POCO.
	/// </summary>
	public class LicensePlateDataDocument : Resource
	{
		public string FileName { get; set; }
		public string LicensePlateText { get; set; }
		public DateTime TimeStamp { get; set; }
		public bool LicensePlateFound { get; set; }
		public bool Exported { get; set; }
	}
}