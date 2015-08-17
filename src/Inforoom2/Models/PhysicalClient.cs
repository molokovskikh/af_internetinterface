using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Common.Tools;
using Inforoom2.Helpers;
using Inforoom2.Intefaces;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping;
using NHibernate.Mapping.Attributes;
using NHibernate.Util;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель физического клиента
	/// </summary>
	[Class(0, Table = "PhysicalClients", Schema = "internet", NameType = typeof(PhysicalClient)), Description("Клиент")]
	public class PhysicalClient : BaseModel, ILogAppeal
	{
		[Property, Description("Пароль физического клиента")]
		public virtual string Password { get; set; }

		[ManyToOne(Column = "_Address", Cascade = "save-update"), NotNull(Message = "Адрес указан не полностью!"), Description("Адрес")]
		public virtual Address Address { get; set; }

		[Property(Column = "_Email", NotNull = true), Description("Электронаня почта физического клиента")]
		public virtual string Email
		{
			get
			{
				if (Client != null && Client.Contacts != null)
				{
					var contactMail = this.Client.Contacts.FirstOrDefault(s => s.Type == ContactType.Email);
					return contactMail != null ? contactMail.ContactString : "";
				}
				return "";
			}
			set
			{
				if (Client != null && Client.Contacts != null)
				{
					var contactMail = this.Client.Contacts.FirstOrDefault(s => s.Type == ContactType.Email);
					if (contactMail != null)
					{
						contactMail.ContactString = value;
					}
				}
			}
		}

		public PhysicalClient()
		{
			LastTimePlanChanged = SystemTime.Now();
		}

		[ManyToOne(Column = "Tariff"), NotNull(Message = "Выберите тариф"), Description("Тариф")]
		public virtual Plan Plan { get; set; }

		[Property(Column = "_LastTimePlanChanged")]
		public virtual DateTime LastTimePlanChanged { get; set; }

		[Property, Description("Общий баланс на счете клиента,который он может потратить")]
		public virtual decimal Balance { get; set; }

		[Property, Description("Сумма,которая взята с клиента за подключение(для клиентов 0,у частного сектора 5000)")]
		public virtual decimal ConnectSum { get; set; }

		[Property, Description("Сумма внесенная сотрудниками клиенту")]
		public virtual decimal VirtualBalance { get; set; }

		[Property, Description("Клиент проверен. устанавливается в админке, при редактировании клиента")]
		public virtual bool Checked { get; set; }

		[Property, Description("Реальные внесенные деньги клиентом на свой счет")]
		public virtual decimal MoneyBalance { get; set; }

		[Property(Column = "IdDocType"), Description("Документ, удостоверяющий личность")]
		public virtual CertificateType CertificateType { get; set; }

		[Property(Column = "IdDocName"), Description("Название документа, удостоверяющего личность")]
		public virtual string CertificateName { get; set; }

		[Property, Description("Номер паспорта")]
		public virtual string PassportNumber { get; set; }

		[Property, Description("Серия паспорта")]
		public virtual string PassportSeries { get; set; }

		[DataType(DataType.Date)]
		[Property, Description("Дата выдачи паспорта")]
		public virtual DateTime PassportDate { get; set; }

		[Property(Column = "RegistrationAdress"), Description("Адрес регистрации")]
		public virtual string RegistrationAddress { get; set; }

		[Property(Column = "WhoGivePassport"), Description("Кем выдан")]
		public virtual string PassportResidention { get; set; }

		[Property, Description("Номер абонента Ситилайн")]
		public virtual int? ExternalClientId { get; set; }

		[Property(Column = "_PhoneNumber", NotNull = true), Description("Номер телефона клиента")]
		public virtual string PhoneNumber
		{
			get
			{
				if (Client != null && Client.Contacts != null)
				{
					var contactPhone = this.Client.Contacts.FirstOrDefault(s => s.Type == ContactType.MobilePhone);
					return contactPhone != null ? contactPhone.ContactString : "";
				}
				return "";
			}
			set
			{
				if (Client != null && Client.Contacts != null)
				{
					var contactPhone = this.Client.Contacts.FirstOrDefault(s => s.Type == ContactType.MobilePhone);
					if (contactPhone != null)
					{
						contactPhone.ContactString = value;
					}
				}
			}
		}

		[Property(NotNull = true), NotEmpty(Message = "Введите имя"), Description("Имя")]
		public virtual string Name { get; set; }

		[Property(NotNull = true), NotEmpty(Message = "Введите фамилию"), Description("Фамилия")]
		public virtual string Surname { get; set; }

		[Property(NotNull = true), NotEmpty(Message = "Введите отчество"), Description("Отчество")]
		public virtual string Patronymic { get; set; }
		 
		[DataType(DataType.Date)]
		[Property(Column = "DateOfBirth"), Description("Дата рождения клиента")]
		public virtual DateTime BirthDate { get; set; }

		[OneToOne(PropertyRef = "PhysicalClient")]
		public virtual Client Client { get; set; }

		public virtual string FullName
		{
			get { return Surname + " " + Name + " " + Patronymic; }
		}

		public virtual UserWriteOff RequestChangePlan(Plan planToSwitchOn)
		{
			var price = Plan.GetTransferPrice(planToSwitchOn);
			if (!IsEnoughBalance(price))
			{
				return null;
			}
			return SwitchPlan(planToSwitchOn, price);
		}

		private UserWriteOff SwitchPlan(Plan planTo, decimal price)
		{
			var comment = string.Format("Изменение тарифа, старый '{0}' новый '{1}'", Plan.Name, planTo.Name);
			Plan = planTo;
			WriteOff(price);
			var writeOff = new UserWriteOff
			{
				Client = Client,
				Date = SystemTime.Now(),
				Sum = price,
				Comment = comment,
				IsProcessedByBilling = true
			};
			LastTimePlanChanged = SystemTime.Now();
			if (Client.Internet.ActivatedByUser)
				Client.Endpoints.ForEach(e => e.PackageId = Plan.PackageSpeed.PackageId);
			return writeOff;
		}

		public virtual bool IsEnoughBalance(decimal sum)
		{
			if (sum < 0)
			{
				return false;
			}
			return Balance - sum > 0;
		}

		public virtual WriteOff WriteOff(decimal sum, bool writeoffVirtualFirst = false)
		{
			var writeoff = CalculateWriteoff(sum, writeoffVirtualFirst);

			if (writeoff == null)
				return null;

			Balance -= writeoff.WriteOffSum;
			VirtualBalance -= writeoff.VirtualSum;
			MoneyBalance -= writeoff.MoneySum;

			return writeoff;
		}

		public virtual WriteOff CalculateWriteoff(decimal sum, bool writeoffVirtualFirst = false)
		{
			if (sum <= 0)
				return null;

			decimal virtualWriteoff;
			decimal moneyWriteoff;

			if (writeoffVirtualFirst)
			{
				virtualWriteoff = Math.Min(sum, VirtualBalance);
			}
			else
			{
				virtualWriteoff = Math.Min(Math.Abs(Math.Min(MoneyBalance - sum, 0)), VirtualBalance);
			}
			moneyWriteoff = sum - virtualWriteoff;

			return new WriteOff
			{
				Client = Client,
				WriteOffDate = SystemTime.Now(),
				WriteOffSum = Math.Round(sum, 2),
				MoneySum = Math.Round(moneyWriteoff, 2),
				VirtualSum = Math.Round(virtualWriteoff, 2),
				Sale = Client.Discount,
				BeforeWriteOffBalance = Client.Balance
			};
		}


		///  Генерация пароля для пользователя
		///  *взято из старой админки////////////////////// 
		public static string GeneratePassword(PhysicalClient ph)
		{
			var availableChars = "23456789qwertyupasdfghjkzxcvbnmQWERTYUPASDFGHJKLZXCVBNM";
			var password = String.Empty;
			while (password.Length < 8)
			{
				int availableChars_elem = 0;

				var rngCsp = new RNGCryptoServiceProvider();
				var randomNumber = new byte[1];
				do
				{
					rngCsp.GetBytes(randomNumber);
				} while (!(randomNumber[0] < (availableChars.Length - 1) * (Byte.MaxValue / (availableChars.Length - 1))));

				availableChars_elem = (randomNumber[0] % (availableChars.Length - 1)) + 1;

				password += availableChars[availableChars_elem];
			}
			ph.Password = Md5.GetHash(password);
			return password;
		}

		//////////////////////////////////////////////////

		public virtual Client GetAppealClient(ISession session)
		{
			return session.Query<Client>().FirstOrDefault(s => s.PhysicalClient.Id == this.Id);
		}
		public virtual List<string> GetAppealFields()
		{
			return new List<string>() {
				"Name",
				"Surname",
				"Patronymic", 
				"CertificateType",
				"CertificateName",
				"PassportSeries",
				"PassportNumber",
				"PassportDate",
				"PassportResidention",
				"RegistrationAddress",
				"Plan",
				"Checked",
				"Address"
			};
		}
		public virtual string GetAdditionalAppealInfo(string property, object oldPropertyValue, ISession session)
		{
			string message = "";
			// для свойства Tariff
			if (property == "Plan")
			{
				// получаем псевдоним из описания 
				property = this.Plan.GetDescription();
				var oldPlan = oldPropertyValue == null ? null : ((Plan)oldPropertyValue);
				var currentPlan = this.Plan;
				if (oldPlan != null)
				{
					message += property + " было: " + oldPlan.Name + " <br/>";
				}
				else
				{
					message += property + " было: " + "значение отсуствует <br/> ";
				}
				if (currentPlan != null)
				{
					message += property + " стало: " + currentPlan.Name + " <br/>";
				}
				else
				{
					message += property + " стало: " + "значение отсуствует <br/> ";
				}
			}
			return message;
		}
	}

	public enum CertificateType
	{
		[Display(Name = "Паспорт РФ")]
		[Description("Паспорт РФ")]
		Passport = 0,
		[Display(Name = "Иной документ")]
		[Description("Иной документ")]
		Other = 1
	}
}