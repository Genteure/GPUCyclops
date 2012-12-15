namespace Media.Formats.MP4
{
    using System;
    using System.Text;

    public class HintMediaHeaderBox : FullBox
    {
        public HintMediaHeaderBox()
            : base(BoxTypes.HintMediaHeader)
        {
          this.Size += 10UL;
        }

        public override void Read(BoxReader reader)
        {
            using (new SizeChecker(this, reader))
            {
                base.Read(reader);
                MaxPDUSize = reader.ReadUInt16();
                AvgPDUSize = reader.ReadUInt16();
                MaxBitrate = reader.ReadUInt16();
                AvgBitrate = reader.ReadUInt16();
                reader.ReadUInt16(); // reserved = 0
            }
        }


        public override void Write(BoxWriter writer)
        {
            using (new SizeCalculator(this, writer))
            {
                base.Write(writer);
                writer.Write(MaxPDUSize);
                writer.Write(AvgPDUSize);
                writer.Write(MaxBitrate);
                writer.Write(AvgBitrate);
                writer.Write((short)0);
            }
        }

        public override string ToString()
        {
            StringBuilder xml = new StringBuilder();
            xml.Append(base.ToString());
            xml.Append("<MaxPDUSize>").Append(MaxPDUSize).Append("</MaxPDUSize>");
            xml.Append("<AvgPDUSize>").Append(AvgPDUSize).Append("</AvgPDUSize>");
            xml.Append("<MaxBitrate>").Append(MaxBitrate).Append("</MaxBitrate>");
            xml.Append("<AvgBitrate>").Append(AvgBitrate).Append("</AvgBitrate>");
            xml.Append("</box>");

            return (xml.ToString());
        }


        public ushort MaxPDUSize { get; private set; }
        public ushort AvgPDUSize { get; private set; }
        public ushort MaxBitrate { get; private set; }
        public ushort AvgBitrate { get; private set; }
    }
}
