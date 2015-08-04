using System;
using System.ComponentModel;
using Common.Tools;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	public enum AppealType
	{
		[Description("Все")] All = 0,
		[Description("Пользовательские")] User = 1,
		[Description("Системные")] System = 3,
		[Description("Внешние")] FeedBack = 5,
		[Description("Статистические")] Statistic = 7,
	}

	/// <summary>
	/// Сообщение о пользователе для сотрудников техподдержки
	/// </summary>
	[Class(0, Table = "Appeals", NameType = typeof(Appeal))]
	public class Appeal : BaseModel
	{
		public Appeal()
		{
			Date = SystemTime.Now();
			inforoom2 = true;
		}

		public Appeal(string appeal, Client client, AppealType appealType) : this()
		{
			Message = appeal;
			Client = client;
			AppealType = appealType;
		}

		[Property(Column = "Appeal"), Description("Tекст информационного сообщения для сотрудника техподдержки")]
		public virtual string Message { get; set; }

		[Property, Description("Дата создания сообщения о пользователе для сотрудника техподдержки")]
		public virtual DateTime Date { get; set; }

		[ManyToOne(Column = "Partner")]
		public virtual Employee Employee { get; set; }

		[ManyToOne(Column = "Client")]
		public virtual Client Client { get; set; }

		[Property, Description("Тип сообщения о пользователе для сотрудников техподдержки")]
		public virtual AppealType AppealType { get; set; }

		[Property(Column = "_Inforoom2")]
		public virtual bool inforoom2 { get; set; }
	}
}