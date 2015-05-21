using Castle.ActiveRecord;

namespace InternetInterface.Models
{
	[ActiveRecord("inforoom2_city", Schema = "internet", Lazy = true)]
	public class City
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }
	}
}
