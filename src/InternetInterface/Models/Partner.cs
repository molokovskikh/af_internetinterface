using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Tools.Calendar;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Models.Security;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models.Universal;
using NHibernate;
using NHibernate.Linq;

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

		public Partner(string login, UserRole role)
			: this(role)
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

		[Property]
		public virtual bool IsDisabled { get; set; }

		[Property]
		public virtual bool ShowContractOfAgency { get; set; }

		[BelongsTo("Categorie", Lazy = FetchWhen.OnInvoke, Cascade = CascadeEnum.SaveUpdate), ValidateNonEmpty]
		public virtual UserRole Role { get; set; }

		[HasMany(ColumnKey = "Agent", OrderBy = "RegistrationDate", Lazy = true)]
		public virtual IList<PaymentsForAgent> Payments { get; set; }

		[HasMany(ColumnKey = "Agent", OrderBy = "RegistrationDate", Lazy = true)]
		public virtual IList<Payment> CommonPayments { get; set; }

		public virtual IList<string> AccesedPartner { get; set; }

		public virtual bool AccesPartner(string reduseRulesName)
		{
			return AccesedPartner.Contains(reduseRulesName);
		}

		public virtual bool NeedShowAgenceContract(Payment payment)
		{
			return payment != null && ShowContractOfAgency && payment.Client.IsPhysical() && !payment.Virtual;
		}

		public virtual void ValidateSelf(ErrorSummary errors)
		{
			if (Role != null && Role.ReductionName == "Service") {
				if (WorkBegin == null) {
					errors.RegisterErrorMessage("WorkBegin", "Поле должно быть заполнено");
				}
				if (WorkEnd == null) {
					errors.RegisterErrorMessage("WorkEnd", "Поле должно быть заполнено");
				}
				if (WorkStep == null) {
					errors.RegisterErrorMessage("WorkStep", "Поле должно быть заполнено");
				}
			}
		}

		public static Partner GetPartnerForLogin(string login)
		{
			return ArHelper.WithSession(s => s.Query<Partner>().FirstOrDefault(p => p.Login == login && !p.IsDisabled));
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
			return
				session.Query<Partner>()
					.Where(p => p.Role.ReductionName == "Service" && !p.IsDisabled)
					.OrderBy(p => p.Name)
					.ToList();
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
					//Просмотр личных данных
					"SSI", new IPermission[] {
						new ControllerPermission(typeof (PaymentsController)),
						new ControllerPermission(typeof (ChannelGroupsController)),
						new ControllerPermission(typeof (InvoicesController)),
						new ControllerPermission(typeof (ActsController)),
						new ControllerPermission(typeof (ContractsController)),
						new ControllerPermission(typeof (ServicesController)),
						new ControllerPermission(typeof (TariffsController)),
						new ControllerPermission(typeof (ExportController)),
						new ControllerPermission(typeof (MapController)),
						new ControllerPermission(typeof (TvRequestController)),
						new ControllerPermission(typeof (IpPoolsController)),
						new ControllerActionPermission("Sms", "GetSmsStatus"),
						new ControllerActionPermission("UserInfo", "RemakeVirginityClient"),
						new ControllerActionPermission("UserInfo", "DeleteGraph"),
						new ControllerActionPermission("Brigads", "ReportOnWork"),
						new ControllerActionPermission("UserInfo", "ShowRegions"),
						new ControllerActionPermission("UserInfo", "EditRegion"),
						new ControllerActionPermission("UserInfo", "RegisterRegion"),
						new ControllerPermission(typeof (RentableHardwaresController))
					}
				}, {
					"DHCP", new IPermission[] {
						new ControllerPermission(typeof (SwitchesController))
					}
				}, {
					"CB", new IPermission[] {
						new ControllerActionPermission("Payments", "Cancel")
					}
				}, {
					"ASR", new IPermission[] {
						new ControllerPermission(typeof (ServiceRequestController))
					}
				}, {
					"RP", new IPermission[] {
						new ControllerPermission(typeof (PartnersController))
					}
				}, {
					"VD", new IPermission[] {
						new ControllerPermission(typeof (ConnectionRequestController))
					}
				}, {
					//Управление бригадами
					"MB", new IPermission[] {
						new ControllerPermission(typeof (BrigadsController))
					}
				}
			};

			ILookup<string, IPermission[]> lookup = permissionMap.ToLookup(k => k.Key, k => k.Value);
			return AccesedPartner.Select(p => lookup[p].SelectMany(i => i)).SelectMany(p => p);
		}

		public static IList<Partner> All(ISession session)
		{
			return session.Query<Partner>().OrderBy(p => p.Name).ToList();
		}

		public static IList<Partner> All()
		{
			return ArHelper.WithSession(s => s.Query<Partner>().OrderBy(p => p.Name).ToList());
		}

		public static Partner GetInitPartner()
		{
			return InitializeContent.Partner;
		}
	}
}