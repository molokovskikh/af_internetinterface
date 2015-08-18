using System;
using System.ComponentModel;
using System.Net;
using Inforoom2.Helpers;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель протокола TV-каналов
	/// </summary>
	[Class(0, Table = "TvProtocols", NameType = typeof(TvProtocol))]
	public class TvProtocol : BaseModel
	{
		[Property, Description("Наименование TV-протокола")]
		public virtual string Name { get; set; }

	}
}