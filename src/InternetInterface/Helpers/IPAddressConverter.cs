using System;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using Castle.Components.Binder;

namespace InternetInterface.Helpers
{
	public class IPAddressTypeDescriptorProvider : TypeDescriptionProvider
	{
		public override ICustomTypeDescriptor GetTypeDescriptor(System.Type objectType, object instance)
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
		public override bool CanConvertFrom(ITypeDescriptorContext context, System.Type sourceType)
		{
			return typeof(string) == sourceType;
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			IPAddress address;
			IPAddress.TryParse(value as string, out address);
			return address;
		}
	}
}