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
	[Class(0, Table = "siteVersionChanges", NameType = typeof(SiteVersionChange))]
	public class SiteVersionChange : BaseModel
	{
		[Property, NotEmpty(Message = "Введите номер версии"), Description("Версия")]
		public virtual string Version { get; set; }
		[Property, NotEmpty(Message = "Введите описание изменений"), Description("Изменения")]
		public virtual string Changes { get; set; }
	}
}