using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Inforoom2.Intefaces;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Запись в графике инженеров
	/// </summary>
	[Class(0, Table = "ServicemenScheduleItems", NameType = typeof(ServicemenScheduleItem))]
	public class ServicemenScheduleItem : BaseModel, IServicemenScheduleItem
	{
		[Property,Description("Комментарий")]
		public virtual string Comment { get; set; }

		[ManyToOne, Description("Клиент")]
		public virtual Client Client { get; set; }

		[ManyToOne, Description("Сервисная заявка")]
		public virtual ServiceRequest ServiceRequest { get; set; }

		[ManyToOne, Description("Инженер")]
		public virtual ServiceMan ServiceMan { get; set; }

		[Property, Description("Начало выполнения")]
		public virtual DateTime? BeginTime { get; set; }

		[Property, Description("Конец выполнения")]
		public virtual DateTime? EndTime { get; set; }

		[Property, Description("Тип")]
		public virtual Type RequestType { get; set; }

		[Property, Description("Статус")]
		public virtual bool Status { get; set; }

		/// <summary>
		/// Получить объект с интерфейсом IServicemenScheduleItem
		/// </summary> 
		public virtual IServicemenScheduleItem GetObject()
		{
			return Client as IServicemenScheduleItem ?? ServiceRequest as IServicemenScheduleItem;
		}

		public virtual string GetAddress()
		{
			return GetObject().GetAddress();
		}

		public virtual Client GetClient()
		{
			return GetObject().GetClient();
		}

		public virtual string GetPhone()
		{
			return GetObject().GetPhone();
		}

		/// <summary>
		/// Тип элемента на графике обработки заявок
		/// </summary>
		public enum Type
		{
			[Description("ServiceRequest")] ServiceRequest,
			[Description("ConnectionRequest")] ClientConnectionRequest
		}

		/// <summary>
		/// Получение элемента на графике обработки заявок
		/// </summary>
		/// <param name="dbSession">Сессия в БД</param>
		/// <param name="id">Идентификатор (Сервисной заявки / Клиента)</param>
		/// <param name="type">Тип заявки</param>
		/// <returns></returns>
		public static ServicemenScheduleItem GetSheduleItem(ISession dbSession, int id, Type type)
		{
			if (type == Type.ServiceRequest) {
				// находим элемент в БД
				var existedItem = dbSession.Query<ServicemenScheduleItem>().FirstOrDefault(s => s.ServiceRequest.Id == id && s.RequestType == type);
				// если не существует, создаем новый на основе заявки
				var itemTosave = existedItem ?? dbSession.Query<ServiceRequest>().Where(s => s.Id == id)
					.Select(s => new ServicemenScheduleItem() { RequestType = type, ServiceRequest = s, Comment = s.Description }).FirstOrDefault();
				// если элемент найден или создан сохраняем его в БД и возвращаем
				if (itemTosave != null && itemTosave.Id == 0)
					dbSession.Save(itemTosave);
				return itemTosave;
			}
			if (type == Type.ClientConnectionRequest) {
				// находим элемент в БД
				var existedItem = dbSession.Query<ServicemenScheduleItem>().FirstOrDefault(s => s.Client.Id == id && s.RequestType == type);
				// если не существует, создаем новый
				var itemTosave = existedItem ?? new ServicemenScheduleItem() { RequestType = type, Client = dbSession.Query<Client>().FirstOrDefault(s => s.Id == id) };
				// если элемент найден или создан сохраняем его в БД и возвращаем
				if (itemTosave.Id == 0)
					dbSession.Save(itemTosave);
				return itemTosave;
			}
			return null;

		}
	}
}