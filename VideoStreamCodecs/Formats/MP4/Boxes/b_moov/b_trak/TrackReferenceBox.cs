using System.Text;

namespace Media.Formats.MP4 {
  public class TrackReferenceBox : Box {
        private uint[] trackID;

        public TrackReferenceBox() : base(BoxTypes.TrackReference) {}

        public TrackReferenceBox(uint trackID)
          : this()
        {
          this.trackID = new uint[1];
          this.trackID[0] = trackID;
          this.Size += 4UL; // for one track ID
        }

        public override void  Read(BoxReader reader) {
          base.Read(reader);
          trackID = new uint[(int)((long)Size - (reader.BaseStream.Position - (long)Offset)) / 4];
          for (int i = 0; i < trackID.Length; i++) {
            trackID[i] = reader.ReadUInt32();
          }
        }

        public override void Write(BoxWriter writer)
        {
            using (new SizeCalculator(this, writer))
            {
                base.Write(writer);
                for (int i = 0; i < trackID.Length; i++)
                {
                    writer.Write(trackID[i]);
                }
            }
        }

        public override string ToString() {
            StringBuilder xml = new StringBuilder();
            xml.Append(base.ToString());
            xml.Append("<trackID>");
            for (int i = 0; i < trackID.Length; i++)
            {
                xml.Append(trackID[i]);
                if (i < trackID.Length - 1)
                {
                    xml.Append(",");
                }
            }
            xml.Append("</trackID>");
            xml.Append("</box>");

            return (xml.ToString());
        }

        public uint[] TrackID
        {
            get
            {
                return (trackID);
            }
        }
  }

}
