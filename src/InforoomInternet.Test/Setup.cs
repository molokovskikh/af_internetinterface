using System.Reflection;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Web.Ui.ActiveRecordExtentions;
using InternetInterface.Helpers;
using NUnit.Framework;

namespace InforoomInternet.Test
{
	[SetUpFixture]
	public class Setup
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			if (!ActiveRecordStarter.IsInitialized) {
				ActiveRecordInitialize.Init(
					ConnectionHelper.GetConnectionName(),
					Assembly.Load("InforoomInternet"),
					Assembly.Load("InforoomInternet.Test"),
					Assembly.Load("InternetInterface"));
			}
		}
	}
}