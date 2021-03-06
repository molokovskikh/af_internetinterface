﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Helpers
{
	public class UpdateOldAddressHelper
	{
		/// <summary>
		/// Используется в связи с переходом на новую админку
		/// </summary>
		public static void UpdateOldAddressOfPhysicByClientId(int clientId, ISession dbSession)
		{
			var propObj = dbSession.CreateSQLQuery(
				string.Format(@"SELECT r.Region AS region, s.Name AS street, h.Number AS house,  a.Floor AS floor,
								a.Apartment AS apartment, a.Entrance AS entrance, p.Id, ci.Name, hr.Region AS houseRegion  , hr.Region AS houseCity
								FROM internet.regions AS r  
								INNER JOIN  internet.inforoom2_city AS ci ON r._City = ci.Id 
								INNER JOIN  internet.inforoom2_street AS s ON s.Region = r.Id 
								INNER JOIN  internet.inforoom2_house AS h ON h.Street = s.Id 
								INNER JOIN  internet.inforoom2_address AS a ON h.Id = a.house 
								INNER JOIN  internet.physicalclients AS p ON a.Id = p._Address 
								INNER JOIN  internet.clients AS c ON p.Id = c.PhysicalClient 
								LEFT JOIN  internet.regions AS hr ON hr.Id = h.Region  
								LEFT JOIN  internet.inforoom2_city AS hc ON hr._City = hc.Id 
								WHERE c.Id={0}", clientId)).UniqueResult();
			if (propObj != null) {
				var aList = (object[])propObj;
				var regionExists = dbSession.Query<RegionHouse>().FirstOrDefault(s => s.Name == (aList[8] ?? aList[0]));
				var streetExists = dbSession.Query<Street>().FirstOrDefault(s => s.Name == aList[1]);
				if (streetExists == null) {
					var newStreet = new Street();
					newStreet.Name = aList[1].ToString();
					dbSession.Save(newStreet);
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
				newHouse.Number = string.IsNullOrEmpty(strToInt) ? 0 : Convert.ToInt32(strToInt);
				newHouse.Case = justStr.Substring(strToInt.Length, justStr.Length - strToInt.Length);
				newHouse.Street = aList[1].ToString();
				newHouse.Region = regionExists ?? new RegionHouse() { Name = (aList[8] ?? aList[0]).ToString() };
				dbSession.Save(newHouse);
				string streetOrHouseTown = (aList[9] ?? aList[7]).ToString();
				dbSession.CreateSQLQuery(string.Format(
					@" UPDATE internet.physicalclients SET City='{0}',Street='{1}',House='{2}',Floor='{3}',Apartment='{4}', Entrance='{5}',HouseObj={6}, CaseHouse='{8}'  WHERE Id={7}",
					streetOrHouseTown, aList[1], newHouse.Number, aList[3] == "" ? "0" : aList[3], aList[4] == "" ? "0" : aList[4],
					aList[5] == "" ? "0" : aList[5], newHouse.Id, aList[6], newHouse.Case) + "; " +
				                         string.Format(" UPDATE internet.clients SET Address='улица {1} дом {2} квартира {4} подъезд {5} этаж {3}' " + " WHERE Id={6}", (aList[8] ?? aList[0]),
					                         aList[1], newHouse.Number + newHouse.Case, aList[3] == "" ? "0" : aList[3], aList[4] == "" ? "0" : aList[4], aList[5] == "" ? "0" : aList[5], clientId)).UniqueResult();
			}
		}
	}
}