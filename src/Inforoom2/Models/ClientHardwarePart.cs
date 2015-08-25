using System;
using System.ComponentModel;
using Common.Tools;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Комплектация арендуемого оборудования (при аренде)
	/// </summary>
	[Class(0, Table = "ClientHardwareParts", NameType = typeof(ClientHardwarePart))]
	public class ClientHardwarePart : BaseModel
	{
		[ManyToOne(Column = "PartId", Cascade = "save-update"), Description("Комплектация")]
		public virtual HardwarePart Part { get; set; }

		[ManyToOne(Column = "ClientRentId", Cascade = "save-update"), Description("Аренда клиента")]
		public virtual ClientRentalHardware ClientRentalHardware { get; set; }

		[Property]
		public virtual bool Absent { get; set; }

		[Property]
		public virtual bool NotGiven { get; set; }
		
	}
}