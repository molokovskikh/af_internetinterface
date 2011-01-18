using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Helpers;
using NUnit.Framework;
using InternetInterface.Controllers;

namespace InternetInterface.Test.Unit_
{
	[TestFixture]
	class LoginFixture : ActiveDirectoryHelper
	{
		[Test]
		public void ImageTest()
		{

			IsAuthenticated("test123", "0o9i8u7y6t");
			//IsAuthenticated("Diller1", "1q2w3e4r5t");
			//ChangePassword("test123", "0o9i8u7y6t");
		}
	}
}
