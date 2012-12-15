/*
8.8.2.2 Syntax
  aligned(8) class MovieExtendsHeaderBox extends FullBox(‘mehd’, version, 0) {
    if (version==1) {
      unsigned int(64) fragment_duration;
    } else { // version==0
      unsigned int(32) fragment_duration;
    }
  }
8.8.2.3 Semantics
  fragment_duration is an integer that declares length of the presentation of the whole movie including
  fragments (in the timescale indicated in the Movie Header Box). The value of this field corresponds to
  the duration of the longest rawTrack, including movie fragments. If an MP4 file is created in real-time, such
  as used in live streaming, it is not likely that the fragment_duration is known in advance and this
  box may be omitted.
*/

using System;
using System.Text;

namespace Media.Formats.MP4
{

  class MovieExtendsHeaderBox : FullBox {
    public MovieExtendsHeaderBox() : base(BoxTypes.MovieExtendsHeader) {
    }

    public MovieExtendsHeaderBox(uint duration)
      : this()
    {
      Version = 1;
      FragmentDuration = (ulong)duration;
      this.Size += 8UL;
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);
        if (Version == 1) {
          FragmentDuration = reader.ReadUInt64();
        } else {
          FragmentDuration = (UInt64)reader.ReadUInt32();
        }
      }
    }


    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            if (Version == 1)
                writer.WriteUInt64(FragmentDuration);
            else
                writer.WriteUInt32((uint)FragmentDuration);
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<fragmentduration>").Append(FragmentDuration).Append("</fragmentduration>");
      xml.Append("</box>");
      return (xml.ToString());
    }


    public UInt64 FragmentDuration;
  }
}
