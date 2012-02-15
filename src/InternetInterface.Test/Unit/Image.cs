using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Controllers;
using System.Drawing;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	class Image : UserInfoController
	{

		private uint ColorToUInt(Color color)
		{
			return (uint)( (color.R << 16) | (color.G << 8) | (color.B << 0));
		}

		private Color UIntToColor(uint color)
		{
			byte r = (byte)(color >> 16);
			byte g = (byte)(color >> 8);
			byte b = (byte)(color >> 0);
			return Color.FromArgb( r, g, b);
		}
	}
}
