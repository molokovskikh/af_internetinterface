namespace InforoomInternet.Helpers
{
	public class AppHelper : Common.Web.Ui.Helpers.AppHelper
	{
		public override bool HavePermission(string controller, string action)
		{
			return false;
		}
	}
}