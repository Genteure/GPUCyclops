// SyncSampleMapBox
//
namespace Media.Formats.MP4
{
  using System;
  using System.Text;
  using System.Linq;
  using System.IO;

  public class SyncSampleMapBox : FullBox
  {
    public SyncSampleMapBox() : base(BoxTypes.SyncSampleMap)
    {
      this.Size += 4UL; // EntryCount
    }

    public uint EntryCount { get; private set; }
    public uint[] SampleNumbers { get; private set; }

    public override void Read(BoxReader reader)
    {
        using (new SizeChecker(this, reader))
        {
            base.Read(reader);

            EntryCount = reader.ReadUInt32();
            SampleNumbers = new uint[EntryCount];
            for (int i = 0; i < EntryCount; i++)
            {
                SampleNumbers[i] = reader.ReadUInt32();
            }
        }
    }

    public override string ToString()
    {
      StringBuilder xml = new StringBuilder();

      xml.Append(base.ToString()); // <box>
      xml.Append("<entryCount>").Append(EntryCount.ToString()).Append("</entryCount>");
      xml.Append("<sampleNumbers>");

		for (int i = 0; i < EntryCount && SampleNumbers != null && SampleNumbers.Length > i; i++)
      {
        xml.Append(SampleNumbers[i].ToString()).Append(" ");
      }

      xml.Append("</sampleNumbers>");
      xml.Append("</box>");

      return (xml.ToString());
    }

    /// <summary>
    /// IsIFrame
    /// Determine whether a sample is an IFrame.
    /// </summary>
    /// <param name="index">Index that starts from 1 (not 0-based)</param>
    /// <returns></returns>
    public bool IsIFrame(uint index)
    {
      return SampleNumbers.Any(ifr => ifr == index);
    }

    /// <summary>
    /// GetLastIFrame
    /// Given an index, look for the last IFrame before index. If index is an IFrame, return the input index.
    /// </summary>
    /// <param name="index">Index that starts from 1 (not 0-based)</param>
    /// <returns></returns>
    public uint GetLastIFrame(uint index)
    {
      uint retVal = index;

      for (int i = (int)index; i > 0; i--)
      {
        if (IsIFrame((uint)i))
        {
          return (uint)i;
        }
      }

      return (uint)0;  // IFrame not found
    }


    BinaryReader SyncSampleReader = null;

    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.WriteUInt32(EntryCount);
            if (SyncSampleReader != null)
            {
              for (int i = 0; i < EntryCount; i++)
              {
                writer.WriteUInt32(SyncSampleReader.ReadUInt32());
              }
            }
            else if ((SampleNumbers != null) && (EntryCount == SampleNumbers.Length))
            {
              for (int i = 0; i < EntryCount; i++)
              {
                writer.WriteUInt32(SampleNumbers[i]);
              }
            }
            else throw new Exception("SyncSampleMapBox.Write: nothing to write");
        }
    }

    public void FinalizeBox(BinaryReader syncSampleReader, uint count)
    {
      if (syncSampleReader.BaseStream.Length != 4 * count)
        throw new Exception("SyncSampleMapBox: inconsistency in stss box entry count");
      EntryCount = count;
      //if (count == 0)
      //  this.Size = 0; // this box will not be written out
      //else
      if (count > 0)
        this.Size += (ulong)syncSampleReader.BaseStream.Length;
      this.SyncSampleReader = syncSampleReader;
    }
  }
}
