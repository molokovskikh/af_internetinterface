using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Event;

namespace Inforoom2.Intefaces
{
	interface ILogAppeal
	{
		Client GetAppealClient(ISession session);
		List<string> GetAppealFields();
		string GetAdditionalAppealInfo(string property, object oldPropertyValue,  ISession session);
	}
}
