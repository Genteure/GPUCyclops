using System;

namespace Media.Formats.MP4
{
	public class UtilsArray
	{
		public static void Set(byte[] bytes, int offset, byte b)
		{
			for (int i=0; i<bytes.Length; i++)
			{
				bytes[i] = b;
			}
		}
	}
}

