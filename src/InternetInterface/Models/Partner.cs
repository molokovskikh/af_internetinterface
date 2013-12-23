using System;
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
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.SqlCommand;

namespace InternetInterface.Models
{
	[ActiveRecord("Partners", Schema = "internet", Lazy = true)]
	public class Partner : ValidActiveRecordLinqBase<Partner>
	{
		public static TimeSpan DefaultWorkBegin = new TimeSpan(9, 0, 0);
		public static TimeSpan DefaultWorkEnd = new TimeSpan(19, 0, 0);
		public static TimeSpan DefaultWorkStep = new TimeSpan(0, 30, 0);

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
			if (role.ReductionName == "Service") {
				WorkBegin = DefaultWorkBegin;
				WorkEnd = DefaultWorkEnd;
				WorkStep = DefaultWorkStep;
			}
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

		[Property]
		public virtual TimeSpan? WorkBegin { get; set; }

		[Property]
		public virtual TimeSpan? WorkEnd { get; set; }

		[Property]
		public virtual TimeSpan? WorkStep { get; set; }

		[BelongsTo("Categorie", Lazy = FetchWhen.OnInvoke, Cascade = CascadeEnum.SaveUpdate), ValidateNonEmpty]
		public virtual UserRole Role { get; set; }

		[HasMany(ColumnKey = "Agent", OrderBy = "RegistrationDate", Lazy = true)]
		public virtual IList<PaymentsForAgent> Payments { get; set; }

		public virtual IList<string> AccesedPartner { get; set; }

		public virtual void ValidateSelf(ErrorSummary errors)
		{
			if (Role != null && Role.ReductionName == "Service") {
				if (WorkBegin == null) {
					errors.RegisterErrorMessage("WorkBegin", "Поле должно быть заполено");
				}
				if (WorkEnd == null) {
					errors.RegisterErrorMessage("WorkEnd", "Поле должно быть заполено");
				}
				if (WorkStep == null) {
					errors.RegisterErrorMessage("WorkStep", "Поле должно быть заполено");
				}
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

		public static List<Partner> GetHouseMapAgents(ISession session)
		{
			return session.Query<Partner>().Where(p => p.Role.ReductionName == "Agent").OrderBy(p => p.Name).ToList();
		}

		public static List<Partner> GetServiceEngineers(ISession session)
		{
			return session.Query<Partner>().Where(p => p.Role.ReductionName == "Service").OrderBy(p => p.Name).ToList();
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
						new ControllerPermission(typeof(ServiceRequestController)),
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