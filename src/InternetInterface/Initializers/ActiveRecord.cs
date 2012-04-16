using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.MonoRailExtentions;

namespace InternetInterface.Initializers
{
	public class ActiveRecord : ActiveRecordInitializer
	{
		public ActiveRecord()
		{
			Assemblies = new [] { "InternetInterface" };
			AdditionalTypes = new[] {typeof (ValidEventListner)};
		}
	}
}