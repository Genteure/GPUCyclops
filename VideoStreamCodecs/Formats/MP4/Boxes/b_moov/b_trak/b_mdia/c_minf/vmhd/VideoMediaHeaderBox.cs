/*
  8.4.5.2.1 Syntax
    aligned(8) class VideoMediaHeaderBox
    extends FullBox(‘vmhd’, version = 0, 1) {
      template unsigned int(16) graphicsmode = 0; // copy, see below
      template unsigned int(16)[3] opcolor = {0, 0, 0};
    }
  
  8.4.5.2.2 Semantics
    version is an integer that specifies the version of this box
    graphicsmode specifies a composition mode for this sourceVideo rawTrack, from the following enumerated set,
    which may be extended by derived specifications:
    copy = 0 copy over the existing image
    opcolor is a set of 3 colour values (red, green, blue) available for use by graphics modes
 * */

using System.Text;

namespace Media.Formats.MP4
{

  public class VideoMediaHeaderBox : FullBox {
    public VideoMediaHeaderBox() : base(BoxTypes.VideoMediaHeader) 
    {
      this.Flags = 1;
      this.Size += 8UL;
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);
        GraphicsMode = reader.ReadUInt16();
        OpColor[0] = reader.ReadUInt16();
        OpColor[1] = reader.ReadUInt16();
        OpColor[2] = reader.ReadUInt16();
      }
    }


    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.WriteUInt16(GraphicsMode);
            writer.WriteUInt16(OpColor[0]);
            writer.WriteUInt16(OpColor[1]);
            writer.WriteUInt16(OpColor[2]);
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<graphicsmode>").Append(GraphicsMode).Append("</graphicsmode>");
      xml.Append("<opcolor>").Append(OpColor[0]).Append(OpColor[1]).Append(OpColor[2]).Append("</opcolor>");
      xml.Append("</box>");
      return (xml.ToString());
    }

    public ushort GraphicsMode;
    public ushort[] OpColor = new ushort[3];
  }
}
