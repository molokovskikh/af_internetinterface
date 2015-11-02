using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Common.Web.Ui.ActiveRecordExtentions;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("Regions", Schema = "internet", Lazy = true)]
	public class RegionHouse
	{
		public RegionHouse()
		{
		}

		public RegionHouse(string name)
		{
			Name = name;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property("Region")]
		public virtual string Name { get; set; }

		[BelongsTo(Column = "_City", Cascade = CascadeEnum.SaveUpdate)]
		public virtual City City { get; set; }

		[Property]
		public virtual bool IsExternalClientIdMandatory { get; set; }
		
		[Property(Column = "_RegionOfficePhoneNumber")]
		public virtual string RegionOfficePhoneNumber { get; set; }

		public static IList<RegionHouse> All()
		{
			return ArHelper.WithSession(s => All(s));
		}

		public static IList<RegionHouse> All(ISession session)
		{
			return session.Query<RegionHouse>()
				.OrderBy(r => r.Name)
				.ToList();
		}
	}
}