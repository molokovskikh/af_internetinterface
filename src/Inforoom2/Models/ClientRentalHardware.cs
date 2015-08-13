using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель услуги "Аренда оборудования" (по сути это Аренда или арендуемое оборудование)
	/// </summary>
	[Class(0, Table = "ClientRentalHardware", NameType = typeof(ClientRentalHardware))]
	public class ClientRentalHardware : BaseModel
	{
		public ClientRentalHardware()
		{
			ClientHardwareParts = new List<ClientHardwarePart>();
		}

		[ManyToOne(NotNull = true), Description("Тип оборудования")]
		public virtual RentalHardware Hardware { get; set; }

		[Property, NotNullNotEmpty(Message = "Укажите название модели")]
		public virtual string Name { get; set; }

		[Property, NotNullNotEmpty(Message = "Укажите серийный номер")]
		public virtual string SerialNumber { get; set; }

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

		//TODO: REMOVE. Поле нигде не используется
		[Property(Column = "CompleteSet"), NotNull, Description("Оборудование в полной комплектации")]
		public virtual bool IsCompleteSet { get; set; }

		[Property, Description("Поле комментария для сотрудника")]
		public virtual string Comment { get; set; }

		[Property, Description("Причина диактивации")]
		public virtual string DeactivateComment { get; set; }

		[Bag(0, Table = "ClientHardwareParts", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "ClientRentId")]
		[OneToMany(2, ClassType = typeof(ClientHardwarePart)), Description("Комплектация")]
		public virtual IList<ClientHardwarePart> ClientHardwareParts { get; set; }

		// Метод активации "Аренды оборудования"
		public virtual void Activate(ISession dbSession, Employee employee)
		{
			BeginDate = SystemTime.Now();
			IsActive = true;
			Employee = employee;

			// 1-ое обращение - об активации услуги
			var message = String.Format("Услуга \"Аренда оборудования типа \"{0}\" активирована", Hardware.Name);
			var appeal = new Appeal(message, Client, AppealType.User) {
				Employee = employee
			};
			dbSession.Save(appeal);

			// 2-ое обращение - об арендуемом оборудовании
			message = string.Format("Клиент арендовал \"{0}\", модель \"{1}\", S/N {2}", Hardware.Name, Name, SerialNumber);
			appeal = new Appeal(message, Client, AppealType.User) {
				Employee = employee
			};
			dbSession.Save(appeal);
		}

		// Метод деактивации "Аренды оборудования"
		public virtual void Deactivate(ISession dbSession, Employee employee)
		{
			EndDate = SystemTime.Now();
			IsActive = false;
			dbSession.Update(this);
			//сообщенин о деактивации
			var message = String.Format("Услуга \"Аренда оборудования типа \"{0}\" (<a href='{1}/HardwareRent/UpdateHardwareRent/{2}'>{2}</a>) деактивирована,<br/> модель \"{3}\", S/N {4}.<br/>Возвращен {5}",
				Hardware.Name, ConfigHelper.GetParam("adminPanelNew"), Id, Name, SerialNumber, GetAbsentPartsOfRentedHardware());
			var appeal = new Appeal(message, Client, AppealType.User) {
				Employee = employee
			};
			dbSession.Save(appeal);
		}

		//Комментарий к деактивации "Аренды оборудования"
		public virtual void DeactivateCommentSend(ISession dbSession, Employee employee)
		{
			string deactivateAppeal = DeactivateComment;
			if (!string.IsNullOrEmpty(deactivateAppeal)) {
				deactivateAppeal = String.Format("Комментарий к деактивации аренды \"{0}\" (<a href='{1}/HardwareRent/UpdateHardwareRent/{2}'>{2}</a>) : {3}",
					Hardware.Name, ConfigHelper.GetParam("adminPanelNew"), Id, deactivateAppeal);

				var appealWithComment = new Appeal(deactivateAppeal, Client, AppealType.User) {
					Employee = employee
				};
				dbSession.Save(appealWithComment);
			}
		}

		//Комментарий к деактивации "Аренды оборудования", после его отправки в архив (чтобы контролировать задолжность)
		public virtual void DeactivateRentUpdateAppeal(ISession dbSession, Employee employee)
		{
			var message = String.Format("Обновление деактивированного оборудования \"{0}\" (<a href='{1}/HardwareRent/UpdateHardwareRent/{2}'>{2}</a>).<br/>Возвращен {3}",
				Hardware.Name, ConfigHelper.GetParam("adminPanelNew"), Id, GetAbsentPartsOfRentedHardware());

			if (DeactivateComment != "") {
				message = String.Format("{0}<br/>Комментарий к деактивации аренды \"{1}\" : {2}", message, Hardware.Name, DeactivateComment);
			}
			// 1-ое обращение - об активации услуги
			var appeal = new Appeal(message, Client, AppealType.User) {
				Employee = employee
			};
			dbSession.Save(appeal);
		}

		/// <summary>
		/// Возвращает итоговую цену за месяц аренды оборудования 
		/// </summary>
		public virtual decimal GetPrice()
		{
			return (Hardware != null) ? Hardware.Price : 0m;
		}

		/// <summary>
		/// Получение всей комплектации оборудования, допоолнение его, если комплектация пополнилась, 
		/// если у комплектации на форме не назначена аренда, назначается теккущей аренде (это при создании, т.к. на форме нет Id создаваемой аренды)
		/// </summary> 
		public virtual void GetClientHardwareParts()
		{
			//если комплектация не пополнилась
			if (ClientHardwareParts.Count == Hardware.HardwareParts.Count) {
				//проверяем значение аренды, если аренда не указана
				var clientHardwareParts = ClientHardwareParts.Where(s => s.ClientRentalHardware == null || s.ClientRentalHardware.Id == 0).ToList();
				//задаем текущую каждому эл-ту аренды
				if (clientHardwareParts.Count > 0) {
					ClientHardwareParts = clientHardwareParts.Select(o => {
						o.ClientRentalHardware = o.ClientRentalHardware == null || o.ClientRentalHardware.Id == 0 ? this : o.ClientRentalHardware;
						return o;
					}).ToList();
				}
			}
			else {
				//если комплектация пополнилась
				var newParts = Hardware.HardwareParts.Where(s => !ClientHardwareParts.Any(o => o.Part.Id == s.Id))
					.Select(s => { return new ClientHardwarePart() { ClientRentalHardware = this, Part = s }; }).ToList();
				//нужно добавить недостающие эл-ты в список арендуемых
				if (newParts.Count > 0) {
					ClientHardwareParts.AddEach(newParts);
				}
			}
		}

		/// <summary>
		/// Создание сообщения о списке недостающих эл-тов комплектации по аренде клиента
		/// </summary>
		/// <returns>списке недостающих эл-тов комплектации</returns>
		public virtual string GetAbsentPartsOfRentedHardware()
		{
			var clientHardware = ClientHardwareParts.Count(s => !s.Absent) == Hardware.HardwareParts.Count || ClientHardwareParts.Count == 0
				? "<strong>полный комплект</strong>"
				: "<strong style='color:red;'>неполный комплект</strong>, отсуствует:</br>" +
				  String.Join(",</br>", ClientHardwareParts.Where(s => s.Absent).Select(o => "- " + o.Part.Name).ToList());
			return clientHardware;
		}
	}
}