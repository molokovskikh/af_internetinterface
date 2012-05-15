using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;
using log4net;

namespace InternetInterface.Controllers
{
	public class SelectOperator
	{
		public string Operator { get; set; }
		public string OptionName { get; set; }
	}

	[FilterAttribute(ExecuteWhen.BeforeAction, typeof (AuthenticationFilter))]
	public class HouseMapController : BaseController
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(HouseMapController));

		public void HouseEdit()
		{
		}

		public void NetworkSwitches(int id)
		{
			PropertyBag["id"] = id;
			PropertyBag["Switches"] = Models.NetworkSwitches.All(DbSession);
			CancelLayout();
		}

		public void ViewHouseInfo()
		{
			PropertyBag["Houses"] = House.FindAll().OrderBy(h => h.Street);
			PropertyBag["SelectedHouse"] = House.FindFirst().Id;
			PropertyBag["agents"] = Partner.GetHouseMapAgents();
			FindHouse();
		}

		public void BasicHouseInfo(uint id)
		{
			PropertyBag["Editing"] = House.Find(id).ApartmentCount == 0;
			PropertyBag["sHouse"] = House.Find(id);
			PropertyBag["sStatuses"] = ApartmentStatus.Queryable.Where(s => s.ShortName != "Request").ToList();
			CancelLayout();
		}

		public void ForPrintToAgent(uint id)
		{
			PropertyBag["ApStatuses"] = ApartmentStatus.FindAll();
			PropertyBag["sHouse"] = House.Find(id);
			PropertyBag["ForPrint"] = true;
		}

		public void EditHouse(uint House)
		{
			PropertyBag["house"] = Models.House.Find(House);
			PropertyBag["Entrances"] = Entrance.Queryable.Where(e => e.House.Id == House).ToList();
			PropertyBag["Switches"] = Models.NetworkSwitches.All(DbSession);
		}

		[AccessibleThrough(Verb.Post)]
		public void EditHouse([ARDataBind("house", AutoLoad = AutoLoadBehavior.Always)] House house, [ARDataBind("Entrances")] Entrance[] enterances)
		{
			house.Update();
			foreach (var enterance in Entrance.Queryable.Where(e => e.House.Id == house.Id).ToList())
				enterance.Delete();

			var enCount = 0;
			foreach (var enterance in enterances)
			{
				enCount++;
				var networkSwitche =  DbSession.Get<NetworkSwitches>(enterance.Switch.Id);
				new Entrance {
					Cable = enterance.Cable,
					House = house,
					Number = enCount,
					Strut = enterance.Strut,
					Switch = networkSwitche
				 }.Save();
			}
			EditHouse(house.Id);
			RedirectToUrl("..//HouseMap/ViewHouseInfo.rails?House=" + house.Id);
		}

		public void FindHouse()
		{
			PropertyBag["SelectEn"] = new List<SelectOperator> {
																   new SelectOperator
																   {Operator = " = ", OptionName = "Равно"},
																   new SelectOperator
																   {Operator = " > ", OptionName = "Больше"},
																   new SelectOperator
																   {Operator = " < ", OptionName = "Меньше"}
															   };
		}

		public void HouseFindResult(string adress, int SubscriberCount, int PenetrationPercent, int PassCount,
									DateTime startDate, DateTime endDate,
									string SubscriberCount_Oper, string PenetrationPercent_Oper, string PassCount_Oper)
		{
			var where = string.Empty;
			if (!string.IsNullOrEmpty(adress))
				where += "and h.Street like :houseAdress ";
			if (PassCount != 0)
				where += string.Format(" and h.PassCount {0} :PassCount", PassCount_Oper);
			if ((startDate != DateTime.MinValue) && (endDate != DateTime.MinValue))
				where += " and h.LastPassDate >= :startDate and h.LastPassDate <= :endDate";
			if (where != string.Empty)
				where = " WHERE " + where.Remove(0, 4);
			where += " group by h.id ";
			var having = string.Empty;
			if (SubscriberCount != 0)
				having += string.Format(" and Count(pc.id) {0} :SubscriberCount", SubscriberCount_Oper);
			if (PenetrationPercent != 0)
				having += string.Format(" and Count(pc.id)/h.ApartmentCount {0} :PenetrationPercent",
										PenetrationPercent_Oper);
			if (having != string.Empty)
				having = " HAVING " + having.Remove(0, 4);
			var sqlStr =
				@"select h.*  from internet.Houses h 
			left join internet.PhysicalClients pc on pc.HouseObj = h.id " +
				where + having;
			var query = DbSession.CreateSQLQuery(sqlStr).AddEntity(typeof (House));
			if (!string.IsNullOrEmpty(adress))
				query.SetParameter("houseAdress", '%' + adress + '%');
			if (SubscriberCount != 0)
				query.SetParameter("SubscriberCount", SubscriberCount);
			if (PenetrationPercent != 0)
				query.SetParameter("PenetrationPercent", PenetrationPercent);
			if (PassCount != 0)
				query.SetParameter("PassCount", PassCount);
			if ((startDate != DateTime.MinValue) && (endDate != DateTime.MinValue))
			{
				query.SetParameter("startDate", startDate);
				query.SetParameter("endDate", endDate);
			}
			var result = query.List<House>();
			PropertyBag["Houses"] = result;
			PropertyBag["agents"] = Partner.GetHouseMapAgents();
			FindHouse();
		}


		[return: JSONReturnBinder]
		public houseReturned Register()
		{
			var returnObj = new houseReturned();
			var street = Request.Form["Street"];
			var number = Request.Form["Number"];
			var _case = Request.Form["Case"];
			int res;
			if (!Int32.TryParse(number, out res))
			{
				returnObj.errorMessage += "Неправильно введен номер дома";
			}
			if (string.IsNullOrEmpty(returnObj.errorMessage))
			{
				if (House.Queryable.Any(h => h.Street == street && h.Number == Int32.Parse(number) && h.Case == _case))
					returnObj.errorMessage += "Дом с таким одресом уже существует";
			}
			if (string.IsNullOrEmpty(returnObj.errorMessage))
			{
				var newHouse = new House { Street = street, Number = Int32.Parse(number), Case = _case };
				newHouse.Save();
				returnObj.houseId = (int)newHouse.Id;
				return returnObj;
			}
			return returnObj;
		}

		[return: JSONReturnBinder]
		public string SaveApartment()
		{
			var message = string.Empty;
			var house = Request.Form["House"];
			var apartment = Request.Form["Apartment"];
			var last_inet = Request.Form["last_inet"];
			var last_TV = Request.Form["last_TV"];
			var comment = Request.Form["comment"];
			var statusId = UInt32.Parse(Request.Form["status"]);
			var status = statusId > 0 ? ApartmentStatus.Find(statusId) : null;
			var apps =
				Apartment.Queryable.Where(a => a.Number == Int32.Parse(apartment) && a.House.Id == Int32.Parse(house)).
					ToList();
			if (apps.Count != 0)
			{
				foreach (var app in apps)
				{
					app.LastInternet = last_inet;
					app.LastTV = last_TV;
					app.Comment = comment;
					app.Status = status;
					app.Update();
					CreateAppealHistoryElement(app, last_inet, last_TV, comment, status);
				}
				message = "Информация обновлена";
			}
			else
			{
				apps.Add(
					new Apartment {
									  House = House.Find(Convert.ToUInt32(house)),
									  Number = Int32.Parse(apartment),
									  LastInternet = last_inet,
									  LastTV = last_TV,
									  Status = status,
									  Comment = comment
								  });
				foreach (var app in apps)
				{
					app.Save();
					CreateAppealHistoryElement(app, last_inet, last_TV, comment, status);
				}
				message = "Информация сохранена";
			}

			return message;
		}

		private void CreateAppealHistoryElement(Apartment apartment, string lastInet, string lastTv, string comment, ApartmentStatus status)
		{
			new ApartmentHistory {
									 Agent = InitializeContent.Partner,
									 Apartment = apartment,
									 ActionName =
										 string.Format(
											 "<b> Установлены параметры </b> - <br /> Интернет:{0} <br /> TV: {1} <br /> Статус:{2} <br /> Комментарий:{3}",
											 lastInet, lastTv, status != null ? status.Name : string.Empty, comment),
									 ActionDate = DateTime.Now
								 }.Save();
		}

		[return: JSONReturnBinder]
		public bool SaveHouseMap()
		{
			var SelectHouse = Request.Form["SelectHouse"];
			var NetSwitch = Request.Form["NetSwitch[]"].Split(new[] {','});
			var Strut = Request.Form["Strut[]"].Split(new[] {','});
			var Cable = Request.Form["Cable[]"].Split(new[] {','});
			var apCount = Request.Form["ApCount"];
			var house = House.Find(Convert.ToUInt32(SelectHouse));
			house.ApartmentCount = Convert.ToInt32(apCount);
			house.Update();
			for (int i = 0; i < NetSwitch.Length; i++)
			{
				new Entrance {
								 Cable = Convert.ToBoolean(Cable[i]),
								 House = house,
								 Number = i + 1,
								 Strut = Convert.ToBoolean(Strut[i]),
								 Switch = Convert.ToInt32(NetSwitch[i]) > 0 ? DbSession.Load<NetworkSwitches>(Convert.ToUInt32(NetSwitch[i])) : null
							 }.Save();
			}
			return true;
		}

		[return: JSONReturnBinder]
		public void V_Prohod()
		{
			var agent = Partner.Find(UInt32.Parse(Request.Form["agent"]));
			var date_agent = DateTime.Parse(Request.Form["date_agent"]);
			var house = House.Find(UInt32.Parse(Request.Form["house"]));
			new BypassHouse {
								Agent = agent,
								House = house,
								BypassDate = date_agent
							}.Save();
			house.LastPassDate = date_agent;
			house.PassCount++;
			house.Update();
		}

		[return: JSONReturnBinder]
		public int GetCompetitorCount()
		{
			return House.Find(UInt32.Parse(Request.Form["House"])).CompetitorCount;
		}

		[return: JSONReturnBinder]
		public object GetApartment()
		{
			try
			{
				var house = Request.Form["House"];
				var apartment = Int32.Parse(Request.Form["apartment_num"]);
				var apps = Apartment.Queryable.Where(a => a.Number == apartment && a.House.Id == Int32.Parse(house)).
					ToList().FirstOrDefault();
				if (apps != null)
				{
					if (apps.Status != null)
						return new {status = apps.Status.Id, apps.LastInternet, apps.LastTV};
					else
						return new {apps.LastInternet, apps.LastTV};
				}
			}
			catch (Exception ex)
			{
				_log.Error(string.Format("Возникла ошибка GetApartment, номер квартиры: {0}, номер дома: {1}, оператор: {2}",
				                         Request.Form["apartment_num"], Request.Form["House"], InitializeContent.Partner.Name));
			}
			return new { status = 0 };
		}


		[return: JSONReturnBinder]
		public List<HistoryInfo> LoadApartmentHistory()
		{
			var house = Request.Form["House"];
			var apartment = Int32.Parse(Request.Form["apartment_num"]);
			var apps =
				Apartment.Queryable.Where(a => a.Number == apartment && a.House.Id == Int32.Parse(house)).
					ToList().FirstOrDefault();
			if (apps != null)
				return
					ApartmentHistory.Queryable.Where(a => a.Apartment == apps).ToList().OrderByDescending(
						a => a.ActionDate).Select(
							a => new HistoryInfo(a)).ToList();
			return new List<HistoryInfo>();
		}

		public class HistoryInfo
		{
			public string Number;
			public string ActionName;
			public string Agent;
			public string ActionDate;

			public HistoryInfo()
			{
			}

			public HistoryInfo(ApartmentHistory history)
			{
				Number = history.Apartment.Number.ToString();
				ActionName = history.ActionName;
				Agent = history.Agent.Name;
				ActionDate = history.ActionDate.ToShortDateString();
			}
		}

		public class houseReturned
		{
			public houseReturned()
			{
				houseId = 0;
				errorMessage = String.Empty;
			}

			public int houseId;
			public string errorMessage;
		}
	}
}