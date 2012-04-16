using System.Web;
using Castle.MonoRail.Framework.Routing;
using Castle.MonoRail.Framework.Test;
using NUnit.Framework;

namespace InternetInterface.Test
{
	[TestFixture]
	public class Troubleshoot
	{
		[Test]
		public void test()
		{
			RoutingEngine engine = new RoutingEngine();
			engine.Add(new PatternRoute("/")
				.DefaultForController().Is("Login")
				.DefaultForAction().Is("LoginPartner"));

			engine.Add(new PatternRoute("/<controller>/<action>.[format]"));
			engine.Add(new PatternRoute("/<controller>/<id>/<action>.[format]"));
			var match = engine.FindMatch("/Login/LoginPartner.rails", new RouteContext(new StubRequest(), null, "", null));
		}
	}
}