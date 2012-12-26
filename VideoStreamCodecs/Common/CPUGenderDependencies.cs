using System;

namespace Media
{
  /// <summary>
  /// CPUGenderDependencies
  /// This encapsulates all CPU gender-dependent stuff like big-endianism.
  /// </summary>
  public static class CPUGenderDependencies
  {
    public static int INT(int rawInt)
    {
      if (BitConverter.IsLittleEndian)
      {
        byte[] bytes = BitConverter.GetBytes(rawInt);
        byte tmp;
        tmp = bytes[0];
        bytes[0] = bytes[3];
        bytes[3] = tmp;
        tmp = bytes[1];
        bytes[1] = bytes[2];
        bytes[2] = tmp;
        return BitConverter.ToInt32(bytes, 0);
      }
      return rawInt;
    }

    public static uint UINT(uint rawUint)
    {
      if (BitConverter.IsLittleEndian)
      {
        byte[] bytes = BitConverter.GetBytes(rawUint);
        byte tmp;
        tmp = bytes[0];
        bytes[0] = bytes[3];
        bytes[3] = tmp;
        tmp = bytes[1];
        bytes[1] = bytes[2];
        bytes[2] = tmp;
        return BitConverter.ToUInt32(bytes, 0);
      }
      return rawUint;
    }
  }
}
