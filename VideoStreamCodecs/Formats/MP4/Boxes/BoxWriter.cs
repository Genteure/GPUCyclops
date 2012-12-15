using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Media.Formats.MP4
{
  /// <summary>
  /// BoxWriter
  /// This class writes out integers of various sizes in byte-reversed order.
  /// </summary>
  public class BoxWriter : BinaryWriter {
    public BoxWriter(Stream output) : base(output, Encoding.UTF8) {
    }


    public void WriteBoxType(BoxType inType) {
      this.Write(inType.GetBytes(), 0, 4);
    }

    public void WriteString(string inString)
    {
      foreach (char ch in inString) {
        this.Write(ch);
      }
      this.Write('\0');
    }

    public void WriteProperties(object variable)
    {
        foreach (PropertyInfo info in variable.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
        {
            if (info.PropertyType == typeof(ulong)) {
              this.WriteUInt64((UInt64)info.GetValue(variable, null));
//                info.SetValue(variable, this.ReadUInt64(), null);
            }
            else if (info.PropertyType == typeof(uint))
            {
              this.WriteUInt32((UInt32)info.GetValue(variable, null));
//                info.SetValue(variable, this.ReadUInt32(), null);
            }
            else if (info.PropertyType == typeof(ushort))
            {
              this.WriteUInt16((UInt16)info.GetValue(variable, null));
//                info.SetValue(variable, this.ReadUInt16(), null);
            }
            else if (info.PropertyType == typeof(byte))
            {
              this.Write((byte)info.GetValue(variable, null));
//                info.SetValue(variable, this.ReadByte(), null);
            }
            else
            {
              BoxType btype = (BoxType)info.GetValue(variable, null);
              this.Write(btype.GetBytes());
            }
        }
    }

    public override void Write(ushort u) {
      WriteUInt16(u);
    }

    public override void Write(short s)
    {
      WriteInt16(s);
    }

    public override void Write(uint u)
    {
      WriteUInt32(u);
    }

    public override void Write(int i)
    {
      WriteInt32(i);
    }

    public override void Write(ulong u)
    {
      WriteUInt64(u);
    }

    public override void Write(long l)
    {
      WriteInt64(l);
    }

    private void WriteBytesInReverseOrder(byte[] bytes)
    {
      for (int index = bytes.Length - 1; index >= 0; index--)
      {
        this.Write(bytes[index]);
      }
    }

    public void WriteUInt16(ushort u)
    {
      byte[] b = BitConverter.GetBytes(u);
      WriteBytesInReverseOrder(b);
    }

    public void WriteInt16(short s)
    {
      byte[] b = BitConverter.GetBytes(s);
      WriteBytesInReverseOrder(b);
    }

    private void Write3BytesInReverseOrder(byte[] b)
    {
      for (int index = 2; index >= 0; index--)
      {
        this.Write(b[index]);
      }
    }

    public void WriteUInt24(uint u)
    {
      byte[] b = BitConverter.GetBytes(u);
      Write3BytesInReverseOrder(b);
    }

    public void WriteInt24(int i)
    {
      byte[] b = BitConverter.GetBytes(i);
      Write3BytesInReverseOrder(b);
    }

    public void WriteUInt32(uint u)
    {
      byte[] b = BitConverter.GetBytes(u);
      WriteBytesInReverseOrder(b);
    }

    public void WriteInt32(int i)
    {
      byte[] b = BitConverter.GetBytes(i);
      WriteBytesInReverseOrder(b);
    }

    public void WriteUInt64(ulong u)
    {
      byte[] b = BitConverter.GetBytes(u);
      WriteBytesInReverseOrder(b);
    }

    public void WriteInt64(long l)
    {
      byte[] b = BitConverter.GetBytes(l);
      WriteBytesInReverseOrder(b);
    }


    public void WriteNullTerminatedString(string str)
    {
        foreach (char ch in str)
        {
            this.Write(ch);
        }
        this.Write((char)0);
    }
  }
}
