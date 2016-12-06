using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;
using System.Net;
using InforoomControlPanel.Helpers;
using Remotion.Linq.Clauses;

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
			// формирование фильтра
			var pager = new InforoomModelFilter<Switch>(this);
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Id", OrderingDirection.Desc);
			//получение критерия для Hibernate запроса из класса ModelFilter
			var criteria = pager.GetCriteria();
            ViewBag.pager = pager; 
			return View("SwitchList");
		}
		/// <summary>
		/// Изменение коммутаторов
		/// </summary>
		public ActionResult SwitchCreate()
		{
			ViewBag.Zones = DbSession.Query<Zone>().OrderBy(i => i.Name).ToList();
			ViewBag.NetworkNodes = DbSession.Query<NetworkNode>().OrderBy(i => i.Name).ToList();
			return View();
		}

		/// <summary>
		/// Изменение коммутатора
		/// </summary>
		[HttpPost]
		public ActionResult SwitchCreate([EntityBinder] Switch Switch, string switchIp)
		{ 
			IPAddress address = null;
			IPAddress.TryParse(switchIp, out address);
			Switch.Ip = address;
                var errors = ValidationRunner.Validate(Switch);
			if (errors.Length == 0)
			{
				DbSession.Save(Switch);
				SuccessMessage("Коммутатор успешно добавлен");
				return RedirectToAction("SwitchList");
			} 
			ViewBag.Zones = DbSession.Query<Zone>().OrderBy(i => i.Name).ToList();
			ViewBag.NetworkNodes = DbSession.Query<NetworkNode>().OrderBy(i => i.Name).ToList();
			ViewBag.Switch = Switch;
			return View();
		}

		/// <summary>
		/// Изменение коммутаторов
		/// </summary>
		[HttpGet]
		public ActionResult EditSwitch(int id)
		{
			ViewBag.Switch = DbSession.Get<Switch>(id);
			ViewBag.Zones = DbSession.Query<Zone>().OrderBy(i => i.Name).ToList();
			ViewBag.NetworkNodes = DbSession.Query<NetworkNode>().OrderBy(i => i.Name).ToList();
			ViewBag.SwitchLeaseCount = DbSession.Query<Lease>().Count(s => s.Switch != null && s.Switch.Id == id);
			return View();
		}

		/// <summary>
		/// Изменение коммутатора
		/// </summary>
		[HttpPost]
		public ActionResult EditSwitch([EntityBinder] Switch Switch, string switchIp)
		{
			IPAddress address = null;
			IPAddress.TryParse(switchIp, out address);
			Switch.Ip = address;
			var errors = ValidationRunner.Validate(Switch);
			if (errors.Length == 0) {
				DbSession.Save(Switch);
				SuccessMessage("Коммутатор успешно изменнен");
				return RedirectToAction("EditSwitch", new {id = Switch.Id});
			}
			EditSwitch(Switch.Id);
			ViewBag.Switch = Switch;
			ViewBag.SwitchLeaseCount = DbSession.Query<Lease>().Count(s => s.Switch != null && s.Switch.Id == Switch.Id);
			return View();
		}
		/// <summary>
		/// Удаление коммутатора
		/// </summary>
		[HttpPost]
		public ActionResult SwitchRemove(int id)
		{
			var switchToRemove = DbSession.Get<Switch>(id);
			if (switchToRemove != null
				&& switchToRemove.Endpoints.Count(s => !s.Disabled) == 0
				&& DbSession.Query<Lease>().Count(s => s.Switch != null && s.Switch.Id == id) == 0) {
				DbSession.Delete(switchToRemove);
				SuccessMessage("Коммутатор успешно удален");
			} else {
				ErrorMessage("Коммутатор не может быть удален, т.к. имеются активные точки подключения/лизы.");
			}
			return RedirectToAction("SwitchList");
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
		/// Страница списка коммутаторов
		/// </summary>
		public ActionResult RegionIpPools()
		{
			ViewBag.IpPools = DbSession.Query<IpPool>().OrderBy(i => i.Begin).ToList();
			ViewBag.Regions = DbSession.Query<Region>().OrderBy(i => i.Name).ToList();
			ViewBag.IpPoolRegions = DbSession.Query<IpPoolRegion>().OrderBy(i => i.Region.Name).ToList();
			return View();
		}
		/// <summary>
		/// Изменение коммутатора
		/// </summary>
		[HttpPost]
		public ActionResult RegionIpPoolAdd([EntityBinder] IpPoolRegion newIpPoolRegion)
		{
			var errors = ValidationRunner.Validate(newIpPoolRegion);
			if (errors.Length == 0)
			{
				DbSession.Save(newIpPoolRegion);
				SuccessMessage($"IP-пул {newIpPoolRegion.IpPool.GetBeginIp() + " - " + newIpPoolRegion.IpPool.GetEndIp()} для региона {newIpPoolRegion.Region.Name} успешно удален");
			}
			else {
				ErrorMessage("IP-пул не был добавлен");
			}
			return RedirectToAction("RegionIpPools");
		}
		/// <summary>
		/// Удаление у узла связи обслуживающего адреса
		/// </summary>
		public ActionResult RegionIpPoolDelete(int id)
		{
			var ipPoolRegion = DbSession.Get<IpPoolRegion>(id);
			DbSession.Delete(ipPoolRegion);
			SuccessMessage($"IP-пул {ipPoolRegion.IpPool.GetBeginIp() +" - "+ ipPoolRegion.IpPool.GetEndIp()} для региона {ipPoolRegion.Region.Name} успешно удален");
			return RedirectToAction("RegionIpPools");
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

		/// <summary>
		/// Информация о соединении
		/// </summary>
		public ActionResult ClientEndPointState(int id)
		{  
			return View(id);
		}
	}
}