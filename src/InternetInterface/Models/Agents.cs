using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using InternetInterface.Controllers.Filter;

namespace InternetInterface.Models
{
	[ActiveRecord("Agents", Schema = "internet", Lazy = true)]
	public class Agent : ActiveRecordLinqBase<Agent>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[BelongsTo("Partner")]
		public virtual Partner Partner { get; set; }

		public static Agent GetByInitPartner()
		{
			return Queryable.FirstOrDefault(a => a.Partner == InitializeContent.Partner);
		}
	}
}