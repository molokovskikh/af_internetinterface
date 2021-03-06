﻿using System;
using System.ComponentModel;
using Common.Tools;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	public enum LogEventType
	{
		[Description("Добавление")] Insert,
		[Description("Обновление")] Update,
		[Description("Удаление")] Delete
	}

	/// <summary>
	/// Логирование действий связанных с моделями
	/// </summary>
	[Class(0, Table = "logs", NameType = typeof(Log))]
	public class Log : BaseModel
	{
		public Log()
		{
			Date = SystemTime.Now();
		}

		[Description("Сообщение лога")]
		[Property]
		public virtual string Message { get; set; }

		[Description("Дата измененения")]
		[Property]
		public virtual DateTime Date { get; set; }

		[Description("Инициатор события")]
		[ManyToOne(Column = "Employee")]
		public virtual Employee Employee { get; set; }

		[Description("Тип события, повлекшего действие связанное с моделью")]
		[Property]
		public virtual LogEventType Type { get; set; }

		[Description("Название модели")]
		[Property]
		public virtual string ModelClass { get; set; }

		[Description("Id модели")]
		[Property]
		public virtual int ModelId { get; set; }
	}
}