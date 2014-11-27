using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Mapping;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "PhysicalClients", Schema = "internet", NameType = typeof(PhysicalClient))]
	public class PhysicalClient : BaseModel
	{
		
		[Property]
		public virtual string Password { get; set; }

		[Property(Column = "_Salt")]
		public virtual string Salt { get; set; }

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

		[Property(Column = "_PhoneNumber",NotNull = true), NotEmpty, Pattern(Regex = (@"^((\d{3})-(\d{7}))"))]
		public virtual string PhoneNumber { get; set; }

		[Property(NotNull = true), NotEmpty(Message = "Введите имя")]
		public virtual string Name { get; set; }

		[Property(NotNull = true), NotEmpty(Message = "Введите фамилию")]
		public virtual string Surname { get; set; }

		[Property(NotNull = true), NotEmpty(Message = "Введите отчество")]
		public virtual string Patronymic { get; set; }
		
		public virtual string FullName
		{
			get { return Surname + " " + Name + " " + Patronymic; }
		}

		public virtual bool ChangeTariffPlan(Plan planToSwitchOn)
		{
			if (IsFreePlanChange) {
				SwitchPlan(planToSwitchOn, 0);
				return true;
			}
			if (!IsEnoughBalance(planToSwitchOn.SwitchPrice)) {
				return false;
			}
			SwitchPlan(planToSwitchOn, planToSwitchOn.SwitchPrice);
			return true;
		}

		private void SwitchPlan(Plan toPlan, decimal price)
		{
			Plan = toPlan;
			Balance -= price;
			LastTimePlanChanged = DateTime.Now;
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
	}
}