using System;
using System.Linq;
using System.Net;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.WebPages;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;
using NHibernate.Util;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления тарифами пользователя
	/// </summary>
	public class PlansController : ControlPanelController
	{
		public PlansController()
		{
			ViewBag.BreadCrumb = "Тарифы";
		}

		public ActionResult Index()
		{
			return PlanIndex();
		}

		/// <summary>
		/// Список тарифов
		/// </summary>
		public ActionResult PlanIndex()
		{
			var plans = DbSession.Query<Plan>().OrderByDescending(i => i.Priority).ToList();
			var regions = DbSession.Query<Region>().ToList();
			ViewBag.Plans = plans;
			ViewBag.Regions = regions;
			return View("PlanIndex");
		}

		/// <summary>
		/// Форма добавление тарифа
		/// </summary>
		public ActionResult CreatePlan()
		{
			//Создаентся тарифный план  
			var plan = new Plan();
			ViewBag.Plan = plan;
			ViewBag.PackageSpeed = DbSession.Query<PackageSpeed>().Where(s => s.Confirmed).OrderBy(s => s.Speed)
				.GroupBy(s => s.Speed).Select(grp => grp.First()).ToList();

			return View();
		}

		/// <summary>
		/// Добавление тарифа в базу	(ValidateInput = false указываем для получения HTML со страницы)
		/// </summary>
		[HttpPost, ValidateInput(false)]
		public ActionResult CreatePlan([EntityBinder] Plan plan)
		{
			var errors = ValidationRunner.ValidateDeep(plan);
			if (errors.Length == 0) {
				DbSession.Save(plan);
				SuccessMessage("Тарифный план успешно добавлен!");
				return RedirectToAction("PlanIndex");
			}
			ViewBag.Plan = plan;
			ViewBag.PackageSpeed = DbSession.Query<PackageSpeed>().Where(s => s.Confirmed).OrderBy(s => s.Speed)
				.GroupBy(s => s.Speed).Select(grp => grp.First()).ToList();

			return View("CreatePlan");
		}

		/// <summary>
		/// Просмотр тарифа
		/// </summary>
		public ActionResult EditPlan(int id)
		{
			//забирается тарифный план из базы данных
			var plan = DbSession.Get<Plan>(id);
			//создание промежуточного объекта для перехода между тарифами
			var PlanTransfer = new PlanTransfer();
			//назначение поля тарифа
			PlanTransfer.PlanFrom = plan;
			var plans = DbSession.Query<Plan>().OrderByDescending(i => i.Id).ToList();
			foreach (var transfer in plan.PlanTransfers) plans.Remove(transfer.PlanTo);

			var RegionPlan = new RegionPlan();
			RegionPlan.Plan = plan;
			var regions = DbSession.Query<Region>().ToList();
			foreach (var rp in plan.RegionPlans) regions.Remove(rp.Region);
			ViewBag.PackageSpeed = DbSession.Query<PackageSpeed>().Where(s => s.Confirmed).OrderBy(s => s.Speed)
				.GroupBy(s => s.Speed).Select(grp => grp.First()).ToList();
			var channelGroups = DbSession.Query<TvChannelGroup>().ToList();
			plan.TvChannelGroups.ForEach(i => channelGroups.Remove(i));
			ViewBag.Plans = plans;
			ViewBag.Plan = plan;
			ViewBag.Regions = regions;
			ViewBag.PlanTransfer = PlanTransfer;
			ViewBag.TvChannelGroups = channelGroups;
			ViewBag.RegionPlan = RegionPlan;
			return View("EditPlan", new { id = plan.Id });
		}

		/// <summary>
		/// Изменение тарифа	(ValidateInput = false указываем для получения HTML со страницы)
		/// </summary>
		[HttpPost, ValidateInput(false)]
		public ActionResult EditPlan([EntityBinder] Plan plan)
		{
			var errors = ValidationRunner.ValidateDeep(plan);
			if (errors.Length == 0) {
				DbSession.Save(plan);
				SuccessMessage("Тарифный план успешно отредактирован");
				return RedirectToAction("EditPlan", new { id = plan.Id });
			}

			EditPlan(plan.Id);
			ViewBag.Plan = plan;
			return View("EditPlan");
		}

		/// <summary>
		/// Удаление тарифа
		/// </summary>
		/// <param name="id">Идентификатор тарифа</param>
		/// <returns></returns>
		public ActionResult RemovePlan(int id)
		{
			SafeDelete<Plan>(id);
			return RedirectToAction("PlanIndex");
		}

		/// <summary>
		/// Увеличивает приоритет тарифа
		/// </summary>
		/// <param name="id">Идентификатор тарифа</param>
		/// <returns></returns>
		public ActionResult IncreasePlanShowPriority(int id)
		{
			var plan = DbSession.Get<Plan>(id);
			plan.IncereasePriority(DbSession);
			return RedirectToAction("PlanIndex");
		}

		/// <summary>
		/// Уменьшает приоритет тарифа
		/// </summary>
		/// <param name="id">Идентификатор тарифа</param>
		/// <returns></returns>
		public ActionResult DecreasePlanShowPriority(int id)
		{
			var plan = DbSession.Get<Plan>(id);
			plan.DecreasePriority(DbSession);
			return RedirectToAction("PlanIndex");
		}

		/// <summary>
		/// Список тарифов
		/// </summary>
		public ActionResult HtmlPlanIndex()
		{
			var planContent = DbSession.Query<PlanHtmlContent>().OrderByDescending(i => i.Region).ToList();
			ViewBag.PlanContent = planContent;
			return View("HtmlPlanIndex");
		}

		/// <summary>
		/// Форма добавление тарифа
		/// </summary>
		public ActionResult CreateHtmlPlan()
		{
			//Создаентся тарифный план  
			var planContent = new PlanHtmlContent();
			var regions = DbSession.Query<Region>().Where(s => s.ShownOnMainPage).OrderBy(s => s.Name).ToList();

			ViewBag.Regions = regions;
			ViewBag.PlanContent = planContent;

			return View();
		}

		/// <summary>
		/// Изменение тарифа	(ValidateInput = false указываем для получения HTML со страницы)
		/// </summary>
		[HttpPost, ValidateInput(false)]
		public ActionResult CreateHtmlPlan([EntityBinder] PlanHtmlContent planContent)
		{
			var htmlForRegionExists = DbSession.Query<PlanHtmlContent>()
				.FirstOrDefault(s => s.Id != planContent.Id && s.Region == planContent.Region);

			if (htmlForRegionExists != null) {
				htmlForRegionExists.Content = planContent.Content;
				DbSession.Save(htmlForRegionExists);
				SuccessMessage("HTML страницы 'тарифы' успешно заменен.");
				return RedirectToAction("HtmlPlanIndex");
			}
			var errors = ValidationRunner.ValidateDeep(planContent);
			if (errors.Length == 0) {
				DbSession.Save(planContent);
				SuccessMessage("HTML страницы 'тарифы' добавлена.");
				return RedirectToAction("HtmlPlanIndex");
			}

			var regions = DbSession.Query<Region>().Where(s => s.ShownOnMainPage).OrderBy(s => s.Name).ToList();
			ViewBag.Regions = regions;
			ViewBag.PlanContent = planContent;
			return View();
		}

		/// <summary>
		/// Форма добавление тарифа
		/// </summary>
		public ActionResult EditHtmlPlan(int id)
		{
			//Создаентся тарифный план  
			var planContent = DbSession.Query<PlanHtmlContent>().FirstOrDefault(s => s.Id == id);
			var regions = DbSession.Query<Region>().Where(s => s.ShownOnMainPage).OrderBy(s => s.Name).ToList();

			ViewBag.Regions = regions;
			ViewBag.PlanContent = planContent;

			return View();
		}

		/// <summary>
		/// Изменение тарифа	(ValidateInput = false указываем для получения HTML со страницы)
		/// </summary>
		[HttpPost, ValidateInput(false)]
		public ActionResult EditHtmlPlan([EntityBinder] PlanHtmlContent planContent)
		{
			var htmlForRegionExists = DbSession.Query<PlanHtmlContent>()
				.FirstOrDefault(s => s.Id != planContent.Id && s.Region == planContent.Region);

			if (htmlForRegionExists != null) {
				htmlForRegionExists.Content = planContent.Content;
				DbSession.Save(htmlForRegionExists);
				SuccessMessage("HTML страницы 'тарифы' успешно заменен.");
				return RedirectToAction("HtmlPlanIndex");
			}
			var errors = ValidationRunner.ValidateDeep(planContent);
			if (errors.Length == 0) {
				DbSession.Save(planContent);
				SuccessMessage("HTML страницы 'тарифы' отредактирован");
				return RedirectToAction("HtmlPlanIndex");
			}

			var regions = DbSession.Query<Region>().Where(s => s.ShownOnMainPage).OrderBy(s => s.Name).ToList();
			ViewBag.Regions = regions;
			ViewBag.PlanContent = planContent;
			return RedirectToAction("EditHtmlPlan", new { id = planContent.Id });
		}

		/// <summary>
		/// Удаление тарифа
		/// </summary>
		/// <param name="id">Идентификатор тарифа</param>
		/// <returns></returns>
		public ActionResult RemoveHtmlPlan(int id)
		{
			var planContent = DbSession.Get<PlanHtmlContent>(id);
			DbSession.Delete(planContent);
			return RedirectToAction("HtmlPlanIndex");
		}

		/// <summary>
		/// Добавление стоимости перехода
		/// </summary>
		public ActionResult AddPlanTransfer([EntityBinder] PlanTransfer planTransfer)
		{
			var errors = ValidationRunner.ValidateDeep(planTransfer);
			if (errors.Length == 0) {
				DbSession.Save(planTransfer);
				SuccessMessage("Стоимость перехода успешно отредактирован");
				return RedirectToAction("EditPlan", new { id = planTransfer.PlanFrom.Id });
			}
			EditPlan(planTransfer.PlanFrom.Id);
			ViewBag.PlanTransfer = planTransfer;
			return View("EditPlan");
		}

		/// <summary>
		/// Удаление стоимости перехода
		/// </summary>
		public ActionResult DeletePlanTransfer(int id)
		{
			var transfer = DbSession.Get<PlanTransfer>(id);
			DbSession.Delete(transfer);
			DbSession.Flush();
			SuccessMessage("Стоимость перехода успешно удалена");
			return RedirectToAction("EditPlan", new { id = transfer.PlanFrom.Id });
		}

		/// <summary>
		/// Добавление региона
		/// </summary>
		/// <returns></returns>
		public ActionResult AddRegionPlan([EntityBinder] RegionPlan regionPlan)
		{
			var errors = ValidationRunner.Validate(regionPlan);
			if (errors.Length == 0) {
				DbSession.Save(regionPlan);
				SuccessMessage("Регион успешно добавлен");
				return RedirectToAction("EditPlan", new { id = regionPlan.Plan.Id });
			}
			EditPlan(regionPlan.Plan.Id);
			ViewBag.RegionPlan = regionPlan;
			return View("EditPlan");
		}


		/// <summary>
		/// Удаление региона
		/// </summary>
		public ActionResult DeleteRegion(int id)
		{
			var rp = DbSession.Get<RegionPlan>(id);
			DbSession.Delete(rp);
			DbSession.Flush();
			SuccessMessage("Регион успешно удален");
			return RedirectToAction("EditPlan", new { id = rp.Plan.Id });
		}

		/// <summary>
		/// Список ТВ протоколов
		/// </summary>
		public ActionResult TvProtocolList()
		{
			var protocols = DbSession.Query<TvProtocol>().OrderByDescending(i => i.Id).ToList();
			ViewBag.TvProtocols = protocols;
			return View();
		}

		/// <summary>
		/// Создание ТВ протокола
		/// </summary>
		public ActionResult CreateTvProtocol()
		{
			var protocol = new TvProtocol();
			ViewBag.TvProtocol = protocol;
			return View();
		}

		/// <summary>
		/// Создание ТВ протокола
		/// </summary>
		[HttpPost]
		public ActionResult CreateTvProtocol([EntityBinder] TvProtocol tvProtocol)
		{
			var errors = ValidationRunner.Validate(tvProtocol);
			if (errors.Length == 0) {
				DbSession.Save(tvProtocol);
				SuccessMessage("Протокол успешно добавлен!");
				return RedirectToAction("TvProtocolList");
			}
			CreateTvProtocol();
			ViewBag.TvProtocol = tvProtocol;
			return View();
		}

		/// <summary>
		/// Измененение ТВ протокола
		/// </summary>
		public ActionResult EditTvProtocol(int id)
		{
			var tvProtocol = DbSession.Get<TvProtocol>(id);
			ViewBag.Title = "Редактирование протокола для ТВ";
			ViewBag.TvProtocol = tvProtocol;
			return View("CreateTvProtocol");
		}

		/// <summary>
		/// Измененение ТВ протокола
		/// </summary>
		[HttpPost]
		public ActionResult EditTvProtocol([EntityBinder] TvProtocol tvProtocol)
		{
			var errors = ValidationRunner.Validate(tvProtocol);
			if (errors.Length == 0) {
				DbSession.Save(tvProtocol);
				SuccessMessage("Протокол успешно изменен!");
				return RedirectToAction("TvProtocolList");
			}
			EditTvProtocol(tvProtocol.Id);
			ViewBag.TvProtocol = tvProtocol;
			return View("CreateTvProtocol");
		}

		/// <summary>
		/// Удаление ТВ протокола
		/// </summary>
		public ActionResult DeleteTvProtocol(int id)
		{
			SafeDelete<TvProtocol>(id);
			return RedirectToAction("TvProtocolList");
		}

		/// <summary>
		/// Список ТВ каналов
		/// </summary>
		public ActionResult TvChannelList()
		{
			var tvChannels = DbSession.Query<TvChannel>().OrderByDescending(i => i.Priority).ThenByDescending(i => i.Id).ToList();
			ViewBag.TvChannels = tvChannels;
			return View();
		}

		/// <summary>
		/// Увеличивает приоритет канала
		/// </summary>
		/// <param name="id">Идентификатор канала</param>
		/// <returns></returns>
		public ActionResult IncreaseTvChannelPriority(int id)
		{
			var channel = DbSession.Get<TvChannel>(id);
			channel.IncereasePriority(DbSession);
			return RedirectToAction("TvChannelList");
		}

		/// <summary>
		/// Уменьшает приоритет канала
		/// </summary>
		/// <param name="id">Идентификатор канала</param>
		/// <returns></returns>
		public ActionResult DecreaseTvChannelPriority(int id)
		{
			var channel = DbSession.Get<TvChannel>(id);
			channel.DecreasePriority(DbSession);
			return RedirectToAction("TvChannelList");
		}

		/// <summary>
		/// Создание ТВ канала
		/// </summary>
		public ActionResult CreateTvChannel()
		{
			var tvChannel = new TvChannel();
			var protocols = DbSession.Query<TvProtocol>().ToList();
			ViewBag.TvChannel = tvChannel;
			ViewBag.TvProtocols = protocols;
			return View();
		}

		/// <summary>
		/// Создание ТВ канала
		/// </summary>
		[HttpPost]
		public ActionResult CreateTvChannel([EntityBinder] TvChannel TvChannel)
		{
			var errors = ValidationRunner.Validate(TvChannel);
			if (errors.Length == 0) {
				DbSession.Save(TvChannel);
				SuccessMessage("Канал успешно добавлен!");
				return RedirectToAction("TvChannelList");
			}
			CreateTvChannel();
			ViewBag.TvChannel = TvChannel;
			return View();
		}

		/// <summary>
		/// Редактирование ТВ канала
		/// </summary>
		public ActionResult EditTvChannel(int id)
		{
			var tvChannel = DbSession.Get<TvChannel>(id);
			CreateTvChannel();
			ViewBag.Title = "Редактирование ТВ канала";
			ViewBag.TvChannel = tvChannel;
			return View("CreateTvChannel");
		}

		/// <summary>
		/// Редактирование ТВ канала
		/// </summary>
		[HttpPost]
		public ActionResult EditTvChannel([EntityBinder] TvChannel TvChannel)
		{
			var errors = ValidationRunner.Validate(TvChannel);
			if (errors.Length == 0) {
				DbSession.Save(TvChannel);
				SuccessMessage("Канал успешно изменен!");
				return RedirectToAction("TvChannelList");
			}

			EditTvChannel(TvChannel.Id);
			ViewBag.TvChannel = TvChannel;
			return View("CreateTvChannel");
		}

		/// <summary>
		/// Удаление ТВ канала
		/// </summary>
		public ActionResult DeleteTvChannel(int id)
		{
			SafeDelete<TvChannel>(id);
			return RedirectToAction("TvChannelList");
		}

		/// <summary>
		/// Список групп ТВ каналов
		/// </summary>
		public ActionResult TvChannelGroupList()
		{
			var tvChannelGroups = DbSession.Query<TvChannelGroup>().OrderByDescending(i => i.Id).ToList();
			ViewBag.TvChannelGroups = tvChannelGroups;
			return View();
		}

		/// <summary>
		/// Создание группы ТВ каналов
		/// </summary>
		public ActionResult CreateTvChannelGroup()
		{
			var tvChannelGroup = new TvChannelGroup();
			ViewBag.TvChannelGroup = tvChannelGroup;
			return View();
		}

		/// <summary>
		/// Создание группы ТВ каналов
		/// </summary>
		[HttpPost]
		public ActionResult CreateTvChannelGroup([EntityBinder] TvChannelGroup tvChannelGroup)
		{
			var errors = ValidationRunner.Validate(tvChannelGroup);
			if (errors.Length == 0) {
				DbSession.Save(tvChannelGroup);
				SuccessMessage("Объект успешно добавлен!");
				return RedirectToAction("TvChannelGroupList");
			}
			CreateTvChannelGroup();
			ViewBag.TvChannelGroup = tvChannelGroup;
			return View();
		}

		/// <summary>
		/// Редактирование группы ТВ каналов
		/// </summary>
		public ActionResult EditTvChannelGroup(int id)
		{
			var tvChannelGroup = DbSession.Get<TvChannelGroup>(id);
			CreateTvChannelGroup();
			//Выбираем каналы, которых нет у группы
			var channels = DbSession.Query<TvChannel>().ToList();
			tvChannelGroup.TvChannels.ForEach(i => channels.Remove(i));

			ViewBag.TvChannels = channels;
			ViewBag.Title = "Редактирование группы ТВ каналов";
			ViewBag.TvChannelGroup = tvChannelGroup;
			return View("CreateTvChannelGroup");
		}

		/// <summary>
		/// Редактирование группы ТВ каналов
		/// </summary>
		[HttpPost]
		public ActionResult EditTvChannelGroup([EntityBinder] TvChannelGroup tvChannelGroup)
		{
			var errors = ValidationRunner.Validate(tvChannelGroup);
			if (errors.Length == 0) {
				DbSession.Save(tvChannelGroup);
				SuccessMessage("Объект успешно изменен!");
				return RedirectToAction("EditTvChannelGroup", new { id = tvChannelGroup.Id });
			}

			EditTvChannel(tvChannelGroup.Id);
			ViewBag.TvChannelGroup = tvChannelGroup;
			return View("CreateTvChannelGroup");
		}

		/// <summary>
		/// Удаление группы ТВ каналов
		/// </summary>
		public ActionResult DeleteTvChannelGroup(int id)
		{
			SafeDelete<TvChannelGroup>(id);
			return RedirectToAction("TvChannelGroupList");
		}

		/// <summary>
		/// Добавление канала в группу каналов
		/// </summary>
		/// <param name="tvChannelGroup">Группа каналов (существующая)</param>
		/// <returns></returns>
		public ActionResult AttachTvChannel([EntityBinder] TvChannelGroup tvChannelGroup)
		{
			if (ValidationRunner.ValidateDeep(tvChannelGroup).Length == 0) {
				DbSession.Save(tvChannelGroup);
				SuccessMessage("Объект успешно прикреплен!");
			}
			else
				ErrorMessage("Объект не удалось прикрепить! Возможно вложеные объекты не являются валидными.");
			return RedirectToAction("EditTvChannelGroup", new { id = tvChannelGroup.Id });
		}

		/// <summary>
		/// Добавление группы каналов к тарифу
		/// </summary>
		/// <param name="tvChannelGroup">Тариф (существующий)</param>
		/// <returns></returns>
		public ActionResult AttachTvChannelGroup([EntityBinder] Plan plan)
		{
			if (ValidationRunner.ValidateDeep(plan).Length == 0) {
				DbSession.Save(plan);
				SuccessMessage("Объект успешно прикреплен!");
			}
			else
				ErrorMessage("Объект не удалось прикрепить! Возможно вложеные объекты не являются валидными.");
			return RedirectToAction("EditPlan", new { id = plan.Id });
		}

		public ActionResult InternetPlanChangerIndex()
		{
			var pager = new InforoomModelFilter<PlanChangerData>(this);
			pager.SetOrderBy("TargetPlan");
			ViewBag.Pager = pager;
			return View();
		}

		public ActionResult CreateInternetPlanChanger()
		{
			var planChanger = new PlanChangerData();
			var plans = DbSession.Query<Plan>().Where(s => s.Disabled == false).OrderBy(s => s.Name).ToList();
			ViewBag.PlanChanger = planChanger;
			ViewBag.Plans = plans;
			return View();
		}

		[HttpPost, ValidateInput(false)]
		public ActionResult CreateInternetPlanChanger([EntityBinder] PlanChangerData planChanger)
		{
			var errors = ValidationRunner.Validate(planChanger);
			if (errors.Length == 0) {
				DbSession.Save(planChanger);
				SuccessMessage("Объект успешно добавлен!");
				return RedirectToAction("InternetPlanChangerIndex", new { id = planChanger.Id });
			}
			var plans = DbSession.Query<Plan>().Where(s => s.Disabled == false).OrderBy(s => s.Name).ToList();
			ViewBag.PlanChanger = planChanger;
			ViewBag.Plans = plans;
			return View();
		}

		public ActionResult EditInternetPlanChanger(int id)
		{
			var planChanger = DbSession.Get<PlanChangerData>(id);
			var plans = DbSession.Query<Plan>().Where(s => s.Disabled == false).OrderBy(s => s.Name).ToList();
			ViewBag.PlanChanger = planChanger;
			ViewBag.Plans = plans;
			return View();
		}

		[HttpPost, ValidateInput(false)]
		public ActionResult EditInternetPlanChanger([EntityBinder] PlanChangerData planChanger)
		{
			var errors = ValidationRunner.Validate(planChanger);
			if (errors.Length == 0) {
				DbSession.Save(planChanger);
				SuccessMessage("Объект успешно изменен!");
				return RedirectToAction("InternetPlanChangerIndex", new { id = planChanger.Id });
			}
			var plans = DbSession.Query<Plan>().Where(s => s.Disabled == false).OrderBy(s => s.Name).ToList();
			ViewBag.PlanChanger = planChanger;
			ViewBag.Plans = plans;
			return View();
		}

		public ActionResult DeleteInternetPlanChanger(int id)
		{
			SafeDelete<PlanChangerData>(id);
			return RedirectToAction("InternetPlanChangerIndex");
		}


		public ActionResult PackageSpeedList()
		{
			var pager = new InforoomModelFilter<PackageSpeed>(this);
			var criteria = pager.GetCriteria();
			ViewBag.Pager = pager;
			return View();
		}

		[HttpPost]
		public ActionResult CreatePackageSpeed([EntityBinder] PackageSpeed packageSpeed)
		{
			var errors = ValidationRunner.Validate(packageSpeed);
			if (errors.Length == 0) {
				DbSession.Save(packageSpeed);
				string linkToConfirm = ConfigHelper.GetParam("adminPanelNew") + Url.Action("ConfirmPackageSpeed", new { id = packageSpeed.Id });
				var redmineTask = packageSpeed.AssignRedmineTask(DbSession, linkToConfirm);
				SuccessMessage("На Redmine создана задача по активации скорости - " + redmineTask.Id);
				return RedirectToAction("PackageSpeedList");
			}
			//передаем список Скоростей
			var pager = new InforoomModelFilter<PackageSpeed>(this);
			var criteria = pager.GetCriteria();
			ViewBag.Pager = pager;
			//передаем создаваемую скорость
			ViewBag.PackageSpeed = packageSpeed;
			return View("PackageSpeedList");
		}

		public ActionResult EditPackageSpeed(int id)
		{
			var packageSpeed = DbSession.Query<PackageSpeed>().FirstOrDefault(s => s.Id == id && s.Confirmed == false);
			if (packageSpeed == null) {
				SuccessMessage("Редактирование данной скорости невозможно *(вероятно она уже подтверждена)");
				return RedirectToAction("PackageSpeedList");
			}
			ViewBag.PackageSpeed = packageSpeed;
			return View();
		}

		[HttpPost]
		public ActionResult EditPackageSpeed([EntityBinder] PackageSpeed packageSpeed)
		{
			var errors = ValidationRunner.Validate(packageSpeed);
			if (errors.Length == 0 && packageSpeed.Confirmed == false) {
				DbSession.Save(packageSpeed);
				return RedirectToAction("PackageSpeedList");
			}
			ViewBag.PackageSpeed = packageSpeed;
			return View();
		}

		public ActionResult ConfirmPackageSpeed(int id)
		{
			var packageSpeed = DbSession.Get<PackageSpeed>(id);
			ViewBag.PackageSpeed = packageSpeed;
			return View();
		}

		[HttpPost]
		public ActionResult ConfirmPackageSpeed([EntityBinder] PackageSpeed packageSpeed)
		{
			var errors = ValidationRunner.Validate(packageSpeed);
			if (errors.Length == 0) {
				packageSpeed.Confirmed = true;
				DbSession.Save(packageSpeed);
				SuccessMessage("Скорость подтверждена!");
				return RedirectToAction("PackageSpeedList");
			}
			ViewBag.PackageSpeed = packageSpeed;
			return View();
		}
	}
}