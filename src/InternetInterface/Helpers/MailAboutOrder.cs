﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;
using Castle.Core.Smtp;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Models.Audit;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate;

namespace InternetInterface.Helpers
{
	public class SendEmailAboutOrder : SendEmail
	{
		public SendEmailAboutOrder() : base(typeof(NotificationAboutChangeOrder), "internet@ivrn.net")
		{
		}
	}

	public class LogInsert : Attribute
	{
		public Type LoggingType { get; set; }

		public LogInsert(Type loggingType)
		{
			LoggingType = loggingType;
		}
	}

	public interface ILogInterface
	{
		void Log(object entity);
	}

	public class LogOrderInsert : ILogInterface
	{
		public void Log(object entity)
		{
			Order order = MessageOrderHelper.GetOrderFromEntity(entity);
			var message = MessageOrderHelper.GenerateText(order, "создание");
			MessageOrderHelper.SendMail("Уведомление о создании заказа", message);
			var appeal = new Appeals(message, order.Client, AppealType.System, true);
			ArHelper.WithSession(s => s.Save(appeal));
		}
	}

	public class NotificationAboutChangeOrder : ISendNoticationChangesInterface
	{
		public void Send(AuditableProperty property, object entity, string to)
		{
			Order order = MessageOrderHelper.GetOrderFromEntity(entity);
			var message = MessageOrderHelper.GenerateText(order, "внесение изменений") + property.Message;
			MessageOrderHelper.SendMail("Уведомление: внесение изменений в заказ", message);
		}
	}

	public class MessageOrderHelper
	{
		public static IEmailSender Sender = new FolderSender(ConfigurationManager.AppSettings["SmtpServer"]);

		public static Order GetOrderFromEntity(object entity)
		{
			Order order = null;
			if (entity.GetType() == typeof(Order)) {
				order = (Order)entity;
			}
			if (entity.GetType() == typeof(OrderService)) {
				order = ((OrderService)entity).Order;
			}
			return order;
		}

		public static string GenerateText(Order order, string operationType)
		{
			decimal sum = 0;
			if (order.OrderServices != null)
				sum = order.OrderServices.Sum(x => x.Cost);
			string listOfServices = "";
			if (order.OrderServices != null)
				foreach (OrderService oneOrder in order.OrderServices) {
					listOfServices += String.Format(@"
Описание услуги: {0}
Услуга: {1}", oneOrder.Description, oneOrder.GetPeriodic());
				}
			var message = new StringBuilder();
			message.AppendLine(string.Format("Зарегистрировано {0} заказа для Юр.Лица", operationType));
			message.AppendLine(string.Format("Клиент: {0}-{1}", order.Client.Id.ToString("D5"), order.Client.Name));
			message.AppendLine(string.Format("Заказ: № {0}", order.Id));
			message.AppendLine(string.Format("Сумма {0}", sum));
			message.AppendLine(string.Format("Оператор: {0}", InitializeContent.Partner.Name));
			message.AppendLine(listOfServices);
			return message.ToString();
		}

		public static void SendMail(string subject, string textMessage)
		{
			string[] address = { "internet@ivrn.net", "billing@analit.net" };
			var mailer = new Mailer(Sender);
			mailer.SendText("internet@ivrn.net", address, subject, textMessage);
		}
	}
}