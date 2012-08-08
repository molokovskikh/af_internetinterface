namespace InternetInterface.Helpers
{
	public class BigEndianConverter
	{
		public static byte[] GetBytes(int value)
		{
			return new[] {
				(byte)(value >> 24),
				(byte)(value >> 16),
				(byte)(value >> 8),
				(byte)value,
			};
		}

		public static byte[] GetBytes(uint value)
		{
			return GetBytes((int)value);
		}

		public static uint ToInt32(byte[] bytes)
		{
			return (uint)(bytes[0] << 24) + (uint)(bytes[1] << 16) + (uint)(bytes[2] << 8) + (uint)bytes[3];
		}

		public static ushort ToUInt16(byte[] bytes, int i)
		{
			return (ushort)((ushort)(bytes[i] << 8) + (ushort)bytes[i + 1]);
		}
	}
}