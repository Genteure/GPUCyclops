// <summary>
// Sample To Chunk Box
// Samples within the media data (mdat) are grouped into chunks of varying sizes.
// In a chunk, samples can be of varying sizes also. This table can be used to find
// the chunk that contains a sample, its position, and assodiated sample description.
// </summary>

using System;
using System.Text;
using System.IO;

namespace Media.Formats.MP4
{
  public class ChunkEntry {
    public uint firstChunk;
    public uint samplesPerChunk;
    public uint sampleDescriptionIndex; // index into StsdBox entries
  }

  public class SampleToChunkBox : FullBox {
    SampleTableBox parent;
    public uint TotalChunks;

    public SampleToChunkBox(SampleTableBox inParent) : base(BoxTypes.SampleToChunk) {
      parent = inParent;
      this.Size += 4UL; // EntryCount
    }

    public void CheckIntegrityOfChunkData()
    {
			if (EntryCount == 0) return;
      int sampleCount = 0;
      int chunkCount = 0;
      int chnkGrp;
      int chunkCountInGroup;
      for (chnkGrp = 0; chnkGrp < (EntryCount - 1); chnkGrp++)
      {
        chunkCountInGroup = (int)(ChunkEntries[chnkGrp + 1].firstChunk - ChunkEntries[chnkGrp].firstChunk);
        int samplesPerChunk = (int)ChunkEntries[chnkGrp].samplesPerChunk;
        sampleCount += samplesPerChunk * chunkCountInGroup;
        chunkCount += chunkCountInGroup;
      }
      chunkCountInGroup = (int)(TotalChunks - (ChunkEntries[chnkGrp].firstChunk - 1));
      chunkCount += chunkCountInGroup;
      sampleCount += (int)(chunkCountInGroup * ChunkEntries[chnkGrp].samplesPerChunk);

      if ((chunkCount != TotalChunks) || (sampleCount != parent.SampleSizeBox.SampleCount))
        throw new Exception("Bad chunk data");
    }


    public override void Read(BoxReader reader)
    {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);

        EntryCount = reader.ReadUInt32();
        ChunkEntries = new ChunkEntry[EntryCount];

        for (int i = 0; i < EntryCount; i++) {
          ChunkEntries[i] = new ChunkEntry();
          ChunkEntries[i].firstChunk = reader.ReadUInt32();
          ChunkEntries[i].samplesPerChunk = reader.ReadUInt32();
          ChunkEntries[i].sampleDescriptionIndex = reader.ReadUInt32();
        }
      }
      if (parent.ChunkOffSetBox != null)
          TotalChunks = parent.ChunkOffSetBox.EntryCount;
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<entrycount>").Append(EntryCount).Append("</entrycount>");
      xml.Append("<chunkentries>");
		for (int i = 0; i < EntryCount && ChunkEntries != null && ChunkEntries.Length > i; i++)
		{
        xml.Append("<chunk>");
        xml.Append("<firstChunk>").Append(ChunkEntries[i].firstChunk).Append("</firstChunk>");
        xml.Append("<samplesPerChunk>").Append(ChunkEntries[i].samplesPerChunk).Append("</samplesPerChunk>");
        xml.Append("<sampleDescriptionIndex>").Append(ChunkEntries[i].sampleDescriptionIndex).Append("</sampleDescriptionIndex>");
        xml.Append("</chunk>");
      }
      xml.Append("</chunkentries>");
      xml.Append("</box>");

      return (xml.ToString());
    }

    public uint EntryCount = 0;
    public ChunkEntry[] ChunkEntries;

    /// <summary>
    /// GetFileOffset
    /// Given a sample index, return the stream offset for the payload of that sample.
    /// Warning: this code is brittle.
    /// <!-- This routine, if modified, should be code-inspected after modification. We've had erroneous modifications
    /// that do not show a symptom until that one rare case in which the error would manifest itself.
    /// For example, changing the loop condition compare logic below from the current '<=' to just '<' would not reveal
    /// the error until triggered by difficult-to-determine conditions. -->
    /// </summary>
    /// <param name="sample">uint</param>
    /// <returns>Offset, which is a long integer.</returns>
    public long GetFileOffset(uint sample)
    {
        uint currentChunk = 1;
        uint currentChunkGroup = 0;
        uint firstSampleInChunk = 1;

        if (parent.ChunkOffSetBox.EntryCount != TotalChunks)
          throw new Exception("Chunk Offset box inconsistent with Chunk Group box");

        // search for chunk in which sample belongs
        uint samplesPerChunk = ChunkEntries[currentChunkGroup].samplesPerChunk;
        while ((firstSampleInChunk + samplesPerChunk) <= sample)
        {
            firstSampleInChunk += samplesPerChunk;
            // increment currentChunk
            // first, check whether the next chunk belongs to the next chunk group
            if ((currentChunkGroup + 1 < EntryCount) && (ChunkEntries[currentChunkGroup + 1].firstChunk == (currentChunk + 1)))
            {
                currentChunkGroup++;
                currentChunk = ChunkEntries[currentChunkGroup].firstChunk;
                samplesPerChunk = ChunkEntries[currentChunkGroup].samplesPerChunk;
            }
            else if (currentChunk <= TotalChunks)
                currentChunk++;
            else throw new Exception(string.Format("Invalid sample ID: {0}, count of samples = {1}", sample, parent.SampleSizeBox.SampleCount));
        }
        if (currentChunk > parent.ChunkOffSetBox.EntryCount) // ChunkCount)
            throw new Exception("Invalid chunk index");
        long fileOffset = (long)parent.ChunkOffSetBox.ChunkOffsets[currentChunk - 1];
        if (firstSampleInChunk < sample)
        {
          for (int i = (int)firstSampleInChunk; i < sample; i++)
          {
            fileOffset += parent.SampleSizeBox.SampleSize;
            if (parent.SampleSizeBox.SampleSize == 0)
              fileOffset += parent.SampleSizeBox.SampleSizeArray[i - 1];
          }
        }
        return fileOffset;
    }

    #region Box Builder -- Routines for building a Sample To Chunk Box from scratch, and for writing it out to a file.

    ChunkEntry chunk = null;
    Stream ChunkGroupStream; // this stream is written to by CreateNewChunkGroup
    BinaryWriter ChunkGroupWriter = null;
    BinaryReader ChunkGroupReader = null;
    public BinaryWriter ChunkOffsetWriter = null; // this is public because we need to access it in SampleTableBox.cs
    string ChunkGroupFileName;


    public override void Write(BoxWriter writer)
    {
      using (new SizeCalculator(this, writer))
      {
        base.Write(writer);
        writer.WriteUInt32(EntryCount);
        if (ChunkGroupReader != null)
        {
          for (int i = 0; i < EntryCount; i++)
          {
            writer.WriteUInt32(ChunkGroupReader.ReadUInt32());
            writer.WriteUInt32(ChunkGroupReader.ReadUInt32());
            writer.WriteUInt32(ChunkGroupReader.ReadUInt32());
          }
        }
        else if ((ChunkEntries != null) && (ChunkEntries.Length == EntryCount))
        {
          for (int i = 0; i < EntryCount; i++)
          {
            writer.WriteUInt32(ChunkEntries[i].firstChunk);
            writer.WriteUInt32(ChunkEntries[i].samplesPerChunk);
            writer.WriteUInt32(ChunkEntries[i].sampleDescriptionIndex);
          }
        }
        else throw new Exception("SampleToChunkBox.Write: Nothing to write");
      }
    }

    /// <summary>
    /// CreateNewChunkGroup
    /// There is no rule about how many chunks there can be in  chunk group. A chunk group may contain ALL chunks 
    /// in a media file. There's a tradeoff between the resulting size of this SampleToChunkBox, and the size of
    /// ChunkOffsetBox. Reducing the size of this SampleToChunkBox (by having less chunk groups) must necessarily 
    /// increase the size of ChunkOffsetBox. In the degenerate case in which there is one slice in every chunk, all
    /// chunks maybe put in a single chunk group, but there would be an entry in the ChunkOffsetBox for every slice. 
    /// In the other extreme case in which all slices are in one single chunk, there would also be only one chunk group 
    /// and there would also be only one entry in the ChunkOffsetBox.
    /// 
    /// Every chunk group entry created is written out to the ChunkGroupStream. See the Write method also.
    /// </summary>
    /// <param name="sampleCount"></param>
    void CreateNewChunkGroup(uint sampleCount)
    {
      if (ChunkGroupWriter == null)
      {
        ChunkGroupFileName = Path.GetTempFileName();
        ChunkGroupStream = File.Create(ChunkGroupFileName);
        ChunkGroupWriter = new BinaryWriter(ChunkGroupStream);
      }
      chunk = new ChunkEntry();
      chunk.firstChunk = parent.ChunkOffSetBox.EntryCount + 1; // the ChunkOffsetBox is also being built on the fly
      chunk.sampleDescriptionIndex = 1; // FIXME: this assumes that for every track there's only one sample description
      chunk.samplesPerChunk = sampleCount; // in this chunk group, every chunk has this many slices
      ChunkGroupWriter.Write(chunk.firstChunk);
      ChunkGroupWriter.Write(chunk.samplesPerChunk);
      ChunkGroupWriter.Write(chunk.sampleDescriptionIndex);
      EntryCount++;
    }

    public void Cleanup()
    {
      if (ChunkGroupStream != null)
      {
        ChunkGroupStream.Close();
        ChunkGroupWriter.Dispose();
        ChunkGroupReader.Dispose();
        File.Delete(ChunkGroupFileName);
      }
    }

    /// <summary>
    /// SetFileOffset
    /// Given a sample index and its length, add it to the current chunk. The parameter samplesInThisBatch is set to 1 in the
    /// SampleTableBox. This means that there is only one slice per chunk. A new chunk offset and possibly a new chunk group
    /// entry is created in this box.
    /// </summary>
    /// <param name="sample">uint sample index</param>
    /// <param name="length">uint sample length in bytes</param>
    /// <param name="samplesInThisBatch">count of samples in this batch</param>
    /// <param name="needNewChunk">true if a new chunk is needed</param>
    /// <param name="currMdatOffset">ulong current offset into mdat</param>
    public void SetFileOffsetForChunk(uint sample, uint length, uint samplesInThisBatch, bool needNewChunk, ref ulong currMdatOffset)
    {
      if (chunk == null) // first call for this TrackBox
      {
        CreateNewChunkGroup(samplesInThisBatch);
      }

      if (needNewChunk)
      {
        //uint c = parent.ChunkOffSetBox.EntryCount;
        //parent.ChunkOffSetBox.ChunkOffsets[c] = (uint)currMdatOffset; // if there's only one chunk, only the first one will have offset
        if (ChunkOffsetWriter == null)
          ChunkOffsetWriter = new BinaryWriter(File.Create(Path.GetTempFileName()));
        ChunkOffsetWriter.Write((uint)currMdatOffset);
        if (chunk.samplesPerChunk != samplesInThisBatch)
        {
          // need new group
          CreateNewChunkGroup(samplesInThisBatch);
        }
        parent.ChunkOffSetBox.EntryCount++;
      }
      currMdatOffset += length;
    }

    /// <summary>
    /// FinalizeBox
    /// Finalize this sample-to-chunk box. Take the chunk group stream, determine the Size of this box by simply using the length
    /// of the chunk group stream.
    /// </summary>
    public void FinalizeBox()
    {
      ChunkGroupWriter.BaseStream.Position = 0L;
      ChunkGroupReader = new BinaryReader(ChunkGroupWriter.BaseStream);
      if ((this.Size + (ulong)ChunkGroupReader.BaseStream.Length) > uint.MaxValue)
        throw new Exception("SampleToChunkBox size too large");
      this.Size += (ulong)ChunkGroupReader.BaseStream.Length;
    }

    #endregion
  }
}

