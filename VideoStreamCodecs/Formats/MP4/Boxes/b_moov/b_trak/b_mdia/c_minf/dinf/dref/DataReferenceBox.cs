/*
aligned(8) class DataReferenceBox
  extends FullBox(‘dref’, version = 0, 0) {
    unsigned int(32) entry_count;
    for (i=1; i <= entry_count; i++) {
      DataEntryBox(entry_version, entry_flags) data_entry;
    }
  }
*/

using System.Text;
using System.Diagnostics;

namespace Media.Formats.MP4
{
  class DataReferenceBox : FullBox {
    public DataReferenceBox() : base(BoxTypes.DataReference) {
      this.Size += 4UL; // entry count
      this.EntryCount = 1;
      DataEntry = new DataEntry[1];
      DataEntry[0] = new DataEntry();
      DataEntry[0].DataEntryUrlBox = new DataEntryUrlBox();
      this.Size += DataEntry[0].DataEntryUrlBox.Size;
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);

        EntryCount = reader.ReadUInt32();
        DataEntry = new DataEntry[EntryCount];

        for (int i=0; i<EntryCount; i++) {
          long pos = reader.BaseStream.Position;
          Box test = new Box(BoxTypes.Any);
          test.Read(reader);
          reader.BaseStream.Position = pos;

          DataEntry entry = new DataEntry();
          if (test.Type == BoxTypes.DataEntryUrn) {
            entry.DataEntryUrnBox = new DataEntryUrnBox();
            entry.DataEntryUrnBox.Read(reader);
          }

          else if (test.Type == BoxTypes.DataEntryUrl)
          {
              entry.DataEntryUrlBox = new DataEntryUrlBox();
              entry.DataEntryUrlBox.Read(reader);
              //            if (entry.DataEntryUrlBox.bBug == true) this.Size += 1;
          }

          else
          {
              test.Read(reader); // skip
              Debug.WriteLine(string.Format("Unknown box type {0} in DataReferenceBox (dref), skipped", test.Type.ToString()));
          }

          DataEntry[i] = entry;
        }
      }
    }


    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.WriteUInt32(EntryCount);
            if (EntryCount > 0)
            foreach (DataEntry entry in DataEntry)
            {
              if (entry.DataEntryUrlBox != null)
              {
                entry.DataEntryUrlBox.Write(writer);
              }
              if (entry.DataEntryUrnBox != null)
              {
                entry.DataEntryUrnBox.Write(writer);
              }
            }
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<entrycount>").Append(EntryCount).Append("</entrycount>");
      for (int i=0; i<EntryCount; i++) {
          if (DataEntry[i].DataEntryUrlBox != null)
            xml.Append(DataEntry[i].DataEntryUrlBox.ToString());
          if (DataEntry[i].DataEntryUrnBox != null)
              xml.Append(DataEntry[i].DataEntryUrnBox.ToString());
      }
      xml.Append("</box>");

      return (xml.ToString());
    }


    public uint EntryCount;
    public DataEntry[] DataEntry;
  }
}
