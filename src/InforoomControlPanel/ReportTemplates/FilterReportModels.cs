using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Inforoom2.Components;

namespace InforoomControlPanel.ReportTemplates
{
	public class FilterInternetsessionsLog
	{
		public uint Id { get; set; }
		public uint EndpointId { get; set; }
		public uint IP { get; set; }
		public string HwId { get; set; }
		public DateTime LeaseBegin { get; set; }
		public DateTime? LeaseEnd { get; set; }
	}

	public class FilterAppeal
	{
		public uint Id { get; set; }
		public string Message { get; set; }
		public DateTime Date { get; set; }
		public uint Employee { get; set; }
		public uint Client { get; set; }
		public uint AppealType { get; set; }
		public int inforoom2 { get; set; }
	}
}