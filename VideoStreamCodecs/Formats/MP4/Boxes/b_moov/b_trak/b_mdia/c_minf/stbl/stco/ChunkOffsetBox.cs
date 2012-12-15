// <summary>
// Chunk Offset Box
// Gives location of each chunk in the mp4 file.
// </summary>

using System;
using System.Text;
using System.IO;

namespace Media.Formats.MP4
{
  public class ChunkOffSetBox : FullBox {
    public SampleTableBox parent;
    private BinaryReader ChunkOffsetReader;

    public ChunkOffSetBox(SampleTableBox inParent) : base(BoxTypes.ChunkOffset) {
      parent = inParent;
      this.Size += 4UL; // entryCount
    }

    /// <summary>
    /// Read - read chunk offset box from input MP4 stream.
    /// </summary>
    /// <param name="reader">BoxReader</param>
    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);

        entryCount = reader.ReadUInt32();
        chunkOffsetArray = new uint[entryCount];
        for (int i = 0; i < entryCount; i++) {
          chunkOffsetArray[i] = reader.ReadUInt32();
        }
      }
      if (parent.SampleToChunkBox != null)
          parent.SampleToChunkBox.TotalChunks = entryCount;
    }

    int fixup = 0;


    /// <summary>
    /// Write - write the ChunkOffsetBox to output file.
    /// For every chunk offset written out, add fixup.
    /// ChunkOffset data is either stored in a temp file, or in the chunkOffsetArray.
    /// Data is in the temp file if we are  using PrepareSampleWriting, and in the chunkOffsetArray
    /// if we are simply copying boxes as they are, no recoding.
    /// </summary>
    /// <param name="writer"></param>
    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);

            writer.WriteUInt32(entryCount);
            if (entryCount > 0)
            {
              if (ChunkOffsetReader != null)
              {
                ChunkOffsetReader.BaseStream.Position = 0L;
                for (int i = 0; i < entryCount; i++)
                {
                  uint offs = (uint)(ChunkOffsetReader.ReadUInt32() + fixup);
                  writer.WriteUInt32(offs);
                }
              }
              else if ((chunkOffsetArray != null) && (entryCount == chunkOffsetArray.Length))
              {
                for (int i = 0; i < entryCount; i++)
                {
                  writer.WriteUInt32((uint)(chunkOffsetArray[i] + fixup));
                }
              }
              else throw new Exception("ChunkOffsetBox.Write: nothing to write");
            }
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<entrycount>").Append(EntryCount).Append("</entrycount>");
      xml.Append("<ChunkOffsets>");
		for (int i = 0; i < EntryCount && ChunkOffsets != null && ChunkOffsets.Length > i; i++)
		{
        xml.Append("<offset>").Append(ChunkOffsets[i]).Append("</offset>");
      }
      xml.Append("</ChunkOffsets>");
      xml.Append("</box>");

      return (xml.ToString());
    }

    public void FinalizeBox(BinaryReader offsetReader)
    {
      ChunkOffsetReader = offsetReader;
      this.Size += (ulong)offsetReader.BaseStream.Length;
    }

    /// <summary>
    /// Fixup
    /// When fixup is called, this box has not been written to the destination file as yet.
    /// Read the temp file, and write it back with fixup added to every element.
    /// </summary>
    /// <param name="headerSize"></param>
    public void Fixup(int headerSize)
    {
      fixup = headerSize;
    }


    uint entryCount;
    public uint EntryCount {
      get { return entryCount; }
      set { entryCount = value; }
    }

    uint[] chunkOffsetArray;
    public uint[] ChunkOffsets {
      get { return chunkOffsetArray; }
    }
  }
}

