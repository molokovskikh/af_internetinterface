using System;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "InternetSettings", NameType = typeof(InternetSettings))]
	public class InternetSettings : BaseModel
	{
		[Property]
		public virtual DateTime NextBillingDate { get; set; }

		[Property]
		public virtual bool LastStartFail { get; set; }
	}
}