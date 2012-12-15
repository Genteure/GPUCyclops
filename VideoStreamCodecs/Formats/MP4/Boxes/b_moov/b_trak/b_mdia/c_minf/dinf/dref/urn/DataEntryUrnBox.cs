/*
aligned(8) class DataInformationBox extends Box(‘dinf’) {
}
*/

using System.Text;

namespace Media.Formats.MP4
{

  class DataEntryUrnBox : FullBox {
    public DataEntryUrnBox() : base(BoxTypes.DataEntryUrn) {
      if ((Name != null) && (Location != null))
        this.Size += (ulong)(Name.Length + Location.Length + 2);
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);
        Name = reader.ReadNullTerminatedString();
        Location = reader.ReadNullTerminatedString();
      }
    }


    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.WriteNullTerminatedString(Name);
            writer.WriteNullTerminatedString(Location);
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<name>").Append(Name).Append("</name>");
      xml.Append("<location>").Append(Location).Append("</location>");
      xml.Append("</box>");

      return (xml.ToString());
    }


    public string Name;
    public string Location;
  }
}
