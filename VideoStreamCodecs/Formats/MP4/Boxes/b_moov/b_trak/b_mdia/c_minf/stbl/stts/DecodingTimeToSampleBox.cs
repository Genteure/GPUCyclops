/*
8.6.1.2.2 Syntax
  aligned(8) class TimeToSampleBox
    extends FullBox(’stts’, version = 0, 0) {
      unsigned int(32) entry_count;
      int i;
      for (i=0; i < entry_count; i++) {
        unsigned int(32) sample_count;
        unsigned int(32) sample_delta;
      }
    }
  For example with Table 2, the entry would be:
  Sample count Sample-delta
  14 10
8.6.1.2.3 Semantics
  version - is an integer that specifies the version of this box.
  entry_count - is an integer that gives the number of entries in the following table.
  sample_count - is an integer that counts the number of consecutive samples that have the given
  duration.
  sample_delta - is an integer that gives the delta of these samples in the time-scale of the media
 */

using System.Text;
using System.IO;
using System;

namespace Media.Formats.MP4
{
  public class DecodingTimeToSampleBox : FullBox {
    public SampleTableBox parent;
    public DecodingTimeToSampleBox(SampleTableBox inParent) : base(BoxTypes.TimeToSample) {
      parent = inParent;
      this.Size += 4UL; // EntryCount
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);

        EntryCount = reader.ReadUInt32();
        SampleCount = new uint[EntryCount];
        SampleDelta = new uint[EntryCount];
        for (int i=0; i<EntryCount; i++) {
          SampleCount[i] = reader.ReadUInt32();
          SampleDelta[i] = reader.ReadUInt32();
        }
      }
    }


    private BinaryReader SttsCountsReader = null;
    private BinaryReader SttsTimeDeltaReader = null;

    public void FinalizeBox(BinaryReader SttsCountsReader, BinaryReader SttsTimeDeltaReader, uint CurrSttsCount)
    {
      Stream SttsCountsStream = SttsCountsReader.BaseStream;
      if (SttsCountsStream.Position > 0) // stts stream must be at the beginning
        throw new Exception("DecodingTimeToSampleBox: must call Finalize first before writing out to output file");
      if ((CurrSttsCount != SttsCountsStream.Length/4) || (CurrSttsCount != SttsTimeDeltaReader.BaseStream.Length/4))
        throw new Exception("DecodingTimeToSampleBox: stts temp file inconsistency");
      this.EntryCount = CurrSttsCount;
      this.SttsCountsReader = SttsCountsReader;
      this.SttsTimeDeltaReader = SttsTimeDeltaReader;
      this.Size += (ulong)(SttsCountsStream.Length + SttsTimeDeltaReader.BaseStream.Length);
    }


    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.WriteUInt32(EntryCount);
            if (SttsCountsReader != null)
            {
              for (int i = 0; i < EntryCount; i++)
              {
                writer.WriteUInt32(SttsCountsReader.ReadUInt32());
                writer.WriteUInt32(SttsTimeDeltaReader.ReadUInt32());
              }
            }
            else if ((SampleCount != null) && (SampleDelta != null) && (SampleCount.Length == EntryCount) && (SampleDelta.Length == EntryCount))
            {
              for (int i = 0; i < EntryCount; i++)
              {
                writer.WriteUInt32(SampleCount[i]);
                writer.WriteUInt32(SampleDelta[i]);
              }
            }
            else throw new Exception("Nothing to write");
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<entrycount>").Append(EntryCount).Append("</entrycount>");
      xml.Append("<entries>");
		for (int i = 0; i < EntryCount && SampleCount != null && SampleCount.Length > i; i++)
		{
        xml.Append("<entry>");
        xml.Append("<samplecount>").Append(SampleCount[i]).Append("</samplecount>");
        xml.Append("<sampledelta>").Append(SampleDelta[i]).Append("</sampledelta>");
        xml.Append("</entry>");
      }
      xml.Append("</entries>");
      xml.Append("</box>");

      return (xml.ToString());
    }


    public uint EntryCount;
    public uint[] SampleCount;
    public uint[] SampleDelta;
  }
}
