using System;
using System.Linq;
using Inforoom2.Models.Services;
using NHibernate;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "Access_Right", NameType = typeof(AccessRight))]
	public class AccessRight : BaseModel
	{
		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string Description { get; set; }
	}
}
