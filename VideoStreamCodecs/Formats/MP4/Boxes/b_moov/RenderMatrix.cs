using System;
using System.Text;

namespace Media.Formats.MP4
{
  /// <summary>
  /// RenderMatrix
  /// Implementation of matrix found in mvhd and tkhd boxes.
  ///   a, b, u
  ///   c, d, v
  ///   x, y, w
  /// where the order in the array is [a, b, u, c, d, v, x, y, w] and all numbers
  /// are fixed decimal.
  /// The following are 16.16 fixed decimal: a, b, u, c, d, v, x, and y.
  /// Only the following is 2.30 fixed decimal: w.
  /// </summary>
  public class RenderMatrix
  {
    uint[] matrix;

    public RenderMatrix()
    {
      matrix = new uint[9]; // stored as 9 32-bit unsigned ints
      matrix[0] = 0x10000; // 1.0
      matrix[4] = 0x10000; // 1.0
      matrix[8] = 0x40000000; // 1.0 (see description of RenderMatrix class)
    }

    public void Read(BoxReader reader)
    {
      for (int i = 0; i < 9; i++) matrix[i] = reader.ReadUInt32(); // int(32)[9] - matrix
    }

    public void Write(BoxWriter writer)
    {
      for (int i = 0; i < 9; i++) writer.WriteUInt32(matrix[i]);
    }

    public void CopyFromArray(uint[] uintArray)
    {
      uintArray.CopyTo(matrix, 0);
    }

    public float this[byte i, byte j]
    {
      get
      {
        int index = 3 * (int)i + (int)j;
        if (index >= 9)
          throw new Exception("RenderMatrix: Invalid array indexer");
        if (index < 8)
          return (float)((matrix[index] >> 16) + (matrix[index] & 0xFFFF) / 0xFFFF);
        else
          return (float)((matrix[index] >> 30) + (matrix[index] & 0x3FFFFFFF) / 0x3FFFFFFF);
      }
    }

    public override string ToString()
    {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<Matrix>");
      for (byte i = 0; i < 3; i++)
        for (byte j = 0; j < 3; j++)
        {
          xml.Append(this[i, j]).Append(" ");
        }
      xml.Append("</Matrix>");
      return (xml.ToString());
    }
  }
}
