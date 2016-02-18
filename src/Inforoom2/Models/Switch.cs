using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using Inforoom2.Helpers;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель коммутатора
	/// </summary>
	[Class(0, Table = "NetworkSwitches", NameType = typeof(Switch))]
	public class Switch : BaseModel
	{
		[Property, Description("Наименование коммутатора"),NotNullNotEmpty(Message = "Укажите наименование коммутатора")]
		public virtual string Name { get; set; }

		[Property(Column = "Comment"), Description("Описание коммутатора")]
		public virtual string Description { get; set; }

		[Property, Description("Mac-адрес коммутатора"), NotNullNotEmpty(Message = "Укажите mac-адрес коммутатора")]
		public virtual string Mac { get; set; }

		[Property(Column = "TotalPorts"), Description("Кол-во портов"), NotNull(Message = "Укажите кол-во портов коммутатора")]
		public virtual int PortCount { get; set; }

		[Property]
		public virtual SwitchType Type { get; set; }

		[Property(Column = "Ip", TypeType = typeof(IPUserType)), Description("Ip-адрес коммутатора"), NotNull(Message = "Укажите ip-адрес коммутатора")]
		public virtual IPAddress Ip { get; set; }

		[Bag(0, Table = "clientendpoints", Cascade = "all-delete-orphan")]
		[Key(1, Column = "Switch")]
		[OneToMany(2, ClassType = typeof(ClientEndpoint))]
		public virtual IList<ClientEndpoint> Endpoints { get; set; }

		[ManyToOne(Cascade = "save-update")]
		public virtual NetworkNode NetworkNode { get; set; }

		public Switch()
		{
			Endpoints = new List<ClientEndpoint>();
		}
		[ManyToOne(Cascade = "save-update")]
		public virtual Zone Zone { get; set; }
	}
	public enum SwitchType
	{
		[Description("Неизвестный")]
		Unknown = 0,
		[Description("Каталист")]
		Catalyst = 1,
		[Description("Линксис")]
		Linksys = 2,
		[Description("Д-линк")]
		Dlink = 3,
		[Description("Хуавей")]
		Huawei = 4
	}
}