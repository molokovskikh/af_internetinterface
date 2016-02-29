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
	[Class(0, Table = "Labels", NameType = typeof(ConnectionRequestMarker))]
	public class ConnectionRequestMarker : BaseModel
	{
		[Property, NotNullNotEmpty(Message = "Название маркера не задано")]
		public virtual string Name { get; set; }

		[Property, NotNullNotEmpty(Message = "Цвет маркера не задан")]
		public virtual string Color { get; set; }

		[Property]
		public virtual bool Deleted { get; set; }

		[Property]
		public virtual string ShortComment { get; set; }
	}
}