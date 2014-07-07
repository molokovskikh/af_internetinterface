using Castle.ActiveRecord;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "Internet")]
	public class Street
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property(NotNull = true)]
		public virtual string Name { get; set; }
	}
}