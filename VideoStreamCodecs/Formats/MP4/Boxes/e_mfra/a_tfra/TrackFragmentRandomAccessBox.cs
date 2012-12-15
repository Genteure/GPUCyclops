using System.Text;

namespace Media.Formats.MP4
{
    using System.Collections.Generic;

    public class TrackFragmentRandomAccessBox : FullBox
    {
        public TrackFragmentRandomAccessBox() : base(BoxTypes.TrackFragmentRandomAccess)
        {
        }

        public override void Read(BoxReader reader)
        {
            using (new SizeChecker(this, reader))
            {
                base.Read(reader);
                this.TrackId = reader.ReadUInt32();
                uint num = reader.ReadUInt32();
                if ((num & 0xfffc) != 0)
                {
                    throw new InvalidBoxException(base.Type, reader.BaseStream.Position, "top 26 bits of length field reserved");
                }
                this.LengthSizeOfTrafNum = (uint) ((num & 12) >> 4);
                this.LengthSizeOfTrunNum = (uint) ((num & 6) >> 2);
                this.LengthSizeOfSampleNum = num & 3;
                this.NumberOfEntry = reader.ReadUInt32();
                this.TrackFragmentRandomAccessEntries = new List<TrackFragmentRandomAccessEntry>((int) this.NumberOfEntry);
                for (int i = 0; i < this.NumberOfEntry; i++)
                {
                    TrackFragmentRandomAccessEntry item = new TrackFragmentRandomAccessEntry();
                    item.Read(reader, base.Version, this.LengthSizeOfTrafNum, this.LengthSizeOfTrunNum, this.LengthSizeOfSampleNum);
                    this.TrackFragmentRandomAccessEntries.Add(item);
                }
            }
        }

        public override void Write(BoxWriter writer) {
          using (new SizeCalculator(this, writer)) {
            base.Write(writer);
            writer.WriteUInt32(TrackId);
            uint num = 0;
            num = (uint)((LengthSizeOfTrafNum << 4) | (LengthSizeOfTrunNum << 2) | LengthSizeOfSampleNum);
            writer.WriteUInt32(num);
            writer.WriteUInt32(NumberOfEntry);
            for (int i=0; i<this.NumberOfEntry; i++) {
              TrackFragmentRandomAccessEntry item = TrackFragmentRandomAccessEntries[i];
              item.Write(writer, base.Version, this.LengthSizeOfTrafNum, this.LengthSizeOfTrunNum, this.LengthSizeOfSampleNum);
            }
          }
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();
          xml.Append(base.ToString());
          xml.Append("<trackid>").Append(TrackId).Append("</trackid>");
          xml.Append("<numberofentry>").Append(NumberOfEntry).Append("</numberofentry>");
          xml.Append("<lengthsizeofsamplenum>").Append(LengthSizeOfSampleNum).Append("</lengthsizeofsamplenum>");
          xml.Append("<lengthsizeoftrafnum>").Append(LengthSizeOfTrafNum).Append("</lengthsizeoftrafnum>");
          xml.Append("<lengthsizeoftrunnum>").Append(LengthSizeOfTrunNum).Append("</lengthsizeoftrunnum>");

          xml.Append("<trackfragmententries>");
          for (int i=0; i<TrackFragmentRandomAccessEntries.Count; i++)
            TrackFragmentRandomAccessEntries[i].ToString();
          xml.Append("</trackfragmententries>");

          xml.Append("</box>");
          return (xml.ToString());
        }


        public uint LengthSizeOfSampleNum { get; set; }
        public uint LengthSizeOfTrafNum { get; set; }
        public uint LengthSizeOfTrunNum { get; set; }
        public uint NumberOfEntry { get; set; }
        public List<TrackFragmentRandomAccessEntry> TrackFragmentRandomAccessEntries { get; set; }
        public uint TrackId { get; set; }
    }
}
