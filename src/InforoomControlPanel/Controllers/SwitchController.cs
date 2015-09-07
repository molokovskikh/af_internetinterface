using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	public class SwitchController : ControlPanelController
	{
		public SwitchController()
		{
			ViewBag.BreadCrumb = "Коммутаторы";
		}

		public  ActionResult Index()
		{
			return SwitchList();
		}

		/// <summary>
		/// Страница списка коммутаторов
		/// </summary>
		public ActionResult SwitchList()
		{
			ViewBag.Switches = DbSession.Query<Switch>().ToList();
			return View("SwitchList");
		}

		/// <summary>
		/// Изменение коммутаторов
		/// </summary>
		public ActionResult EditSwitch(int id)
		{
			ViewBag.Switch = DbSession.Get<Switch>(id);
			ViewBag.NetworkNodes = DbSession.Query<NetworkNode>().OrderBy(i => i.Name).ToList();
			return View();
		}

		/// <summary>
		/// Изменение коммутатора
		/// </summary>
		[HttpPost]
		public ActionResult EditSwitch([EntityBinder] Switch Switch)
		{
			var errors = ValidationRunner.Validate(Switch);
			if (errors.Length == 0) {
				DbSession.Save(Switch);
				SuccessMessage("Коммутатор успешно изменнен");
				return RedirectToAction("EditSwitch", new { id = Switch.Id });
			}
			EditSwitch(Switch.Id);
			ViewBag.Switch = Switch;
			return View();
		}

		/// <summary>
		/// Удаление у узла связи обслуживающего адреса
		/// </summary>
		public ActionResult DeleteSwitchAdress(int id)
		{
			var address = DbSession.Get<SwitchAddress>(id);
			DbSession.Delete(address);
			SuccessMessage("Адрес успешно удален");
			return RedirectToAction("EditNetworkNode", new { id = address.NetworkNode.Id });
		}

		/// <summary>
		/// Страница списка узлов связи
		/// </summary>
		public ActionResult NetworkNodeList()
		{
			var NetworkNodes = DbSession.Query<NetworkNode>().ToList();
			ViewBag.NetworkNodes = NetworkNodes;
			return View();
		}

		/// <summary>
		///Создание нового узла связи
		/// </summary>
		public ActionResult CreateNetworkNode()
		{
			ViewBag.NetworkNode = new NetworkNode();
			return View("CreateNetworkNode");
		}

		[HttpPost]
		public ActionResult CreateNetworkNode(NetworkNode NetworkNode)
		{
			var errors = ValidationRunner.Validate(NetworkNode);
			if (errors.Length == 0) {
				DbSession.Save(NetworkNode);
				SuccessMessage("Узел связи успешно добавлен");
				return RedirectToAction("NetworkNodeList");
			}
			ViewBag.NetworkNode = NetworkNode;
			return View("CreateNetworkNode");
		}

		/// <summary>
		/// Изменение узла связи
		/// </summary>
		public ActionResult EditNetworkNode(int id)
		{
			var node = DbSession.Get<NetworkNode>(id);
			var Switches = DbSession.Query<Switch>().Where(i => i.NetworkNode == null).ToList();
			ViewBag.TwistedPair = new TwistedPair() { NetworkNode = node };
			ViewBag.NetworkNode = node;
			ViewBag.Switches = Switches;
			return View("EditNetworkNode");
		}


		/// <summary>
		/// Добавление многопарника узлу связи
		/// </summary>
		[HttpPost]
		public ActionResult CreateTwistedPair([EntityBinder] TwistedPair TwistedPair)
		{
			var errors = ValidationRunner.Validate(TwistedPair);
			if (errors.Length == 0) {
				DbSession.Save(TwistedPair);
				SuccessMessage("Узел связи успешно изменен");
				return RedirectToAction("EditNetworkNode", new { id = TwistedPair.NetworkNode.Id.ToString() });
			}
			EditNetworkNode(TwistedPair.NetworkNode.Id);
			ViewBag.TwistedPair = TwistedPair;
			return View("EditNetworkNode");
		}

		/// <summary>
		/// Изменение узла связи
		/// </summary>
		[HttpPost]
		public ActionResult EditNetworkNode([EntityBinder] NetworkNode NetworkNode)
		{
			var errors = ValidationRunner.Validate(NetworkNode);
			if (errors.Length == 0) {
				DbSession.Save(NetworkNode);
				SuccessMessage("Узел связи успешно изменен");
				return RedirectToAction("NetworkNodeList");
			}
			ViewBag.NetworkNode = NetworkNode;
			return View("EditNetworkNode");
		}

		/// <summary>
		/// Удаление многопарника у узла связи
		/// </summary>
		public ActionResult DeleteTwistedPair(int id)
		{
			var TwistedPair = DbSession.Get<TwistedPair>(id);
			DbSession.Delete(TwistedPair);
			SuccessMessage("Узел связи успешно изменен");
			return RedirectToAction("EditNetworkNode", new { id = TwistedPair.NetworkNode.Id });
		}

		/// <summary>
		/// Удаление узла связи
		/// </summary>
		public ActionResult DeleteNetworkNode(int id)
		{
			SafeDelete<NetworkNode>(id);
			return RedirectToAction("NetworkNodeList");
		}
	}
}