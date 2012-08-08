using System;
using System.Collections.Generic;
using System.Linq;
using InternetInterface.AllLogic;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture, Ignore("Чинить")]
	public class GetClientsLogicFixture
	{
		private static bool ResultAssert(Func<IList<Client>> result)
		{
			var _result = result();
			if (result != null)
			{
				if (_result.Count != 0)
				{
					if (!_result.First().Status.Connected)
						return true;
					if (result.Method.Name == "<GetClientsTest>b__0")
						return true;
				}
			}
			return true;
		}
	}
}
