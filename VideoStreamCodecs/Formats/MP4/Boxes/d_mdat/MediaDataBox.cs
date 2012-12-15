namespace Media.Formats.MP4
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// mdat box
    /// </summary>
    public class MediaDataBox : Box
    {

        public MediaDataBox() : base(BoxTypes.MediaData)
        {
        }

        public override void Read(BoxReader reader)
        {
          using (new SizeChecker(this, reader)) {
            base.Read(reader);
            long headerSize = reader.BaseStream.Position - (long)this.Offset;
            //if (LargeSize > 0) headerSize += 8;

            PayloadOffset = (ulong)reader.BaseStream.Position;
            // Why are we not looking at LargeSize for other types of boxes? It's really only mdat that can grow in size larger than max long int.
            // Note that we don't ever load the data into MediaData. However, we do write it out from MediaData. See Write below.
            reader.BaseStream.Seek((long)Size - headerSize, SeekOrigin.Current);
          }
        }

        public override void Write(BoxWriter writer)
        {
          base.Write(writer); // write out just the header
        }

        /// <summary>
        /// Write - this write routine for mdat is used only when writing out an MP4 file.
        /// (See MP4StreamWriter.)
        /// </summary>
        /// <param name="writer"></param>
        public void Write(BoxWriter writer, Stream reader) {
          if (base.Size == 0UL)
          {
            base.Write(writer);
            reader.CopyTo(writer.BaseStream);
          }
          else
            using (new SizeCalculator(this, writer))
            {
                base.Write(writer);
                reader.CopyTo(writer.BaseStream);
            }
        }

        public override string ToString()
        {
            StringBuilder xml = new StringBuilder();
            xml.Append(base.ToString());
            xml.Append("<mdat/>");
            xml.Append("</box>");
            return xml.ToString();
        }

        public ulong PayloadOffset { get; private set; }  // for our records, but not part of the 'official' spec/box
    }
}
