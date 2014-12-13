using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	/// <summary>
	/// Ассоциация между IP-пулом и регионом
	/// </summary>
	[ActiveRecord("IpPoolRegions", Schema = "internet", Lazy = true)]
	public class IpPoolRegion : ChildActiveRecordLinqBase<IpPoolRegion>
	{
		public IpPoolRegion()
		{
			IpPool = new IpPool();
		}

		public IpPoolRegion(IpPool pool, RegionHouse region)
		{
			IpPool = pool;
			Region = region.Id;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo("IpPool"), ValidateNonEmpty]
		public virtual IpPool IpPool { get; set; }

		[Property, ValidateNonEmpty]
		public virtual uint Region { get; set; }

		/// <summary>
		///     Возвращает все "белые" IP-пулы из таблицы "IpPools" БД (для указанного региона)
		/// </summary>
		/// <param name="dbSession">Ссылка на сессию работы с БД</param>
		/// <param name="region">Ссылка на конкретную запись из таблицы "Regions" БД</param>
		/// <returns>Список IP-пулов</returns>
		public static List<IpPool> GetPoolsForRegion(ISession dbSession, RegionHouse region = null)
		{
			var poolsList = dbSession.Query<IpPool>().Where(p => !(p.IsGray)).ToList();
			if (region != null && poolsList.Count != 0) {
				var poolRegsList = dbSession.Query<IpPoolRegion>()
					.ToList().FindAll(rp => (rp.Region == region.Id));
				poolsList = poolsList.FindAll(p => poolRegsList.Exists(rp => rp.IpPool.Id == p.Id));
			}
			return poolsList;
		}
	}
}