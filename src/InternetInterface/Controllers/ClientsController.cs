using System;
using System.Collections;
using System.Linq;
using Castle.ActiveRecord.Framework;
using Castle.MonoRail.Framework;
using Common.MySql;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Models;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Models;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	public class ClientsController : BaseController
	{
		[return: JSONReturnBinder]
		public object Search(string text)
		{
			uint id;
			var dissolved = Status.Get(StatusType.Dissolved, DbSession);
			uint.TryParse(text, out id);
			return DbSession.Query<Client>()
				.Where(c => c.Name.Contains(text) || c.Id == id)
				.Where(c => c.Status != dissolved)
				.Take(20)
				.ToList()
				.Select(c => new {
					id = c.Id,
					name = String.Format("[{0}]. {1}", c.Id, c.Name)
				});
		}


		/// <summary>
		/// Используется в связи с переходом на новую админку
		/// </summary>
		public void UpdateAddressByClient(int clientId, string path)
		{
			var propObj = DbSession.CreateSQLQuery(
				string.Format(@"SELECT r.Region AS region, s.Name AS street, h.Number AS house,  a.Floor AS floor,  a.Apartment AS apartment, a.Entrance AS entrance, p.Id  
								FROM internet.regions AS r  
								INNER JOIN  internet.inforoom2_street AS s ON s.Region = r.Id 
								INNER JOIN  internet.inforoom2_house AS h ON h.Street = s.Id 
								INNER JOIN  internet.inforoom2_address AS a ON h.Id = a.house 
								INNER JOIN  internet.physicalclients AS p ON a.Id = p._Address 
								INNER JOIN  internet.clients AS c ON p.Id = c.PhysicalClient 
								WHERE c.Id={0}", clientId)).UniqueResult();
			if (propObj != null) {
				var aList = (object[])propObj;
				var regionExists = DbSession.Query<RegionHouse>().FirstOrDefault(s => s.Name == aList[0]);
				var streetExists = DbSession.Query<RegionHouse>().FirstOrDefault(s => s.Name == aList[1]);
				if (streetExists == null) {
					var newStreet = new Street();
					newStreet.Name = aList[1].ToString();
					DbSession.Save(newStreet);
				}
				var newHouse = new House();
				newHouse.ApartmentCount = 100;
				string strToInt = "";
				string justStr = aList[2].ToString();
				for (int i = 0; i < justStr.Length; i++) {
					try {
						strToInt += Convert.ToInt32(justStr[i].ToString()).ToString();
					}
					catch (Exception) {
						break;
					}
				}

				newHouse.Number = Convert.ToInt32(strToInt);
				newHouse.Street = aList[1].ToString();
				newHouse.Region = regionExists ?? new RegionHouse() { Name = aList[0].ToString() };
				DbSession.Save(newHouse);

				DbSession.CreateSQLQuery(string.Format(" UPDATE internet.physicalclients SET City='{0}',Street='{1}',House='{2}',Floor='{3}',Apartment='{4}', " +
													   "Entrance='{5}',HouseObj={6}  WHERE Id={7}", 
													    aList[0], aList[1], newHouse.Number,
														aList[3] == "" ? "0" : aList[3],
														aList[4] == "" ? "0" : aList[4],
														aList[5] == "" ? "0" : aList[5],
														newHouse.Id, aList[6]) + "; " +
										 string.Format(" UPDATE internet.clients SET Address='улица {1} дом {2} квартира {4} подъезд {5} этаж {3}' " +
													   " WHERE Id={6}", aList[0], 
													   aList[1], newHouse.Number,
													   aList[3] == "" ? "0" : aList[3],
													   aList[4] == "" ? "0" : aList[4],
													   aList[5] == "" ? "0" : aList[5],
													   clientId)).UniqueResult();
			}

			RedirectToUrl(path);
		}
	}
}