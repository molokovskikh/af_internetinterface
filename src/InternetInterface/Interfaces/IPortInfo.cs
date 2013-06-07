using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.Core.Logging;
using NHibernate;

namespace InternetInterface.Interfaces
{
	public interface IPortInfo
	{
		void GetPortInfo(ISession session, IDictionary propertyBag, ILogger logger, uint endPointId);
		string ViewName { get; }
	}
}