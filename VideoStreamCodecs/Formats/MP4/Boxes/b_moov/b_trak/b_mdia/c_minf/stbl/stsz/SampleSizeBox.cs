// <summary>
// Sample Size Box
// Give the size of each sample, in bytes.
// </summary>

using System;
using System.Text;
using System.IO;

namespace Media.Formats.MP4
{
  public class SampleSizeBox : FullBox {
    public SampleTableBox parent;
    public SampleSizeBox(SampleTableBox inParent)
      : base(BoxTypes.SampleSize) {
      parent = inParent;
      sampleCount = 0;
      sampleSize = 0;
      this.Size += 8UL; // default sample size plus sample count
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);

        sampleSize = reader.ReadUInt32();
        sampleCount = reader.ReadUInt32();
        if (sampleSize == 0)
        {
            sampleSizeArray = new uint[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
              sampleSizeArray[i] = reader.ReadUInt32();
            }
        }

      }
    }

    public override string ToString()
    {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<SampleSize>").Append(SampleSize).Append("</SampleSize>");
      xml.Append("<SampleCount>").Append(SampleCount).Append("</SampleCount>");
      if (SampleSize == 0)
      {
        xml.Append("<SampleSizeArray>");
		  for (int i = 0; i < SampleCount && SampleSizeArray != null && SampleSizeArray.Length > i; i++)
        {
          xml.Append("<SampleSize>").Append(SampleSizeArray[i]).Append("</SampleSize>");
        }
        xml.Append("</SampleSizeArray>");
      }
      xml.Append("</box>");

      return (xml.ToString());
    }


    BinaryReader SampleSizeReader = null;

    /// <summary>
    /// FinalizeBox
    /// This also optimizes the size of the array by detecting that all elements are equal, and if so
    /// removing the array and replacing it with a single SampleSize value.
    /// </summary>
    /// <param name="SampleSizeReader"></param>
    /// <param name="count"></param>
    public void FinalizeBox(BinaryReader SampleSizeReader, uint count)
    {
      if (SampleSizeReader.BaseStream.Length != (long)4*count)
        throw new Exception("SampleSizeBox: stsz temp file inconsistency");

      uint val = SampleSizeReader.ReadUInt32();
      for (int i = 0; i < (count - 1); i++)
      {
        if (val != SampleSizeReader.ReadUInt32())
        {
          val = 0;
          break;
        }
      }
      SampleSizeReader.BaseStream.Position = 0L;

      this.sampleSize = val; // must be zero if samples have different sizes
      if (val == 0)
      {
        this.sampleCount = count;
        this.Size += (ulong)SampleSizeReader.BaseStream.Length;
        this.SampleSizeReader = SampleSizeReader;
      }
    }


    public override void Write(BoxWriter writer)
    {
      using (new SizeCalculator(this, writer))
      {
          base.Write(writer);
          writer.WriteUInt32(sampleSize);
          writer.WriteUInt32(SampleCount);
          if (SampleSizeReader != null)
          {
            if ((sampleSize == 0) && (SampleCount > 0))
            {
              if (SampleCount * 4 != SampleSizeReader.BaseStream.Length)
                throw new Exception("Inconsistent SampleSizeBox array size");
              for (int i = 0; i < sampleCount; i++)
                writer.WriteUInt32(SampleSizeReader.ReadUInt32());
            }
          }
          else if ((SampleSizeArray != null) && (SampleCount == SampleSizeArray.Length))
          {
            if ((sampleSize == 0) && (SampleCount > 0))
            {
              for (int i = 0; i < sampleCount; i++)
                writer.WriteUInt32(SampleSizeArray[i]);
            }
          }
          // don't throw an exception here (it won't be caught because we are inside multiple levels of using blocks;
          // let SiceCalculator detect the problem.
          //else throw new Exception("SampleSizeBox.Write: nothing to write");
       }
    }


    uint sampleSize; // must be zero, if not then sampleSizeArray in this box is not present
    uint sampleCount;
    uint[] sampleSizeArray;

    public uint SampleSize {
      get { return sampleSize; }
    }

    public uint SampleCount {
      get { return sampleCount; }
    }

    public uint[] SampleSizeArray {
      get { return sampleSizeArray; }
    }

    public uint GetSizeOfSamples(uint fromSample, uint toSample) {
      uint total = 0;
      if (toSample < fromSample)
        throw new Exception("StblClasses.cs: StszBox.GetSizeOfSamples: invalid param");
      for (uint i = fromSample; (i < toSample) && (i < sampleCount); i++) {
        total += sampleSizeArray[i];
      }
      return total;
    }


  }
}

