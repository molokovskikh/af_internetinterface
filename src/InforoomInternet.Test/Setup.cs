using System.Reflection;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Web.Ui.ActiveRecordExtentions;
using NUnit.Framework;

namespace InforoomInternet.Test
{
	[SetUpFixture]
	public class Setup
	{
		[SetUp]
		public void SetupFixture()
		{
			if (!ActiveRecordStarter.IsInitialized)
			{
				ActiveRecordInitialize.Init(
					ConnectionHelper.GetConnectionName(),
					Assembly.Load("InforoomInternet"),
					Assembly.Load("InforoomInternet.Test"),
					Assembly.Load("InternetInterface"));
			}
		}
	}
}
