using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using Inforoom2.Helpers;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель Зоны (перенесено из старой админки)
	/// </summary>
	[Class(0, Table = "NetworkZones", NameType = typeof(Zone))]
	public class Zone : BaseModel
	{
		public Zone()
		{
			Switches = new List<Switch>();
		}

		[Property, Description("Наименование зоны")]
		public virtual string Name { get; set; }
		
		[ManyToOne(Column = "RegionId")]
        public virtual Region Region { get; set; }

		[Bag(0, Table = "networkswitches")]
		[Key(1, Column = "Zone")]
		[OneToMany(2, ClassType = typeof(Switch))]
		public virtual IList<Switch> Switches { get; set; }
	}
}