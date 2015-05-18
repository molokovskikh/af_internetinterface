using System;
using System.ComponentModel;
using System.Net;
using Inforoom2.Helpers;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{


	[Class(0, Table = "TvProtocols", NameType = typeof(TvProtocol))]
	public class TvProtocol : BaseModel
	{
		[Property]
		public virtual string Name { get; set; }

	}
}