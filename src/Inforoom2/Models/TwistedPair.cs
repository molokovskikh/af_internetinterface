using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using Inforoom2.Helpers;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель многопарника
	/// </summary>
	[Class(0, Table = "twisted_pairs", NameType = typeof(TwistedPair))]
	public class TwistedPair : BaseModel
	{
		[Property, Description("Количество портов")]
		public virtual int PairCount { get; set; }

		[ManyToOne]
		public virtual NetworkNode NetworkNode { get; set; }

		public virtual List<int> GetAvailiblePairCounts()
		{
			var arr = new List<int> { 16, 20, 25 };
			return arr;
		}
	}
}