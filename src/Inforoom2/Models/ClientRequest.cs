using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NHibernate.Engine;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "Requests", NameType = typeof(ClientRequest))]
	public class ClientRequest : BaseModel
	{
		[Property, NotNullNotEmpty]
		public virtual string ApplicantName { get; set; }

		[Property, NotNullNotEmpty]
		public virtual int ApplicantPhoneNumber { get; set; }
		
		[Property(Column = "ApplicantEmail"), Email]
		public virtual string Email { get; set; }

		[ManyToOne(Column = "_Address")]
		public virtual Address Address { get; set; }

		[ManyToOne(Column = "Tariff"), NotNull]
		public virtual Plan Plan { get; set; }

		[Property]
		public virtual bool SelfConnect { get; set; }



		//Поля старой модели заявки

		[Property, NotNullNotEmpty]
		public virtual string City { get; set; }
		
		[Property, NotNullNotEmpty]
		public virtual string Street { get; set; }

		[Property, Min(1)]
		public virtual int? House { get; set; }
		
		[Property]
		public virtual string CaseHouse { get; set; }

		[Property]
		public virtual int? Apartment { get; set; }
		
		[Property]
		public virtual int? Entrance { get; set; }
		
		[Property]
		public virtual int? Floor { get; set; }

		[Property]
		public virtual DateTime ActionDate { get; set; }
		
		[Property]
		public virtual DateTime RegDate { get; set; }
	}
}