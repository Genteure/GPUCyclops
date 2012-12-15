/*
8.6.1.3.2 Syntax
  aligned(8) class CompositionOffsetBox
  extends FullBox(‘ctts’, version = 0, 0) {
    unsigned int(32) entry_count;
    int i;
    for (i=0; i < entry_count; i++) {
      unsigned int(32) sample_count;
      unsigned int(32) sample_offset;
    }
  }
For example in Table 2
  Sample count Sample_offset
  1 10
  1 30
  2 0
  1 30
  2 0
  1 10
  1 30
  2 0
  1 30
  2 0
  
8.6.1.3.3 Semantics
  version - is an integer that specifies the version of this box.
  entry_count is an integer that gives the number of entries in the following table.
  sample_count is an integer that counts the number of consecutive samples that have the given offset.
  sample_offset is a non-negative integer that gives the offset between CT and DT, such that CT(n) =
DT(n) + CTTS(n).
 */

using System;
using System.Text;
using System.IO;

namespace Media.Formats.MP4
{
  public class CompositionTimeToSample : FullBox {

    public SampleTableBox parent;

    Stream CompToSampleStream;         // ctts data temp stream file
    BinaryWriter CompToSampleWriter;
    BinaryReader CompToSampleReader;
    string CompToSampleFileName;

    public CompositionTimeToSample(SampleTableBox inParent) : base(BoxTypes.CompositionOffset) {
      parent = inParent;
      this.Size += 4UL; // +EntryCount * 8;
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);

        EntryCount = reader.ReadUInt32();
        SampleCount = new uint[EntryCount];
        SampleOffset = new uint[EntryCount];
        for (int i=0; i<EntryCount; i++) {
          SampleCount[i] = reader.ReadUInt32();
          SampleOffset[i] = reader.ReadUInt32();
        }
      }
    }

    public override string ToString()
    {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<entrycount>").Append(EntryCount).Append("</entrycount>");
      xml.Append("<entries>");
		for (int i = 0; i < EntryCount && SampleCount != null && SampleCount.Length > i; i++)
      {
        xml.Append("<samplecount>").Append(SampleCount[i]).Append("</samplecount>");
        xml.Append("<sampleoffset>").Append(SampleOffset[i]).Append("</sampleoffset>");
      }
      xml.Append("</entries>");
      xml.Append("</box>");

      return (xml.ToString());
    }


    public override void Write(BoxWriter writer)
    {
			if (this.EntryCount == 0) {
				// this means we had no CTTS's which were different and thus this file 
				// doesn't need them and most likely has no b-frames...
				return;
			}

        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.WriteUInt32(EntryCount);
            if (EntryCount > 0)
            {
              if (CompToSampleStream != null)
              {
                CompToSampleStream.Position = 0L;
                CompToSampleReader = new BinaryReader(CompToSampleStream);
                for (int i = 0; i < EntryCount; i++)
                {
                  writer.WriteUInt32(CompToSampleReader.ReadUInt32());
                  writer.WriteUInt32(CompToSampleReader.ReadUInt32());
                }
                CompToSampleStream.Close();
                CompToSampleStream.Dispose();
              }
              else
              {
                for (int i = 0; i < EntryCount; i++)
                {
                  writer.WriteUInt32(SampleCount[i]);
                  writer.WriteUInt32(SampleOffset[i]);
                }
              }
            }
        }
    }

  	private bool bFirstCTTS = true;
		private uint prevCTTS = uint.MaxValue;
		private uint count = 0;

    public void AddEntry(uint CTTS)
    {
			// if we are the first entry, setup count and previous...
			if (bFirstCTTS) {
				bFirstCTTS = false;
        count = 1;
				prevCTTS = CTTS;
			} else if (prevCTTS == CTTS) { // if we are the same 
				count++;
			} else { // if we get here than the CTTS is different and we need to do stuff about this...
				
				if (this.EntryCount == 0U) {
					CompToSampleFileName = Path.GetTempFileName();
					CompToSampleStream = File.Create(CompToSampleFileName);
					CompToSampleWriter = new BinaryWriter(CompToSampleStream);
				}

				CompToSampleWriter.Write(count);
				CompToSampleWriter.Write(prevCTTS);
				this.EntryCount++;
				count = 1;
				prevCTTS = CTTS;
				this.Size += 8UL;
			}
    }

    public void FinalizeBox()
    {
			if (this.EntryCount == 0) {
				// this means we had no CTTS's which were different and thus this file 
				// doesn't need them and most likely has no b-frames...
				return;
			}

      // The last CTTS entry is important: Windows Media Player fails without it.
      CompToSampleWriter.Write(count);
			CompToSampleWriter.Write(prevCTTS);
      this.EntryCount++;
      this.Size += 8UL;
      if (this.Size != (8UL * this.EntryCount + 16UL))
        throw new Exception("Inconsistent size property for CompositionTimeToSample box");
    }



    public uint EntryCount;
    public uint[] SampleCount;
    public uint[] SampleOffset;
  }
}

