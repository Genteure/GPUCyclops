using System.Text;

namespace Media.Formats.MP4
{
    public class MovieFragmentHeaderBox : FullBox
    {
        public MovieFragmentHeaderBox() : base(BoxTypes.MovieFragmentHeader)
        {
        }

        public MovieFragmentHeaderBox(uint sequenceNum)
          : this()
        {
          this.SequenceNumber = sequenceNum;
          this.Size += 4UL;
        }

        public override void Read(BoxReader reader)
        {
            using (new SizeChecker(this, reader))
            {
                base.Read(reader);
                this.SequenceNumber = reader.ReadUInt32();
            }
        }

        public override void Write(BoxWriter writer) {
          using (new SizeCalculator(this, writer)) {
            base.Write(writer);
            writer.WriteUInt32(SequenceNumber);
          }
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();
          xml.Append(base.ToString());
          xml.Append("<sequencenumber>").Append(SequenceNumber).Append("</sequencenumber>");
          xml.Append("</box>");
          return (xml.ToString());
        }


        public uint SequenceNumber; // set in Fragment.cs when setting up a fragment
    }
}
