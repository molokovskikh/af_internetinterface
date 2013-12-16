﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Web.Ui.Models.Security;
using InternetInterface.Controllers;
using InternetInterface.Helpers;
using InternetInterface.Models.Universal;
using NHibernate.Criterion;
using NHibernate.SqlCommand;

namespace InternetInterface.Models
{
	[ActiveRecord("Partners", Schema = "internet", Lazy = true)]
	public class Partner : ValidActiveRecordLinqBase<Partner>
	{
		public Partner()
		{
		}

		public Partner(string login)
			: this(UserRole.Find(3u))
		{
			Login = login;
			Name = login;
		}

		public Partner(UserRole role)
		{
			Role = role;
			RegDate = DateTime.Now;
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
		public virtual UserRole Role { get; set; }

		[HasMany(ColumnKey = "Agent", OrderBy = "RegistrationDate", Lazy = true)]
		public virtual IList<PaymentsForAgent> Payments { get; set; }

		public virtual IList<string> AccesedPartner { get; set; }

		public virtual TimeSpan WorkEnd
		{
			get
			{
				return new TimeSpan(19, 0, 0);
			}
		}

		public virtual TimeSpan WorkStep
		{
			get
			{
				return new TimeSpan(0, 30, 0);
			}
		}

		public virtual TimeSpan WorkBegin
		{
			get
			{
				return new TimeSpan(9, 0, 0);
			}
		}

		public static Partner GetPartnerForLogin(string login)
		{
			return FindAllByProperty("Login", login).FirstOrDefault();
		}

		public virtual bool CategorieIs(string reductionName)
		{
			return Role.ReductionName == reductionName;
		}

		public virtual bool IsDiller()
		{
			return CategorieIs("Diller");
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
			return Queryable.Where(p => p.Role.ReductionName == "Agent").ToList();
		}

		public static List<Partner> GetServiceEngineers()
		{
			return Queryable.Where(p => p.Role.ReductionName == "Service").ToList();
		}

		public override void SaveAndFlush()
		{
			base.SaveAndFlush();
			var catAS = CategorieAccessSet.FindAllByProperty("Categorie", Role);
			foreach (var categorieAccessSet in catAS) {
				categorieAccessSet.AccessCat.AcceptToOne(this);
			}
		}

		public static bool RegistrLogicPartner(Partner partner, ValidatorRunner validator)
		{
			if (validator.IsValid(partner)) {
				partner.Role.Refresh();
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

		public virtual bool HavePermissionTo(string controller, string action)
		{
			return Permissions().Any(p => p.Match(controller, action));
		}

		public virtual IEnumerable<IPermission> Permissions()
		{
			var permissionMap = new Dictionary<string, IPermission[]> {
				{
					"SSI", new IPermission[] {
						new ControllerPermission(typeof(PaymentsController)),
						new ControllerPermission(typeof(ChannelGroupsController)),
						new ControllerPermission(typeof(InvoicesController)),
						new ControllerPermission(typeof(ActsController)),
						new ControllerPermission(typeof(ContractsController)),
						new ControllerPermission(typeof(ServicesController)),
						new ControllerPermission(typeof(TariffsController)),
						new ControllerPermission(typeof(ExportController)),
						new ControllerPermission(typeof(MapController)),
						new ControllerActionPermission("Sms", "GetSmsStatus"),
						new ControllerActionPermission("UserInfo", "RemakeVirginityClient"),
						new ControllerActionPermission("UserInfo", "DeleteGraph"),
						new ControllerActionPermission("Brigads", "ReportOnWork"),
						new ControllerActionPermission("UserInfo", "ShowRegions"),
						new ControllerActionPermission("UserInfo", "EditRegion"),
						new ControllerActionPermission("UserInfo", "RegisterRegion")
					}
				},
				{
					"DHCP", new IPermission[] {
						new ControllerPermission(typeof(SwitchesController)),
					}
				},
				{
					"CB", new IPermission[] {
						new ControllerActionPermission("Payments", "Cancel")
					}
				},
				{
					"ASR", new IPermission[] {
						new ControllerPermission(typeof(MapController)),
					}
				},
				{
					"RP", new IPermission[] {
						new ControllerPermission(typeof(PartnersController)),
					}
				}
			};

			var lookup = permissionMap.ToLookup(k => k.Key, k => k.Value);
			return AccesedPartner.Select(p => lookup[p].SelectMany(i => i)).SelectMany(p => p);
		}
	}
}