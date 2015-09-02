using System;
using System.ComponentModel;
using Common.Tools;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Пользовательское денежное списание
	/// </summary>
	[Class(0, Table = "UserWriteOffs", Schema = "internet", NameType = typeof(UserWriteOff))]
	public class UserWriteOff : BaseModel
	{
		public UserWriteOff(Client client, decimal sum, string comment)
		{
			Client = client;
			Sum = sum;
			Comment = comment;
			Date = SystemTime.Now();
		}

		public UserWriteOff()
		{ 
		}

		[ManyToOne(Column = "Client")]
		public virtual Client Client { get; set; }

		[Property, Description("Дата денежного списания")]
		public virtual DateTime Date { get; set; }

		[Property, Description("Сумма денежного списания")]
		public virtual decimal Sum { get; set; }

		[Property, NotEmpty, Description("Комментарий к денежному списанию")]
		public virtual string Comment { get; set; }

		[Property(Column = "BillingAccount"), Description("Отметка о том,что обработано биллингом ")]
		public virtual bool IsProcessedByBilling { get; set; }

		[ManyToOne(Column = "Registrator")]
		public virtual Employee Employee { get; set; }
	}
}