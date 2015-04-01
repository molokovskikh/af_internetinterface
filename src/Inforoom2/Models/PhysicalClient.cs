using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Common.Tools;
using Inforoom2.validators;
using NHibernate.Mapping.Attributes;
using NHibernate.Util;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "PhysicalClients", Schema = "internet", NameType = typeof(PhysicalClient))]
	public class PhysicalClient : BaseModel
	{
		[Property]
		public virtual string Password { get; set; }

		[ManyToOne(Column = "_Address", Cascade = "save-update")]
		public virtual Address Address { get; set; }

		[Property(Column = "_Email", NotNull = true)]
		public virtual string Email { get; set; }

		[ManyToOne(Column = "Tariff")]
		public virtual Plan Plan { get; set; }

		[Property(Column = "_LastTimePlanChanged")]
		public virtual DateTime LastTimePlanChanged { get; set; }

		[Property]
		public virtual decimal Balance { get; set; }

		[Property]
		public virtual decimal ConnectSum { get; set; }

		[Property]
		public virtual decimal VirtualBalance { get; set; }

		[Property]
		public virtual decimal MoneyBalance { get; set; }

		[Property(Column = "IdDocType"), Description("Документ, удостоверяющий личность")]
		public virtual CertificateType CertificateType { get; set; }

		[Property(Column = "IdDocName"), NotNullNotEmpty(Message = "Введите название"), Description("Название документа, удостоверяющего личность")]
		public virtual string CertificateName { get; set; }

		[Property, NotNullNotEmpty(Message = "Введите номер паспорта")]
		public virtual string PassportNumber { get; set; }

		[Property, NotNullNotEmpty(Message = "Введите серию паспорта")]
		public virtual string PassportSeries { get; set; }

		[Property, DateTimeNotEmpty]
		public virtual DateTime PassportDate { get; set; }

		[Property(Column = "RegistrationAdress"), NotNull(Message = "Введите адрес регистрации")]
		public virtual string RegistrationAddress { get; set; }

		[Property(Column = "WhoGivePassport"), NotNullNotEmpty(Message = "Поле не может быть пустым")]
		public virtual string PassportResidention { get; set; }

		[Property(Column = "_PhoneNumber", NotNull = true)]
		public virtual string PhoneNumber { get; set; }

		[Property(NotNull = true), NotEmpty(Message = "Введите имя")]
		public virtual string Name { get; set; }

		[Property(NotNull = true), NotEmpty(Message = "Введите фамилию")]
		public virtual string Surname { get; set; }

		[Property(NotNull = true), NotEmpty(Message = "Введите отчество")]
		public virtual string Patronymic { get; set; }

		[Property(Column = "DateOfBirth"), DateTimeNotEmpty]
		public virtual DateTime BirthDate { get; set; }

		[OneToOne(PropertyRef = "PhysicalClient")]
		public virtual Client Client { get; set; }

		public virtual string FullName
		{
			get { return Surname + " " + Name + " " + Patronymic; }
		}

		[OneToMany]
		public virtual ClientRequest ClientRequest { get; set; }

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
				Date = DateTime.Now,
				Sum = price,
				Comment = comment,
				IsProcessedByBilling = true
			};
			LastTimePlanChanged = DateTime.Now;
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
	}

	public enum CertificateType
	{
		[Display(Name = "Паспорт РФ")]
		Passport = 0,
		[Display(Name = "Иной документ")]
		Other = 1
	}
}
