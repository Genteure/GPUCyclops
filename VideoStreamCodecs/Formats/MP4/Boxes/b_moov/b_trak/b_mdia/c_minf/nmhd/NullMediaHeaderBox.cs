using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Media.Formats.MP4
{
  public class NullMediaHeaderBox : FullBox {
    public NullMediaHeaderBox() : base(BoxTypes.NullMediaHeader) {}


    public override void Read(BoxReader reader)
    {
        using (new SizeChecker(this, reader))
        {
            base.Read(reader);
        }
    }

    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(base.ToString());
        sb.Append("</box>");
        return sb.ToString();
    }
  }
}


