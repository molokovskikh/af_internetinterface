using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "clientRequest", NameType = typeof(ClientRequest))]
	public class ClientRequest : BaseModel
	{
		[Property, NotNullNotEmpty]
		public virtual string ApplicantName { get; set; }

		[Property, Min(Value = 1)]
		public virtual int ApplicantPhoneNumber { get; set; }

		[Property, Email, NotNullNotEmpty]
		public virtual string Email { get; set; }

		[Property, NotNullNotEmpty]
		public virtual string City { get; set; }

		[Property, NotNullNotEmpty]
		public virtual string Street { get; set; }

		[Property, Description("Дом"), Min(Value = 1)]
		public virtual int House { get; set; }

		[Property, Description("Корпус"), Min(Value = 1)]
		public virtual int CaseHouse { get; set; }

		[Property, Description("Квартира"), Min(Value = 1)]
		public virtual int Apartment { get; set; }

		[Property, Min(Value = 1)]
		public virtual int Floor { get; set; }

		[Property, Min(Value = 1)]
		public virtual int Entrance { get; set; }

		[ManyToOne(Column = "Tariff")]
		public virtual Tariff Tariff { get; set; }
	}
}