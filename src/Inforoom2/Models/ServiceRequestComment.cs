using System;
using System.Globalization;
using System.Web.Services.Description;
using Common.Tools;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "ServiceIterations", NameType = typeof(ServiceRequestComment))]
	public class ServiceRequestComment : BaseModel
	{
		public ServiceRequestComment()
		{
			CreationDate = SystemTime.Now();
		} 

		[Property(Column = "Description"),NotNullNotEmpty(Message = "Поле должно быть заполнено.")]
		public virtual string Comment { get; set; }

		[Property(Column = "RegDate")]
		public virtual DateTime CreationDate { get; set; }

		[ManyToOne(Column = "Performer")]
		public virtual Employee Author { get; set; }

		[ManyToOne(Column = "Request")]
		public virtual ServiceRequest ServiceRequest { get; set; }

	}
}