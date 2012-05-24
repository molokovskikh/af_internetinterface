﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Models.Security;
using InternetInterface.Helpers;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	[ActiveRecord("Partners", Schema = "internet", Lazy = true)]
	public class Partner : ValidActiveRecordLinqBase<Partner>
	{
		public Partner()
		{}

		public Partner(string login)
		{
			Login = login;
			Name = login;
			Categorie = UserCategorie.Find(3u);
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Введите имя")]
		public virtual string Name { get; set; }

		[Property, ValidateEmail("Ошибка формата Email")]
		public virtual string Email { get; set; }

		[Property, ValidateRegExp(@"^([0-9]{3})\-([0-9]{7})$", "Ошибка формата телефонного номера ***-*******")]
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

		public virtual bool CategorieIs(string reductionName)
		{
			return Categorie.ReductionName == reductionName;
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

		public static bool RegistrLogicPartner(Partner partner, ValidatorRunner validator)
		{
			if (validator.IsValid(partner)) {
				partner.Categorie.Refresh();
				partner.RegDate = DateTime.Now;
				partner.SaveAndFlush();
				return true;
			}
			return false;
		}

		public override string ToString()
		{
			return Name;
		}

		public virtual bool HavePermissionTo(IControllerContext controllerContext)
		{
			return Permissions().Any(p => p.Match(controllerContext));
		}

		public virtual IEnumerable<IPermission> Permissions()
		{
			if (AccesedPartner.Any(p => p.Match("SSI")))
				return new IPermission[] { new ControllerActionPermission("Payments", "Cancel") };
			return Enumerable.Empty<IPermission>();
		}
	}
}