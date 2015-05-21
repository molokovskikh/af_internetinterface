using System;
using System.ComponentModel;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "MessagesForClients", Schema = "internet", NameType = typeof(PrivateMessage))]
	public class PrivateMessage : BaseModel
	{
		[Property, Description("Текст сообщения"), NotNullNotEmpty(Message = "Введите сообщение")]
		public virtual string Text { get; set; }

		[Property(NotNull = true), Description("Показывать")]
		public virtual bool Enabled { get; set; }

		[Property(NotNull = true), Description("Дата регистрации сообщения")]
		public virtual DateTime RegDate { get; set; }

		[Property(NotNull = true), Description("Дата отключения сообщения со страницы клиента")]
		public virtual DateTime EndDate { get; set; }

		[ManyToOne]
		public virtual Client Client { get; set; }

		[ManyToOne]
		public virtual Employee Registrator { get; set; }
	}
}
