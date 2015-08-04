using System;
using System.ComponentModel;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Настройка процесов работающих в фоновом режиме (биллинга)
	/// </summary>
	[Class(0, Table = "InternetSettings", NameType = typeof(InternetSettings))]
	public class InternetSettings : BaseModel
	{
		[Property, Description("Дата следующего запуска процесса")]
		public virtual DateTime NextBillingDate { get; set; }

		[Property, Description("Дата последней ошибки")]
		public virtual bool LastStartFail { get; set; }
	}
}