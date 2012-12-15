namespace Media.Formats.MP4
{
    
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;

    /// <summary>
  /// BoxReader
  /// NOTE: This only deals with unsigned integers. If signed integers are used in the Read methods,
  /// the Read methods in this derived class will NOT be used. Instead, the signed methods in the
  /// base class BinaryReader is used. This means that for signed integers, the byte order is NOT reversed.
  /// CCT: Have added ReadInt16, ReadInt32, and ReadInt64 for reading SIGNED integers.
  /// </summary>
    public class BoxReader : BinaryReader
    {
        public BoxReader(Stream input) : base(input, Encoding.UTF8)
        {
        }

        public void IgnoreZero16()
        {
            if (this.ReadUInt16() != 0)
            {
                throw new InvalidBoxException(this.BaseStream.Position, "Found unexpected non-zero bytes");
            }
        }

        public void IgnoreZero16(int count)
        {
            for (int i = 0; i < count; i++)
            {
                this.IgnoreZero16();
            }
        }

        public void IgnoreZero32()
        {
            if (this.ReadUInt32() != 0)
            {
                throw new InvalidBoxException(this.BaseStream.Position, "Found unexpected non-zero bytes");
            }
        }

        public void IgnoreZeroBytes(int bytesToIgnore)
        {
            for (int i = 0; i < bytesToIgnore; i++)
            {
                if (this.ReadByte() != 0)
                {
                    throw new InvalidBoxException(this.BaseStream.Position, "Found unexpected non-zero byte");
                }
            }
        }

        public BoxType PeekNextBoxType()
        {
            long position = this.BaseStream.Position;
            Box box = new Box(BoxTypes.Any);
            box.Read(this);
            this.BaseStream.Seek(position, SeekOrigin.Begin);
            return box.Type;
        }

        public BoxType ReadBoxType()
        {
            return new BoxType(this.ReadBytes(4));
        }

        public ExtendedBoxType ReadExtendedBoxType()
        {
          return new ExtendedBoxType(this.ReadBytes(16));
        }

        public string ReadNullTerminatedString()
        {
            char ch;
            StringBuilder builder = new StringBuilder();
            while ((ch = this.ReadChar()) != '\0')
            {
                builder.Append(ch);
            }
            return builder.ToString();
        }

        public void ReadProperties(object variable)
        {
            foreach (PropertyInfo info in variable.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                if (info.PropertyType == typeof(ulong))
                {
                    info.SetValue(variable, this.ReadUInt64(), null);
                }
                else if (info.PropertyType == typeof(uint))
                {
                    info.SetValue(variable, this.ReadUInt32(), null);
                }
                else if (info.PropertyType == typeof(ushort))
                {
                    info.SetValue(variable, this.ReadUInt16(), null);
                }
                else if (info.PropertyType == typeof(byte))
                {
                    info.SetValue(variable, this.ReadByte(), null);
                }
                else
                {
                    if (info.PropertyType != typeof(BoxType))
                    {
                        throw new InvalidOperationException("Type not yet supported for auto reading");
                    }
                    BoxType type2 = new BoxType(this.ReadBytes(4));
                    info.SetValue(variable, type2, null);
                }
            }
        }

        public override ushort ReadUInt16()
        {
            byte num = this.ReadByte();
            byte num2 = this.ReadByte();
            return (ushort) ((num << 8) + num2);
        }

        public override short ReadInt16()
        {
          byte num = this.ReadByte();
          byte num2 = this.ReadByte();
          return (short)((num << 8) + num2); // FIXME: not sure about this (what happens to the sign bit?)
        }

        public uint ReadUInt24()
        {
            byte num = this.ReadByte();
            byte num2 = this.ReadByte();
            byte num3 = this.ReadByte();
            return (uint) ((((num << 8) + num2) << 8) + num3);
        }

        public override uint ReadUInt32()
        {
            byte num = this.ReadByte();
            byte num2 = this.ReadByte();
            byte num3 = this.ReadByte();
            byte num4 = this.ReadByte();
            return (uint) ((((((num << 8) + num2) << 8) + num3) << 8) + num4);
        }

        public override int ReadInt32()
        {
          byte num = this.ReadByte();
          byte num2 = this.ReadByte();
          byte num3 = this.ReadByte();
          byte num4 = this.ReadByte();
          return (int)((((((num << 8) + num2) << 8) + num3) << 8) + num4); // FIXME: not sure about this (what happens to the sign bit?)
        }

        public override ulong ReadUInt64()
        {
            uint num = this.ReadUInt32();
            uint num2 = this.ReadUInt32();
            return ((num << 0x20) + num2);
        }

        public override long ReadInt64()
        {
          int num = this.ReadInt32();
          int num2 = this.ReadInt32();
          return ((num << 0x20) + num2); // FIXME: not sure about this (what happens to the sign bit?)
        }
    }
}
