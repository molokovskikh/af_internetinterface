using System;
using System.ComponentModel;
using System.Linq;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель услуги "Аренда оборудования"
	/// </summary>
	[Class(0, Table = "ClientRentalHardware", NameType = typeof(ClientRentalHardware))]
	public class ClientRentalHardware: BaseModel
	{
		[ManyToOne(NotNull = true), Description("Тип оборудования")]
		public virtual RentalHardware Hardware { get; set; }

		[ManyToOne(NotNull = true, Cascade = "save-update"), Description("Модель оборудования")]
		public virtual HardwareModel Model { get; set; }

		[ManyToOne(NotNull = true), Description("Арендующий клиент")]
		public virtual Client Client { get; set; }

		[Property, Description("Дата регистрации оборудования")]
		public virtual DateTime? BeginDate { get; set; }

		[Property, Description("Дата снятия с регистрации/возврата оборудования")]
		public virtual DateTime? EndDate { get; set; }

		[Property(Column = "Active"), NotNull, Description("Индикатор действующей аренды")]
		public virtual bool IsActive { get; set; }

		[ManyToOne, Description("Сотрудник, регистрирующий аренду")]
		public virtual Employee Employee { get; set; }

		[Property, Description("Дата фактической выдачи оборудования")]
		public virtual DateTime? GiveDate { get; set; }

		[Property(Column = "Used"), NotNull, Description("Оборудование, бывшее в употреблении")]
		public virtual bool WasUsed { get; set; }

		[Property(Column = "CompleteSet"), NotNull, Description("Оборудование в полной комплектации")]
		public virtual bool IsCompleteSet { get; set; }

		[Property, Description("Поле комментария для сотрудника")]
		public virtual string Comment { get; set; }

		// Вспомогательное поле "Модель" для хранения во View
		[NotNullNotEmpty(Message = "Укажите модель")]
		public virtual string ModelName { get; set; }

		// Вспомогательное поле "Серийный номер" для хранения во View
		[NotNullNotEmpty(Message = "Укажите серийный номер")]
		public virtual string SerialNumber { get; set; }

		// Метод активации "Аренды оборудования"
		public virtual string Activate(ISession session, Employee employee = null, string comment = "")
		{
			Model = session.Query<HardwareModel>().ToList()
				.FirstOrDefault(m => m.Hardware == Hardware && m.Name == ModelName && m.SerialNumber == SerialNumber);
			if (Model == null) {
				Model = new HardwareModel {
					Hardware = Hardware,
					Name = ModelName,
					SerialNumber = SerialNumber
				};
			}
			BeginDate = SystemTime.Now();
			IsActive = true;
			Employee = employee;
			if (!string.IsNullOrEmpty(comment))
				Comment = comment;
			return String.Format("Услуга \"Аренда оборудования типа \"{0}\" активирована", Hardware.Name);
		}

		// Метод деактивации "Аренды оборудования"
		public virtual string Deactivate(string comment = "")
		{
			EndDate = SystemTime.Now();
			IsActive = false;
			if (!string.IsNullOrEmpty(comment))
				Comment += "; " + comment;
			return String.Format("Услуга \"Аренда оборудования типа \"{0}\" деактивирована", Hardware.Name);
		}
	}
}
