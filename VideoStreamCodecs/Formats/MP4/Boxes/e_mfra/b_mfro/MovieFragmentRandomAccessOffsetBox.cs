using System;
using System.Text;

namespace Media.Formats.MP4
{
    public class MovieFragmentRandomAccessOffsetBox : FullBox
    {
        public MovieFragmentRandomAccessOffsetBox() : base(BoxTypes.MovieFragmentRandomAccessOffset)
        {
        }

        public MovieFragmentRandomAccessOffsetBox(ulong size) : this()
        {
          if (size < uint.MaxValue)
            MfraSize = (uint)size;
          else throw new Exception("Box size too big for a movie fragment");
          this.Size += 4UL;
        }

        public override void Read(BoxReader reader)
        {
            using (new SizeChecker(this, reader))
            {
                base.Read(reader);
                this.MfraSize = reader.ReadUInt32();
            }
        }

        public override void Write(BoxWriter writer) {
          using (new SizeCalculator(this, writer)) {
            base.Write(writer);
            writer.WriteUInt32(MfraSize);
          }
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();
          xml.Append(base.ToString());
          xml.Append("<mfrasize>").Append(MfraSize).Append("</mfrasize>");
          xml.Append("</box>");
          return (xml.ToString());
        }


        public uint MfraSize { get; set; }
    }
}
