using System;
using System.Drawing;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("Labels", Schema = "Internet", Lazy = true)]
	public class Label : ActiveRecordLinqBase<Label>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateIsUnique("Имя должно быть уникальным")]
		public virtual string Name { get; set; }

		[Property]
		public virtual string Color { get; set; }

		[Property]
		public virtual bool Deleted { get; set; }

		[Property]
		public virtual string ShortComment { get; set; }

		public virtual string GetBorderColor()
		{
			var oColor = ColorTranslator.FromHtml(Color);
			
			var fNewHue = oColor.GetHue() + 10;
			var fNewSaturation = oColor.GetSaturation() + 2F;
			var fNewBrightness = oColor.GetBrightness() + 2F;

			var newC = HLStoRGB(fNewHue, fNewBrightness, fNewSaturation);
			var oNewColor = System.Drawing.Color.FromArgb(Convert.ToInt32(newC.R), Convert.ToInt32(newC.G), Convert.ToInt32(newC.B));
			return RGBInterpritator(oNewColor);
		}

		public virtual string RGBInterpritator(Color Color)
		{
			var R = Color.R.ToString("X");
			var G = Color.G.ToString("X");
			var B = Color.B.ToString("X");
			if (R.Length < 2)
				R = "0" + R;
			if (G.Length < 2)
				G = "0" + G;
			if (B.Length < 2)
				B = "0" + B;
			return "#" + R + G + B;
		}

		private long ColorToUInt(MyColor color)
		{
			if (color.R > 255) color.R = 255;
			if (color.G > 255) color.G = 255;
			if (color.B > 255) color.B = 255;
			return ((Convert.ToByte(color.R) << 16) | (Convert.ToByte(color.G) << 8) | (Convert.ToByte(color.B) << 0));
		}

		public virtual MyColor UIntToColor(uint color)
		{
			byte r = (byte)(color >> 16);
			byte g = (byte)(color >> 8);
			byte b = (byte)(color >> 0);
			return new MyColor
					{
						R = r,
						B = b,
						G = g
					};
		}

		public class MyColor
		{
			public float R;
			public float G;
			public float B;
		}

		public virtual MyColor HLStoRGB(float H, float L, float S)
		{
			float OffsetLightness;
			float OffsetSaturation;
			var color = new MyColor();


			if (H < 0) H = (240 - H)%240;
			else H = H%240;

			if (H < 80) color.R = Math.Min(255, 255*(80 - H)/40);
			else if (H > 160) color.R = Math.Min(255, 255*(H - 160)/40);

			if (H < 160) color.G = Math.Min(255, 255*(80 - Math.Abs(H - 80))/40);
			if (H > 80) color.B = Math.Min(255, 255*(80 - Math.Abs(H - 160))/40);

			if (S < 240)
			{
				color = UIntToColor(Convert.ToUInt32(ColorToUInt(color) * ((float)(S / 240))));
				OffsetSaturation = 128*(240 - S)/240;
				color.R += OffsetSaturation;
				color.G += OffsetSaturation;
				color.B += OffsetSaturation;
			}

			L = Math.Min(240, L);
			color = UIntToColor(Convert.ToUInt32(ColorToUInt(color)*((120 - Math.Abs(L - 120))/120)));

			if (L > 120)
			{
				OffsetLightness = 256*(L - 120)/120;
				color.R += OffsetLightness;
				color.G += OffsetLightness;
				color.B += OffsetLightness;
			}

			return color;
		}


	}
}
