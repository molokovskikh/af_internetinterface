using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель узла связи
	/// </summary>
	[Class(Table = "network_nodes")]
	public class NetworkNode : BaseModel
	{
		public NetworkNode()
		{
			Virtual = false;
			Switches = new List<Switch>();
			TwistedPairs = new List<TwistedPair>();
			Addresses = new List<SwitchAddress>();
		}

		[Property, Description("Наименование узла связи")]
		public virtual string Name { get; set; }

		[Property, Description("Маркер обозначающий существует ли узел связи физически или отмечен как виртуальный для связи коммутаторов")]
		public virtual bool Virtual { get; set; }

		[Property, Description("Комментарии к узлу связи")]
		public virtual string Description { get; set; }

		[Bag(0, Table = "NetworkSwitches", Cascade = "save-update")]
		[Key(1, Column = "networknode")]
		[OneToMany(2, ClassType = typeof(Switch))]
		public virtual IList<Switch> Switches { get; set; }

		[Bag(0, Table = "TwistedPairs", Cascade = "all-delete-orphan")]
		[Key(1, Column = "networknode")]
		[OneToMany(2, ClassType = typeof(TwistedPair))]
		public virtual IList<TwistedPair> TwistedPairs { get; set; }

		[Bag(0, Table = "switchaddress", Cascade = "all-delete-orphan")]
		[Key(1, Column = "NetworkNode")]
		[OneToMany(2, ClassType = typeof(SwitchAddress))]
		public virtual IList<SwitchAddress> Addresses { get; set; }

		public virtual bool HasAddress()
		{
			return Addresses.FirstOrDefault() != null;
		}
	}
}