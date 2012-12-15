/*
aligned(8) class DataInformationBox extends Box(‘dinf’) {
}
*/

using System.Text;

namespace Media.Formats.MP4
{
  public class DataInformationBox : Box {
    public DataInformationBox() : base(BoxTypes.DataInformation) {
      DataReferenceBox = new DataReferenceBox();
      this.Size += DataReferenceBox.Size;
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);
        DataReferenceBox.Read(reader);
      }
    }


    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            DataReferenceBox.Write(writer);
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append(DataReferenceBox.ToString());
      xml.Append("</box>");

      return (xml.ToString());
    }

    DataReferenceBox DataReferenceBox = new DataReferenceBox();
  }
}
