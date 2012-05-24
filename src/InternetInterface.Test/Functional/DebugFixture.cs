using NUnit.Framework;
using log4net;

namespace InternetInterface.Test.Functional
{
	public class DebugFixture : global::Test.Support.Web.WatinFixture2
	{
		[Test]
		public void Debug()
		{
			Open("ServiceRequest/ViewRequests");
			LogManager.GetLogger(typeof(DebugFixture)).Debug(browser.Url);
			LogManager.GetLogger(typeof(DebugFixture)).Debug(browser.Html);
		}
	}
}