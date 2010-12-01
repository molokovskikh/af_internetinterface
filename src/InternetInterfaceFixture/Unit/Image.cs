using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Controllers;
using System.Drawing;
using InternetInterfaceFixture.Helpers;
using NUnit.Framework;

namespace InternetInterfaceFixture.Unit
{
	[TestFixture]
	class Image : UserInfoController
	{

		[Test]
		public void ImageTest()
		{
			CreateImage(Color.DeepSkyBlue, 30, 30, "5");
		}
	}
}
