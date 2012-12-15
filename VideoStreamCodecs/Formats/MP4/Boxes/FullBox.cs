using System.Text;

namespace Media.Formats.MP4
{
    using System;

    public class FullBox : Box
    {
        private uint flags;
        private Type m_enumFlags; // looks like this is not used?
        private byte version;

        public FullBox(BoxType type) : base(type)
        {
          this.Size += 4UL;
        }

        public FullBox(BoxType type, Type enumFlags) : base(type)
        {
            this.m_enumFlags = enumFlags;
        }

        public override void Read(BoxReader reader)
        {
            base.Read(reader);
            this.version = reader.ReadByte();
            this.flags = (uint) (((reader.ReadByte() << 0x10) | (reader.ReadByte() << 8)) | reader.ReadByte());
        }

        public override void Write(BoxWriter writer) {
          base.Write(writer);
          writer.Write(version);
          byte a = (byte)((flags >> 0x16) & 0xFF);
          byte b = (byte)((flags >> 0x08) & 0xFF);
          byte c = (byte)((flags) & 0xFF);
          writer.Write(a);
          writer.Write(b);
          writer.Write(c);
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();
          xml.Append(base.ToString());
          xml.Append("<version>").Append(version).Append("</version>");
          xml.Append("<flags>").Append(flags).Append("</flags>");
          return (xml.ToString());
        }	


        public uint Flags {
          get { return this.flags; }
          set { flags = value; }
        }

        public byte Version { 
          get { return this.version; }
          set { version = value; }
        }
    }
}
