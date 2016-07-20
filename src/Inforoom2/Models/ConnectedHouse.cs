using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Web.Services.Description;
using Common.Tools;
using Inforoom2.Intefaces;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель города
	/// </summary>
	[Class(0, Table = "connectedhouses", NameType = typeof(ConnectedHouse))]
	public class ConnectedHouse : BaseModel
	{
		[ManyToOne(Cascade = "save-update"), NotNull, Description("Улица")]
		public virtual Street Street { get; set; }

		[ManyToOne(Cascade = "save-update"), NotNull, Description("Регион")]
		public virtual Region Region { get; set; }

		[Property, NotEmpty(Message = "Номер дома должен быть заполнен"), Description("Номер дома")]
		public virtual string Number { get; set; }

		[Property]
		public virtual bool IsCustom { get; set; }

		[Property, Description("Комментарий")]
		public virtual string Comment { get; set; }

		[Property]
		public virtual bool Disabled { get; set; }

		public enum HouseStreetSide
		{
			Both,
			Left,
			Right
		}

		public enum HouseGenerationState
		{
			AddOrUpdate = 0,
			Add = 1,
			Update = 2,
			Remove = 3
		}

		public static void SynchronizeConnections(ISession dbSession)
		{
			var regionList = dbSession.Query<Region>().ToList();
			foreach (var region in regionList) {
				var currentAddresses = new List<Tuple<int, int?, int, string>>();
				var currentAddressesRaw = dbSession.CreateSQLQuery(string.Format(@"
SELECT targetR2.Id as Region2, targetR.Id as Region1, targetS.Id as Street, targetH.Number as Number
FROM internet.inforoom2_house as targetH
LEFT JOIN internet.inforoom2_street as targetS on targetH.Street = targetS.Id
LEFT JOIN internet.regions as targetR on targetH.Region = targetR.Id
LEFT JOIN internet.regions as targetR2 on targetS.Region = targetR2.Id
WHERE 
 targetH.Id IN 
(SELECT h.Id 
FROM internet.inforoom2_house as h
INNER JOIN internet.inforoom2_street as s on h.Street = s.Id
INNER JOIN internet.inforoom2_address as ad on ad.house = h.Id
INNER JOIN internet.physicalclients as ph on ph._Address = ad.Id
INNER JOIN internet.clients as c on ph.Id = c.PhysicalClient
WHERE 
(c.`Status` = 5 OR c.`Status` = 7 OR c.`Status` = 9 OR c.`Status` = 11)
GROUP BY h.Id) 
AND ((targetR2.Id = {0} AND targetR.Id IS NULL) OR ( targetR.Id = {0}))
ORDER BY targetS.Id
", region.Id)).List<object[]>();
				foreach (var item in currentAddressesRaw) {
					currentAddresses.Add(new Tuple<int, int?, int, string>(
						Convert.ToInt32(item[0]),
						item[1] == null ? null : new Nullable<Int32>(Convert.ToInt32(item[0])),
						Convert.ToInt32(item[2]),
						item[3].ToString()
						));
				}
				currentAddressesRaw.Clear();

				var currentConnectedHouses = new List<Tuple<int, int, int, string, bool, bool>>();
				var currentConnectedHousesRaw = dbSession.CreateSQLQuery(string.Format(@"
SELECT c.Id, c.Region, c.Street, c.Number, c.IsCustom FROM internet.inforoom2_connectedhouses as c WHERE c.Region = {0} ", region.Id)).List<object[]>();
				foreach (var item in currentConnectedHousesRaw) {
					currentConnectedHouses.Add(new Tuple<int, int, int, string, bool, bool>(
						Convert.ToInt32(item[0]),
						Convert.ToInt32(item[1]),
						Convert.ToInt32(item[2]),
						item[3].ToString(),
						Convert.ToInt32(item[3]) == 1,
						Convert.ToInt32(item[4]) == 1
						));
				}
				currentConnectedHousesRaw.Clear();

				foreach (var item in currentAddresses) {
					int regionId = Convert.ToInt32(item.Item2 ?? item.Item1);
					int streetId = Convert.ToInt32(item.Item3);
					string houseNumber = item.Item4;
					var existedConnectedHouse = currentConnectedHouses.FirstOrDefault(s => s.Item2 == regionId && s.Item3 == streetId && s.Item4 == houseNumber);
					if (existedConnectedHouse == null) {
						dbSession.CreateSQLQuery(string.Format(@"
						INSERT INTO internet.inforoom2_connectedhouses (Region,Street,Number) VALUES ({0},{1},'{2}');   
						", region.Id, streetId, houseNumber)).UniqueResult();
					}
					else {
						if (!existedConnectedHouse.Item5) {
							dbSession.CreateSQLQuery(string.Format(@"
					UPDATE  internet.inforoom2_connectedhouses as c SET c.Disabled = 0 WHERE c.Id = {0};   
						", existedConnectedHouse.Item1)).UniqueResult();
						}
					}
				}
				foreach (var item in currentConnectedHouses) {
					int regionId = item.Item2;
					int streetId = Convert.ToInt32(item.Item3);
					string houseNumber = item.Item4;
					var existedConnectedHouse = currentAddresses.FirstOrDefault(s =>
						Convert.ToInt32(s.Item2 ?? s.Item1) == regionId && s.Item3 == streetId && s.Item4 == houseNumber);
					if (existedConnectedHouse == null && !item.Item5 && !item.Item6) {
						dbSession.CreateSQLQuery(string.Format(@"
					UPDATE  internet.inforoom2_connectedhouses as c SET c.Disabled = 1 WHERE c.Id = {0};   
						", item.Item1)).UniqueResult();
					}
				}
			}
		}

		public static List<ConnectedHouse> ConnectionsGet(ISession dbSession, int regionId, int streetId, int numberFirst, int numberLast, HouseStreetSide side, bool disabled = false, bool save = false, HouseGenerationState state = HouseGenerationState.AddOrUpdate)
		{
			var street = dbSession.Query<Street>().FirstOrDefault(s => s.Id == streetId);
			var region = dbSession.Query<Region>().FirstOrDefault(s => s.Id == regionId);
			var listOfNewHouses = new List<ConnectedHouse>();
			for (int i = numberFirst; i <= numberLast; i++) {
				if (i == 0)
					continue;
				if (i >= 9999)
					break;
				//удаление
				if (state == HouseGenerationState.Remove) {
					if (side == HouseStreetSide.Both
					    || (side == HouseStreetSide.Left && (i == 1 && i % 2 != 0 || i != 1 && i % 2 != 0))
					    || (side == HouseStreetSide.Right && i != 1 && i % 2 == 0)) {
						var houseExists = dbSession.Query<ConnectedHouse>().FirstOrDefault(s => s.Region.Id == regionId && s.Street.Id == streetId && s.Number == i.ToString());
						if (houseExists != null) {
							dbSession.Delete(houseExists);
						}
					}
				}
				else {
					//обновление / добавление
					var houseExists = dbSession.Query<ConnectedHouse>().FirstOrDefault(s => s.Region.Id == regionId && s.Street.Id == streetId && s.Number == i.ToString());
					if (houseExists == null && (state == HouseGenerationState.AddOrUpdate || state == HouseGenerationState.Add)) {
						if (side == HouseStreetSide.Both
						    || (side == HouseStreetSide.Left && (i == 1 && i % 2 != 0 || i != 1 && i % 2 != 0))
						    || (side == HouseStreetSide.Right && i != 1 && i % 2 == 0)) {
							var item = new ConnectedHouse() {
								Disabled = disabled,
								Street = street,
								Region = region,
								IsCustom = true,
								Number = i.ToString()
							};
							listOfNewHouses.Add(item);
							if (save)
								dbSession.Save(item);
						}
					}
					else {
						houseExists.Disabled = disabled;
						houseExists.IsCustom = true;
						listOfNewHouses.Add(houseExists);
						if (save)
							dbSession.Update(houseExists);
					}
				}
			}
			return listOfNewHouses;
		}
	}
}