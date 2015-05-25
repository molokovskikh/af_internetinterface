using System;
using System.ComponentModel;
using Castle.ActiveRecord;
using Castle.Components.Validator;

namespace InternetInterface.Models
{
	/// <summary>
	/// Модель услуги "Аренда оборудования"
	/// </summary>
	[ActiveRecord("inforoom2_clientrentalhardware", Schema = "Internet", Lazy = true)]
	public class ClientRentalHardware
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo(NotNull = true), Description("Тип оборудования")]
		public virtual RentalHardware Hardware { get; set; }

		[BelongsTo(NotNull = true, Cascade = CascadeEnum.SaveUpdate), Description("Модель оборудования")]
		public virtual HardwareModel Model { get; set; }

		[BelongsTo(NotNull = true), Description("Арендующий клиент")]
		public virtual Client Client { get; set; }

		[Property, Description("Дата регистрации оборудования")]
		public virtual DateTime? BeginDate { get; set; }

		[Property, Description("Дата снятия с регистрации/возврата оборудования")]
		public virtual DateTime? EndDate { get; set; }

		[Property(Column = "Active", NotNull = true), Description("Индикатор действующей аренды")]
		public virtual bool IsActive { get; set; }

		[BelongsTo(Column = "Employee"), Description("Сотрудник, регистрирующий аренду")]
		public virtual Partner Partner { get; set; }

		[Property, Description("Дата фактической выдачи оборудования")]
		public virtual DateTime? GiveDate { get; set; }

		[Property, Description("Поле комментария для сотрудника")]
		public virtual string Comment { get; set; }

		// Вспомогательное поле "Модель" для хранения во View
		[ValidateNonEmpty("Укажите модель")]
		public virtual string ModelName { get; set; }

		// Вспомогательное поле "Серийный номер" для хранения во View
		[ValidateNonEmpty("Укажите серийный номер")]
		public virtual string SerialNumber { get; set; }

		// Метод активации "Аренды оборудования"
		public virtual string Activate(string comment = "")
		{
			Model = new HardwareModel {
				Hardware = Hardware,
				Name = ModelName,
				SerialNumber = SerialNumber
			};
			BeginDate = DateTime.Now;
			GiveDate = DateTime.Now;
			IsActive = true;
			Partner = Partner.GetInitPartner();
			Comment = comment;
			return String.Format("Услуга \"Аренда оборудования типа \"{0}\" активирована", Hardware.Name);
		}

		// Метод деактивации "Аренды оборудования"
		public virtual string Deactivate(string comment = "")
		{
			EndDate = DateTime.Now;
			IsActive = false;
			Comment += comment;
			return String.Format("Услуга \"Аренда оборудования типа \"{0}\" деактивирована", Hardware.Name);
		}
	}
}
