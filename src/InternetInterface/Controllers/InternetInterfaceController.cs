using Common.Web.Ui.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using Newtonsoft.Json;

namespace InternetInterface.Controllers
{
	public class InternetInterfaceController : BaseController
	{
		public InternetInterfaceController()
		{
			BeforeAction += (action, context1, controller, controllerContext) => {
				controllerContext.PropertyBag["config"] = Global.Config;
			};
		}

		protected void RedirectTo(Client client)
		{
			string uri;
			if (client.GetClientType() == ClientType.Phisical) {
				uri = "~/UserInfo/SearchUserInfo.rails?filter.ClientCode={0}";
			}
			else {
				uri = "~/UserInfo/LawyerPersonInfo.rails?filter.ClientCode={0}";
			}
			RedirectToUrl(string.Format(uri, client.Id));
		}

		public SmsHelper SmsHelper = new SmsHelper();

		protected Partner Partner
		{
			get { return InitializeContent.Partner; }
		}

		protected void RenderJson(object data)
		{
			Response.ContentType = "application/json";
			RenderText(JsonConvert.SerializeObject(data));
		}
	}
}