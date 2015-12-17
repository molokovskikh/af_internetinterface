using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Castle.Components.Validator;
using Inforoom2.Intefaces;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;
using NHibernate.Linq;

namespace Inforoom2.Models
{
	/// <summary>
	/// Юридическое лицо
	/// </summary>
	[Class(0, Table = "LawyerPerson", Schema = "internet", NameType = typeof (LegalClient))]
	public class LegalClient : BaseModel, IClientExpander
	{
		[Property(NotNull = true), Description("Баланс юридического лица")]
		public virtual decimal Balance { get; set; }

		[Property(NotNull = true), Description("Дата, когда списывается абонентская плата")]
		public virtual DateTime PeriodEnd { get; set; }

		[ManyToOne(Column = "RegionId", Cascade = "save-update"), NotNull(Message = "Укажите регион")]
		public virtual Region Region { get; set; }

		[Property(Column = "FullName"), NotNullNotEmpty(Message = "Введите полное наименование")]
		public virtual string Name { get; set; }

		[Property, NotNullNotEmpty(Message = "Введите краткое наименование")]
		public virtual string ShortName { get; set; }

		[Property(Column = "LawyerAdress"), NotNullNotEmpty(Message = "Введите юридический адрес")]
		public virtual string LegalAdress { get; set; }

		[Property, NotNullNotEmpty(Message = "Введите фактический адрес")]
		public virtual string ActualAdress { get; set; }

		[ManyToOne(Column = "_Address", Cascade = "save-update"), Description("Адрес")]
		public virtual Address Address { get; set; }

		[Property, Description("ИНН"), NotNullNotEmpty(Message = "Введите ИНН")]
		public virtual string Inn { get; set; }

		[Property, Description("Контактное лицо"), NotNullNotEmpty(Message = "Укажите контактное лицо")]
		public virtual string ContactPerson { get; set; }

		[Property, Description("Почтовый адрес"), NotNullNotEmpty(Message = "Введите почтовый адрес")]
		public virtual string MailingAddress { get; set; }

		public virtual decimal? Plan
		{
			get
			{
				return Client.LegalClientOrders.Where(o => o.IsActivated && !o.IsDeactivated)
					.SelectMany(o => o.OrderServices)
					.Where(s => s.IsPeriodic)
					.Sum(s => s.Cost);
			}
		}

		[OneToOne(PropertyRef = "LegalClient")]
		public virtual Client Client { get; set; }

		public virtual object GetExtendedClient => this;

		public virtual IList<Contact> GetContacts()
		{
			return Client.Contacts;
		}

		public virtual string GetConnetionAddress()
		{
			return Address.GetStringForPrint();
		}

		public virtual string GetName()
		{
			return ShortName;
		}

		public virtual DateTime? GetRegistrationDate()
		{
			return Client.CreationDate;
		}

		public virtual DateTime? GetDissolveDate()
		{
			var lastClosedOrder =
				Client.LegalClientOrders.Where(o => o.IsDeactivated && o.EndDate != null)
					.ToList()
					.OrderByDescending(f => f.EndDate.Value)
					.FirstOrDefault();

			return ((StatusType) Client.Status.Id) != StatusType.Dissolved ? null : lastClosedOrder?.EndDate;
		}

		public virtual string GetPlan()
		{
			return $"{(this.Plan ?? 0).ToString("0.00")} руб.";
		}

		public virtual decimal GetBalance()
		{
			return Balance;
		}

		public virtual StatusType GetStatus()
		{
			return ((StatusType) Client.Status.Id);
		}

		public static void GetBaseDataForRegistration(ISession dbSession, Client client, Employee employee)
		{
			client.Recipient = dbSession.Query<Recipient>().FirstOrDefault(r => r.INN != null && r.INN == "3666152146");
			client.Status = dbSession.Query<Status>().FirstOrDefault(r => r.Id == (int) StatusType.BlockedAndNoConnected);
			client.Disabled = true;
			client.Type = ClientType.Lawer;
			client._Name = client.LegalClient.ShortName;
			client.Disabled = true;
			client.WhoRegistered = employee;
			client.WhoRegisteredName = client.WhoRegistered.Name;
			client.PercentBalance = 0m;
			client.CreationDate = DateTime.Now;
			client.FreeBlockDays = 0;
			client.SendSmsNotification = false;
			client._oldAdressStr = client.LegalClient.ActualAdress;
			client.YearCycleDate = null;
		}
	}
}