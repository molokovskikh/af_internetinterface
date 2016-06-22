using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Intefaces;
using InternetInterface.Controllers.Filter;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	//перенесено из старой админки (нужен при проверке на необходимость показывать Варнинг)
	[Class(0, Table = "Orders", Schema = "internet", NameType = typeof (ClientOrder)), Description("Заказ")]
	public class ClientOrder : BaseModel, ILogAppeal
	{
		public ClientOrder()
		{
			OrderServices = new List<OrderService>();
		}

		[Property, Description("Номер заказа")]
		public virtual int Number { get; set; }

		[Property, Description("Дата активации заказа")]
		public virtual DateTime? BeginDate { get; set; }

		[Property, Description("Дата окончания заказа")]
		public virtual DateTime? EndDate { get; set; }

		//состояние заказа, выставленное пользователем
		[Property]
		public virtual bool Disabled { get; set; }

		//обработана ли активация заказ, устанавливает биллинг нужен для учета списаний не периодических услуг
		[Property, Description("Активирован")]
		public virtual bool IsActivated { get; set; }

		//обработана ли деактивация заказа, устанавливает биллинг нужен для учета списаний периодических услуг
		[Property, Description("Деактивирован")]
		public virtual bool IsDeactivated { get; set; }

		[Bag(0, Table = "OrderServices", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "OrderId")]
		[OneToMany(2, ClassType = typeof (OrderService)), Description("Услуги")]
		public virtual IList<OrderService> OrderServices { get; set; }

		[ManyToOne(Column = "EndPoint"), Description("Точка подключения")]
		public virtual ClientEndpoint EndPoint { get; set; }

		[ManyToOne(Column = "ClientId")]
		public virtual Client Client { get; set; }

		//Это поле нужно заменить, в результате перехода на новые адреса
		[Property, Description("Адрес подключения (текст)")]
		public virtual string ConnectionAddress { get; set; }

		public virtual Client GetAppealClient(ISession session)
		{
			return Client;
		}

		public virtual List<string> GetAppealFields()
		{
			return new List<string>()
			{
				"Number",
				"BeginDate",
				"EndDate",
				"IsDeactivated",
				"ConnectionAddress",
				"IsActivated",
				"EndPoint",
				"OrderServices"
			};
		}

		public virtual bool IsToBeClosed()
		{
			return EndDate.HasValue && EndDate.Value <= SystemTime.Now().Date;
		}

		public virtual string GetAdditionalAppealInfo(string property, object oldPropertyValue, ISession session)
		{
			string message = ""; 
			if (property == "IsActivated") {
				// получаем псевдоним из описания 
				property = this.GetDescription("IsActivated");
				var oldPlan = oldPropertyValue == null ? null : ((bool?) oldPropertyValue);
				var currentPlan = IsDeactivated;
				if (oldPlan != null) {
					message += property + " было: " + (oldPlan.HasValue && oldPlan.Value ? "да" : "нет") + " <br/>";
				}
				else {
					message += property + " было: " + "значение отсуствует <br/> ";
				}
				if (currentPlan != null) {
					message += property + " стало: " + (currentPlan ? "да" : "нет") + " <br/>";
				}
				else {
					message += property + " стало: " + "значение отсуствует <br/> ";
				}
			}
			if (property == "IsDeactivated")
			{
				// получаем псевдоним из описания 
				property = this.GetDescription("IsDeactivated");
				var oldPlan = oldPropertyValue == null ? null : ((bool?)oldPropertyValue);
				var currentPlan = IsDeactivated;
				if (oldPlan != null)
				{
					message += property + " было: " + (oldPlan.HasValue && oldPlan.Value ? "да" : "нет") + " <br/>";
				}
				else
				{
					message += property + " было: " + "значение отсуствует <br/> ";
				}
				if (currentPlan != null)
				{
					message += property + " стало: " + (currentPlan ? "да" : "нет") + " <br/>";
				}
				else
				{
					message += property + " стало: " + "значение отсуствует <br/> ";
				}
			}
			return message;
		}

		public virtual void SendMailAboutCreate(ISession session, Employee employee)
		{
			var addressRaw = ConfigHelper.GetParam("OrderNotificationMail");
			var addressList = addressRaw.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

			var topic = "Уведомление о создании заказа";
			var body = ClientOrder.MailPattern(this, "создание", employee) + OrderService.MailPattern(this, employee);
			EmailSender.SendEmail(addressList, topic, body);
		}

		public virtual void SendMailAboutUpdate(ISession session, Employee employee)
		{
			var addressRaw = ConfigHelper.GetParam("OrderNotificationMail");
			var addressList = addressRaw.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

			var topic = "Уведомление о внесение изменений в заказ";
			var body = ClientOrder.MailPattern(this, "изменение", employee) + OrderService.MailPattern(this, employee);
			EmailSender.SendEmail(addressList, topic, body);
		}

		public virtual void SendMailAboutClose(ISession session, Employee employee)
		{
			var addressRaw = ConfigHelper.GetParam("OrderNotificationMail");
			var addressList = addressRaw.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

			var topic = "Уведомление о закрытии заказа";
			var body = ClientOrder.MailPattern(this, "закрытие", employee) + OrderService.MailPattern(this, employee);
			EmailSender.SendEmail(addressList, topic, body);
		}

		public static string MailPattern(ClientOrder order, string operationName, Employee employee)
		{
			var sum = order.OrderServices.Sum(x => x.Cost);
			var message = new StringBuilder();
			message.AppendLine(string.Format("Зарегистрировано {0} заказа для Юр.Лица", operationName));
			message.AppendLine(string.Format("Клиент: {0}-{1}", order.Client.Id.ToString("D5"), order.Client.Name));
			message.AppendLine(string.Format("Заказ: № {0}", order.Number));
			message.AppendLine(string.Format("Сумма {0}", sum));
			message.AppendLine(string.Format("Оператор: {0}", employee.Name));
			return message.ToString();
		}
	}

	//перенесено из старой админки (нужен при проверке на необходимость показывать Варнинг)
	[Class(0, Table = "OrderServices", Schema = "internet", NameType = typeof (OrderService), Lazy = true),
	 Description("Услуга")]
	public class OrderService : BaseModel
	{
		public OrderService()
		{
		}

		public OrderService(ClientOrder order, decimal cost, bool isPeriodic)
		{
			Order = order;
			IsPeriodic = isPeriodic;
			Cost = cost;
			if (IsPeriodic)
				Description = "Доступ к сети Интернет";
			else
				Description = "Подключение";
		}

		[Property, NotNullNotEmpty(Message = "Услуги должны иметь читаемые имена"), Description("Описание")]
		public virtual string Description { get; set; }

		[Property, Description("Стоимость")]
		public virtual decimal Cost { get; set; }

		[Property, Description("Услуга периодичная")]
		public virtual bool IsPeriodic { get; set; }

		[ManyToOne(Column = "OrderId")]
		public virtual ClientOrder Order { get; set; }


		public static string MailPattern(ClientOrder order, Employee employee)
		{
			var message = new StringBuilder();
			foreach (var oneOrder in order.OrderServices) {
				message.AppendLine(String.Format(@"Описание услуги: {0}Услуга: {1}", oneOrder.Description,
					oneOrder.IsPeriodic ? "Периодичная" : "Разовая"));
			}
			return message.ToString();
		}
	}
}