using System;
using System.Collections.Generic;
using System.ComponentModel; 
using System.Linq;
using System.Web;
using Inforoom2.Intefaces;
using InternetInterface.Models;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;
using Inforoom2.Models;

namespace Inforoom2.Models
{
	/// <summary>
	/// Инженеры
	/// </summary>
	[Class(0, Table = "ServiceMen", NameType = typeof(ServiceMan))]
	public class ServiceMan : BaseModel
	{
		public ServiceMan()
		{
			SheduleItems = new List<ServicemenScheduleItem>();
		}

		public ServiceMan(Employee employee) : this()
		{
			Employee = employee;
		}

		[Description("Работник")]
		[ManyToOne(Column = "Employee"), NotNull]
		public virtual Employee Employee { get; set; }

		[Description("Регион")]
		[ManyToOne(Column = "Region"), NotNull]
		public virtual Region Region { get; set; }

		[Description("Графа в расписании")]
		[Bag(0, Table = "ServicemenScheduleItems")]
		[Key(1, Column = "Serviceman")]
		[OneToMany(2, ClassType = typeof(ServicemenScheduleItem))]
		public virtual IList<ServicemenScheduleItem> SheduleItems { get; set; }
	}

	[Class(0, Table = "ConnectBrigads", NameType = typeof(ServiceTeam))]
	public class ServiceTeam : BaseModel
	{
		public ServiceTeam()
		{
			Disabled = false;
		}

		public ServiceTeam(Region region)
			: this()
		{
			Region = region;
		}

		[Property(Column = "Name"), NotNullNotEmpty]
		public virtual string Name { get; set; }

		[Property(Column = "IsDisabled"), NotNullNotEmpty]
		public virtual bool Disabled { get; set; }

		[ManyToOne(Column = "Region"), NotNull]
		public virtual Region Region { get; set; }
	}
}