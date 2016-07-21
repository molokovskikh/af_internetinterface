using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using InternetInterface.Models.Universal;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("Houses", Schema = "internet", Lazy = true)]
	public class House : ValidActiveRecordLinqBase<House>
	{
		public House()
		{
		}

		public House(string street, int number, RegionHouse region)
		{
			Street = street;
			Number = number;
			Region = region;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Street { get; set; }

		[Property, ValidateNonEmpty("Введите номер дома"), ValidateInteger("Это поле должно быть число")]
		public virtual int Number { get; set; }

		[Property("CaseHouse")]
		public virtual string Case { get; set; }

		[Property, ValidateNonEmpty("Введите количество квартир"), ValidateInteger("Это поле должно быть число")]
		public virtual int ApartmentCount { get; set; }

		[Property]
		public virtual DateTime? LastPassDate { get; set; }

		[Property]
		public virtual int PassCount { get; set; }

		[HasMany(ColumnKey = "House", OrderBy = "Number", Lazy = true)]
		public virtual IList<Apartment> Apartments { get; set; }

		[HasMany(ColumnKey = "House", OrderBy = "Number", Lazy = true)]
		public virtual IList<Entrance> Entrances { get; set; }

		[HasMany(ColumnKey = "House", OrderBy = "BypassDate", Lazy = true)]
		public virtual IList<BypassHouse> Bypass { get; set; }

		[BelongsTo("RegionId"), ValidateNonEmpty]
		public virtual RegionHouse Region { get; set; }


		public virtual int CompetitorCount
		{
			get { return Apartments.Where(a => !string.IsNullOrEmpty(a.LastInternet) && (a.Status == null || a.Status.ShortName != "request")).Count(); }
		}

		public static List<House> AllSort
		{
			get { return FindAll().OrderBy(h => h.Street).ToList(); }
		}

		public virtual Apartment GetApartmentWithNumber(string num)
		{
			var apartment = Apartments.Where(a => a.Number == num).ToList();
			if (apartment.Count != 0)
				return apartment.First();
			return null;
		}

		public virtual uint GetClientWithApNumber(string num)
		{
			return
				Client.Queryable.Where(c => c.PhysicalClient.HouseObj == this && c.PhysicalClient.Apartment == num)
					.ToList().Select(c => c.Id).FirstOrDefault();
		}

		public virtual int GetSubscriberCount()
		{
			return PhysicalClient.Queryable.Where(p => p.HouseObj == this).Count();
		}

		public virtual BypassHouse GetLastBypass()
		{
			return Bypass.Last();
		}

		public virtual double GetCompetitorsPenetrationPercent()
		{
			if (ApartmentCount == 0)
				return 1;
			return (double)CompetitorCount / ApartmentCount * 100;
		}

		public virtual double GetPenetrationPercent()
		{
			if (ApartmentCount == 0)
				return 1;
			return (double)(PhysicalClient.Queryable.Where(p => p.HouseObj == this).Count()) / ApartmentCount * 100;
		}

		public override string ToString()
		{
			return string.Format("{0} {1} {2}", Street, Number, !string.IsNullOrEmpty(Case) ? "корп " + Case : string.Empty);
		}

		public static IList<House> All(ISession session)
		{
			return session.Query<House>().OrderBy(h => h.Street).ToList();
		}

		public static void SynchronizeHouseConnections(ISession dbSession)
		{
			var regionList = dbSession.Query<RegionHouse>().ToList();
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
AND (( targetR.Id = {0}) OR (targetR.Id IS NULL AND targetR2.Id = {0}  ))
ORDER BY targetS.Id
", region.Id)).List<object[]>();
				foreach (var item in currentAddressesRaw) {
					currentAddresses.Add(new Tuple<int, int?, int, string>(
						Convert.ToInt32(item[0]),
						item[1] == null ? null : new Nullable<Int32>(Convert.ToInt32(item[1])),
						Convert.ToInt32(item[2] ?? 0),
						item[3].ToString()
						));
				}
				currentAddressesRaw.Clear();

				var currentConnectedHouses = new List<Tuple<int, int, int, string, bool, bool>>();
				var currentConnectedHousesRaw = dbSession.CreateSQLQuery(string.Format(@"
SELECT c.Id, c.Region, c.Street, c.Number, c.IsCustom, c.Disabled  FROM internet.inforoom2_connectedhouses as c WHERE c.Region = {0} ", region.Id)).List<object[]>();
				foreach (var item in currentConnectedHousesRaw) {
					currentConnectedHouses.Add(new Tuple<int, int, int, string, bool, bool>(
						Convert.ToInt32(item[0] ?? 0),
						Convert.ToInt32(item[1] ?? 0),
						Convert.ToInt32(item[2] ?? 0),
						item[3].ToString(),
						Convert.ToInt32(item[4] ?? 0) == 1,
						Convert.ToInt32(item[5] ?? 0) == 1
						));
				}
				currentConnectedHousesRaw.Clear();

				foreach (var item in currentAddresses) {
					int regionId = item.Item2 ?? item.Item1;
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
	}
}