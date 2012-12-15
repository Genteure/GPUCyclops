/*
  8.4.5.3.1 Syntax
    aligned(8) class SoundMediaHeaderBox
    extends FullBox(‘smhd’, version = 0, 0) {
      template int(16) balance = 0;
      const unsigned int(16) reserved = 0;
    }
  8.4.5.3.2 Semantics
    version is an integer that specifies the version of this box
    balance is a fixed-point 8.8 number that places mono sourceAudio tracks in a stereo space; 0 is centre (the
    normal value); full left is -1.0 and full right is 1.0.
 */

using System.Text;

namespace Media.Formats.MP4
{
  public class SoundMediaHeaderBox : FullBox {
    public SoundMediaHeaderBox() : base(BoxTypes.SoundMediaHeader) {
      this.Size += 4UL;
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);
        Balance = reader.ReadUInt16();
        reader.ReadUInt16(); // reserved = 0
      }
    }


    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.Write((short)Balance);
            writer.Write((short)0);
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<balance>").Append(Balance).Append("</balance>");
      xml.Append("</box>");

      return (xml.ToString());
    }


    public ushort Balance;
  }
}
