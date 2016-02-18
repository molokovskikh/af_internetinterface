using System;
using System.ComponentModel;
using System.Globalization;
using System.Web.Services.Description;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель города
	/// </summary>
	[Class(0, Table = "PaymentsForAgent", NameType = typeof(PaymentsForEmployee))]
	public class PaymentsForEmployee : BaseModel
	{

		[ManyToOne(Column = "Agent", Cascade = "save-update"), NotNull]
		public virtual Employee Employee { get; set; }

		[Property]
		public virtual decimal Sum { get; set; }

		[Property]
		public virtual string Comment { get; set; }
		[Property]
		public virtual DateTime RegistrationDate { get; set; }

		[ManyToOne(Column = "Action", Cascade = "save-update"), NotNull]
		public virtual EmployeeTariff Tariff { get; set; }
	}
}