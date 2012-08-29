using System.Collections.Generic;


namespace InternetInterface.AllLogic
{
	public class ColorWork
	{
		public static List<string> GetColorSet()
		{
			var colors = new List<string>();
			for (int i = 0; i < 256; i = i + 51) {
				var ival = i.ToString("X");
				if (ival.Length < 2) {
					ival = "0" + ival;
				}
				for (int j = 0; j < 256; j = j + 51) {
					var jval = j.ToString("X");
					if (jval.Length < 2) {
						jval = "0" + jval;
					}
					for (int k = 0; k < 256; k = k + 51) {
						var kval = k.ToString("X");
						if (kval.Length < 2) {
							kval = "0" + kval;
						}
						colors.Add('#' + ival + jval + kval);
					}
				}
			}
			return colors;
		}
	}
}