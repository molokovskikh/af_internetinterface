using System;
using System.Text.RegularExpressions;
using InternetInterface.Models;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{

	[Class(0, Table = "PackageSpeed", NameType = typeof(PackageSpeed))]
	public class PackageSpeed : BaseModel
	{

		[Property]
		public virtual int PackageId { get; set; }

		[Property]
		public virtual int Speed { get;  set; }

		public virtual int GetSpeed()
		{
			return Speed == 0 ? 0 : Speed / 1000000;
		}
	}
}