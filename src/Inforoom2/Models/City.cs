using System.Globalization;
using System.Web.Services.Description;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "city", NameType = typeof(City))]
	public class City : BaseModel
	{
		[Property, NotEmpty(Message = "Введите название города")]
		public virtual string Name { get; set; }
	}
}