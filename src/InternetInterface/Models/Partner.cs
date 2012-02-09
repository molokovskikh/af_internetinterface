using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models.Universal;
using NHibernate.Criterion;
using NHibernate.SqlCommand;

namespace InternetInterface.Models
{

	[ActiveRecord("Partners", Schema = "internet", Lazy = true)]
	public class Partner : ValidActiveRecordLinqBase<Partner>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Введите имя")]
		public virtual string Name { get; set; }

		[Property, ValidateEmail("Ошибка формата Email")]
		public virtual string Email { get; set; }

		[Property, ValidateRegExp(@"^((\d{4,5})-(\d{5,6}))|((\d{1})-(\d{3})-(\d{3})-(\d{2})-(\d{2}))", "Ошибка фотмата телефонного номера (Код города (4-5 цифр) + местный номер (5-6 цифр) или мобильный телефн (8-***-***-**-**))")]
		public virtual string TelNum { get; set; }

		[Property]
		public virtual string Adress { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[Property, ValidateNonEmpty("Введите логин"), ValidateIsUnique("Логин должен быть уникальный")]
		public virtual string Login { get; set; }

		[BelongsTo("Categorie", Lazy = FetchWhen.OnInvoke, Cascade = CascadeEnum.SaveUpdate)]
		public virtual UserCategorie Categorie { get; set; }

		[HasMany(ColumnKey = "Agent", OrderBy = "RegistrationDate", Lazy = true)]
		public virtual IList<PaymentsForAgent> Payments { get; set; }

		public virtual IList<string> AccesedPartner { get; set; }


		public static Partner GetPartnerForLogin(string login)
		{
			return FindAllByProperty("Login", login).FirstOrDefault();
		}

		public virtual decimal GetAgentPayment(Week interval)
		{
			return
				Payments.Where(
					p => p.RegistrationDate.Date >= interval.StartDate.Date && p.RegistrationDate.Date <= interval.EndDate.Date).Sum(
						p => p.Sum);
		}

		public static List<Partner> GetHouseMapAgents()
		{
			return Queryable.Where(p => p.Categorie.ReductionName == "Agent").ToList();
		}

		public static List<Partner> GetServiceIngeners()
		{
			return Queryable.Where( p => p.Categorie.ReductionName == "Service").ToList();
		}

		public override void SaveAndFlush()
		{
			base.SaveAndFlush();
			var catAS = CategorieAccessSet.FindAllByProperty("Categorie", Categorie);
			foreach (var categorieAccessSet in catAS)
			{
				categorieAccessSet.AccessCat.AcceptToOne(this);
			}
		}

		private List<PaymentsForAgent> GetPaymentsFoInterval(Week interval)
		{
			return
				Payments.Where(
					p => 
					p.RegistrationDate.Date >= interval.StartDate.Date && p.RegistrationDate.Date <= interval.EndDate.Date).ToList();
		}

		public virtual List<PaymentsForAgent> GetPaymentsForInterval(Week interval)
		{
			return GetPaymentsFoInterval(interval).Where(p => p.Sum >= 0 && p.Action != null).ToList();
		}

		public virtual List<PaymentsForAgent> GetWriteOffsForInterval(Week interval)
		{
			return GetPaymentsFoInterval(interval).Where(p => p.Sum < 0 && p.Action != null).ToList();
		}

		public virtual List<PaymentsForAgent> GetBunosesForInterval(Week interval)
		{
			return GetPaymentsFoInterval(interval).Where(p => p.Sum >= 0 && p.Action == null).ToList();
		}

		public virtual List<PaymentsForAgent> GetPenaltyForInterval(Week interval)
		{
			return GetPaymentsFoInterval(interval).Where(p => p.Sum < 0 && p.Action == null).ToList();
		}

		public static bool RegistrLogicPartner(Partner _Partner, ValidatorRunner validator)
		{
			if (validator.IsValid(_Partner)) {
				_Partner.Categorie.Refresh();
				//var categorie = UserCategorie.Queryable.Where(u => u.Id == _Partner.Categorie.Id);
				_Partner.RegDate = DateTime.Now;
				_Partner.SaveAndFlush();
				return true;
			}
			else
			{
				return false;
			}
		}
	}

}