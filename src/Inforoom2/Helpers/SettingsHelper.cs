using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using NHibernate;
using NHibernate.Linq;
using Internet = Inforoom2.Models.Services.Internet;
using IpTv = Inforoom2.Models.IpTv;

namespace Inforoom2.Helpers
{
	public class SettingsHelper
	{
		public Status DefaultStatus;
		public Recipient DefaultRecipient;
		public Service[] DefaultServices = new Service[0];
		public Service[] Services = new Service[0];

		public SettingsHelper()
		{
			DefaultServices = new Service[0];
		}

		public SettingsHelper(ISession session)
		{
			DefaultStatus = session.Load<Status>((int) StatusType.BlockedAndNoConnected);
			DefaultRecipient = session.Query<Recipient>().FirstOrDefault(r => r.INN == "3666152146");
			DefaultServices = new Service[]
			{
				session.Query<Internet>().First(),
				session.Query<IpTv>().First(),
			};
			Services = session.Query<Service>().ToArray();
		}

		public static SettingsHelper UnitTestSettings()
		{
			var settings = new SettingsHelper
			{
				DefaultServices = new Service[]
				{
					new Internet(),
					new IpTv(),
				},
				Services = new[]
				{
					new FixedIp
					{
						Price = 30,
					}
				},
				DefaultStatus = new Status
				{
					ShortName = "Worked"
				}
			};
			return settings;
		}
	}
}