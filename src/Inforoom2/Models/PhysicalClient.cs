using System;
using System.Collections.Generic;
using System.Linq;
using Common.Tools;
using NHibernate.Linq;
using NHibernate.Mapping;
using NHibernate.Mapping.Attributes;
using NHibernate.Util;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "PhysicalClients", Schema = "internet", NameType = typeof (PhysicalClient))]
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
		public virtual decimal VirtualBalance { get; set; }

		[Property]
		public virtual decimal MoneyBalance { get; set; }

		[Property]
		public virtual string PassportNumber { get; set; }

		[Property(Column = "_PhoneNumber", NotNull = true)]
		public virtual string PhoneNumber { get; set; }

		[Property(NotNull = true), NotEmpty(Message = "Введите имя")]
		public virtual string Name { get; set; }

		[Property(NotNull = true), NotEmpty(Message = "Введите фамилию")]
		public virtual string Surname { get; set; }

		[Property(NotNull = true), NotEmpty(Message = "Введите отчество")]
		public virtual string Patronymic { get; set; }

		[OneToOne(PropertyRef = "PhysicalClient")]
		public virtual Client Client { get; set; }

		public virtual string FullName
		{
			get { return Surname + " " + Name + " " + Patronymic; }
		}

		public virtual UserWriteOff ChangeTariffPlan(Plan planToSwitchOn)
		{
			if (IsFreePlanChange) {
				return SwitchPlan(planToSwitchOn, 0);
			}
			if (!IsEnoughBalance(planToSwitchOn.SwitchPrice)) {
				return null;
			}
			return SwitchPlan(planToSwitchOn, planToSwitchOn.SwitchPrice);
		}

		private UserWriteOff SwitchPlan(Plan toPlan, decimal price)
		{
			var comment = string.Format("Изменение тарифа, старый '{0}' новый '{1}'", Plan.Name, toPlan.Name);
			Plan = toPlan;
			WriteOff(price);
			var writeOff = new UserWriteOff {
				Client = Client,
				Date = DateTime.Now,
				Sum = price,
				Comment = comment,
				IsProcessedByBilling = true
			};
			LastTimePlanChanged = DateTime.Now;
			if (Client.Internet.ActivatedByUser)
				Client.Endpoints.ForEach(e=>e.PackageId = Plan.PackageId);
			return writeOff;
		}

		public virtual bool IsFreePlanChange
		{
			get { return LastTimePlanChanged.AddMonths(1) < DateTime.Now; }
		}

		public virtual bool IsEnoughBalance(decimal sum)
		{
			if (sum < 0) {
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

			if (writeoffVirtualFirst) {
				virtualWriteoff = Math.Min(sum, VirtualBalance);
			}
			else {
				virtualWriteoff = Math.Min(Math.Abs(Math.Min(MoneyBalance - sum, 0)), VirtualBalance);
			}
			moneyWriteoff = sum - virtualWriteoff;

			return new WriteOff {
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
}