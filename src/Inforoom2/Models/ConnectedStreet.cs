using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Web.Services.Description;
using Common.Tools;
using Inforoom2.Intefaces;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель города
	/// </summary>
	[Class(0, Table = "ConnectedStreets", NameType = typeof(ConnectedStreet))]
	public class ConnectedStreet : BaseModel
	{
		public ConnectedStreet()
		{
			HouseList = new List<ConnectedHouse>();
		}

		[ManyToOne(Cascade = "save-update"), NotNull(Message = "Поле должно быть заполнено"), Description("Регион")]
		public virtual Region Region { get; set; }

		[ManyToOne(Cascade = "save-update"), Description("Улица")]
		public virtual Street AddressStreet { get; set; }

		[Property, NotNullNotEmpty(Message = "Поле должно быть заполнено"), Description("Номер дома")]
		public virtual string Name { get; set; }

		[Property]
		public virtual bool Disabled { get; set; }
		
		[Bag(0, Table = "connectedhouses")]
		[Key(1, Column = "Street")]
		[OneToMany(2, ClassType = typeof(ConnectedHouse))]
		public virtual IList<ConnectedHouse> HouseList { get; set; }
		
	}
}