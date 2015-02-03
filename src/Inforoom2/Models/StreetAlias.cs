using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Inforoom2.Intefaces;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "street_alias", NameType = typeof(StreetAlias))]
	public class StreetAlias : BaseModel
	{
		public StreetAlias()
		{

		}

		[Property]
		public virtual string Name { get; set; }

		[ManyToOne(Column = "Street",  Cascade = "save-update"), NotNull]
		public virtual Street Street { get; set; }

		public static StreetAlias FindAlias(string address, ISession dbSession)
		{
			var aliases = dbSession.Query<StreetAlias>().ToList();
			foreach (var alias in aliases) {
				var str = alias.Name;
				try {
					var regex = new Regex(str);
					var result = regex.IsMatch(address);
					if (result)
						return alias;
				}
				catch (Exception) {
					//Давим, ничего не делая
				}
			}
			return null;
		}
	}
}