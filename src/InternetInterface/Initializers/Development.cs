using Common.Tools;
using Common.Web.Ui.MonoRailExtentions;

namespace InternetInterface.Initializers
{
	public class Development : IEnvironment
	{
		public void Run()
		{
			var config = Global.Config;
			config.PrinterPath = FileHelper.MakeRooted(config.PrinterPath);
			config.DocPath = FileHelper.MakeRooted(config.DocPath);
		}
	}
}