/*
aligned(8) class DataInformationBox extends Box(‘dinf’) {
}
*/

using System.Text;

namespace Media.Formats.MP4
{
  class DataEntryUrlBox : FullBox {
//    public bool bBug = false;
    public DataEntryUrlBox() : base(BoxTypes.DataEntryUrl) 
    {
      base.Flags = 1;
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        long pos = reader.BaseStream.Position;
        base.Read(reader);
//        if (reader.BaseStream.Position - pos == this.Size) {
          // BUG!!!, Microsoft says the size is X, but it needs one more byte for the null terminated string!!!
//          this.Size += 1;
//          bBug = true;
//        }
        if (EnumUtils.IsBitSet<DataEntryFlags>((DataEntryFlags)base.Flags, DataEntryFlags.MediaDataSameFile) == false) 
          Location = reader.ReadNullTerminatedString();
      }
    }


    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            if (EnumUtils.IsBitSet<DataEntryFlags>((DataEntryFlags)base.Flags, DataEntryFlags.MediaDataSameFile) == false)
                writer.WriteNullTerminatedString(Location);
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      if (EnumUtils.IsBitSet<DataEntryFlags>((DataEntryFlags)base.Flags, DataEntryFlags.MediaDataSameFile) == false)
        xml.Append("<location>").Append(Location).Append("</location>");
      xml.Append("</box>");

      return (xml.ToString());
    }


    public string Location;
  }
}
