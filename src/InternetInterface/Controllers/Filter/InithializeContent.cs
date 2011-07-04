using System.Linq;

using InternetInterface.Models;

namespace InternetInterface.Controllers.Filter
{
	/*public class InithializeContent
	{
	    public static Partner partner
	    {
	        get
	        {
	            return
                    Partner.Queryable.First(p => p.Login == Context.Session["Login"]);
	        }
	        set { }
	    }
	}*/

    public class InithializeContent
    {
        public static Partner partner;
    }
}