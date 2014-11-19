using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NHibernate.Engine;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "clientRequest", NameType = typeof(ClientRequest))]
	public class ClientRequest : BaseModel
	{
		[Property, NotNullNotEmpty]
		public virtual string ApplicantName { get; set; }

		[Property]
		public virtual int ApplicantPhoneNumber { get; set; }

		[Property, Email, NotNullNotEmpty]
		public virtual string Email { get; set; }

		[ManyToOne(Column = "Address")]
		public virtual Address Address { get; set; }

		[ManyToOne(Column = "Plan")]
		public virtual Plan Plan { get; set; }
	}
}