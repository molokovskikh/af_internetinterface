using System;
using System.Collections.Generic;
using System.ComponentModel;
using Inforoom2.Components;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель улицы
	/// </summary>
	[Class(0, Table = "RequestMessages", NameType = typeof(ConnectionRequestComment)), Description("Комментарий к заявке на подключение")]
	public class ConnectionRequestComment : BaseModel
	{
		[Property]
		public virtual string Comment { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		[ManyToOne(Column = "Registrator"), Description("Автор регистрации заявки")]
		public virtual Employee Registrator { get; set; }

		[ManyToOne(Column = "Request")]
		public virtual ClientRequest Request { get; set; }
	}
}