using System.Text;

namespace Media.Formats.MP4
{
    using System;

    public class TrackFragmentRandomAccessEntry
    {
      public static ulong LastTime = 0;
      public static ulong BaseTime = 0;

        public void Read(BoxReader reader, byte version, uint LengthSizeOfTrafNum, uint LengthSizeOfTrunNum, uint LengthSizeOfSampleNum)
        {
            if (version == 1)
            {
              ulong tmpTime = reader.ReadUInt64();
//              if (tmpTime < LastTime) BaseTime = LastTime;
                this.Time = BaseTime + tmpTime;
                this.MoofOffset = reader.ReadUInt64();
                LastTime = Time;
            }
            else
            {
                this.Time = reader.ReadUInt32();
                this.MoofOffset = reader.ReadUInt32();
            }

            switch (((LengthSizeOfTrafNum + 1) * 8))
            {
                case 0x18:
                    this.TrafNumber = reader.ReadUInt24();
                    break;

                case 0x20:
                    this.TrafNumber = reader.ReadUInt32();
                    break;

                case 8:
                    this.TrafNumber = reader.ReadByte();
                    break;

                case 0x10:
                    this.TrafNumber = reader.ReadUInt16();
                    break;
            }

            switch (((LengthSizeOfTrunNum + 1) * 8))
            {
                case 0x18:
                    this.TrunNumber = reader.ReadUInt24();
                    break;

                case 0x20:
                    this.TrunNumber = reader.ReadUInt32();
                    break;

                case 8:
                    this.TrunNumber = reader.ReadByte();
                    break;

                case 0x10:
                    this.TrunNumber = reader.ReadUInt16();
                    break;
            }

            switch (((LengthSizeOfSampleNum + 1) * 8))
            {
                case 8:
                    this.SampleNumber = reader.ReadByte();
                    return;

                case 0x10:
                    this.SampleNumber = reader.ReadUInt16();
                    break;

                case 0x18:
                    this.SampleNumber = reader.ReadUInt24();
                    return;

                case 0x20:
                    this.SampleNumber = reader.ReadUInt32();
                    return;
            }
        }

        public void Write(BoxWriter writer, byte version, uint LengthSizeOfTrafNum, uint LengthSizeOfTrunNum, uint LengthSizeOfSampleNum)
        {
            if (version == 1) {
              ulong tmpTime = Time - BaseTime;
              writer.WriteUInt64(tmpTime);
              writer.WriteUInt64(MoofOffset);
            } else {
              writer.WriteUInt32((uint)Time);
              writer.WriteUInt32((uint)MoofOffset);
            }

            switch (((LengthSizeOfTrafNum + 1) * 8)) {
              case 0x18:
                  //this.TrafNumber = reader.ReadUInt24();
                  writer.WriteUInt24(TrafNumber);
                  break;

              case 0x20:
                  //this.TrafNumber = reader.ReadUInt32();
                  writer.WriteUInt32(TrafNumber);
                  break;

              case 8:
                  //this.TrafNumber = reader.ReadByte();
                  writer.Write((byte)TrafNumber);
                  break;

              case 0x10: 
                  //this.TrafNumber = reader.ReadUInt16();
                  writer.WriteUInt16((UInt16)TrafNumber);
                  break;
            }

            switch (((LengthSizeOfTrunNum + 1) * 8)) {
              case 0x18:
                  //this.TrunNumber = reader.ReadUInt24();
                  writer.WriteUInt24(TrunNumber);
                  break;

              case 0x20:
                  //this.TrunNumber = reader.ReadUInt32();
                  writer.WriteUInt32(TrunNumber);
                  break;

              case 8:
                  //this.TrunNumber = reader.ReadByte();
                  writer.Write((byte)TrunNumber);
                  break;

              case 0x10:
                  //this.TrunNumber = reader.ReadUInt16();
                  writer.WriteUInt16((UInt16)TrunNumber);
                  break;
            }

            switch (((LengthSizeOfSampleNum + 1) * 8)) {
              case 8:
                  //this.SampleNumber = reader.ReadByte();
                  writer.Write((byte)SampleNumber);
                  return;

              case 0x10:
                  //this.SampleNumber = reader.ReadUInt16();
                  writer.WriteUInt16((UInt16)SampleNumber);
                  break;

              case 0x18:
                  //this.SampleNumber = reader.ReadUInt24();
                  writer.WriteUInt24(SampleNumber);
                  return;

              case 0x20:
                  //this.SampleNumber = reader.ReadUInt32();
                  writer.WriteUInt32(SampleNumber);
                  return;
            }
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();
          xml.Append("<moofoffset>").Append(MoofOffset).Append("</moofoffset>");
          xml.Append("<samplenumber>").Append(SampleNumber).Append("</samplenumber>");
          xml.Append("<time>").Append(Time).Append("</time>");
          xml.Append("<trafnumber>").Append(TrafNumber).Append("</trafnumber>");
          xml.Append("<trunnumber>").Append(TrunNumber).Append("</trunnumber>");
          return (xml.ToString());
        }

      
        public ulong MoofOffset { get; set; }
        public uint SampleNumber { get; set; }
        public ulong Time { get; set; }
        public uint TrafNumber { get; set; }
        public uint TrunNumber { get; set; }
    }
}
