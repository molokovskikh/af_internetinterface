using System;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using Castle.Components.Binder;

namespace InternetInterface.Helpers
{
	public class IPAddressTypeDescriptorProvider : TypeDescriptionProvider
	{
		public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
		{
			return new IPAddressDescriptor();
		}
	}

	public class IPAddressDescriptor : CustomTypeDescriptor
	{
		public override TypeConverter GetConverter()
		{
			return new IPAddressConverter();
		}
	}

	public class IPAddressConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return typeof(string) == sourceType;
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			IPAddress address;
			var ipString = value as string;
			if (ipString == null)
				return null;
			IPAddress.TryParse(ipString, out address);
			return address;
		}
	}
}