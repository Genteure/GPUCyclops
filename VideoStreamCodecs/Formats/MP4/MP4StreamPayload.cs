using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Formats.MP4
{
  /// <summary>
  /// MP4StreamPayload
  /// The MP4Stream base class has access to all MP4 boxes.
  /// This class is derived from MP4StreamReader to allow it to access all boxes.
  /// We use the chunk and chunk offset boxes to access the physical slices (payload).
  /// The (incomplete) utility MP4StreamEditor and the QA program MP4CheckIntegrity uses this class.
  /// </summary>
  public class MP4StreamPayload : MP4StreamReader
  {
    public MP4StreamPayload()
    {
    }

    List<uint>[] SampleCountsInChunk;
    void InitializeSampleCountsInChunk(int trackIndex)
    {
      SampleCountsInChunk[trackIndex] = new List<uint>();
      SampleCountsInChunk[trackIndex].Clear();
      TrackBox[] tracks = this.mmb.TrackBoxes;
      SampleTableBox sampleTable = tracks[trackIndex].MediaBox.MediaInformationBox.SampleTableBox;
      SampleToChunkBox stsc = sampleTable.SampleToChunkBox;
      uint totalChunks = sampleTable.ChunkOffSetBox.EntryCount;
      int chunkEntryIndex;
      uint samplesPerChunk;
      for (chunkEntryIndex = 0; chunkEntryIndex < (stsc.EntryCount - 1); chunkEntryIndex++)
      {
        for (uint chunkCount = stsc.ChunkEntries[chunkEntryIndex].firstChunk; chunkCount < stsc.ChunkEntries[chunkEntryIndex + 1].firstChunk;
          chunkCount++)
        {
          samplesPerChunk = stsc.ChunkEntries[chunkEntryIndex].samplesPerChunk;
          SampleCountsInChunk[trackIndex].Add(samplesPerChunk);
        }
      }
      uint remainingChunkCount = (uint)sampleTable.ChunkOffSetBox.ChunkOffsets.Length - stsc.ChunkEntries[chunkEntryIndex].firstChunk + 1;
      samplesPerChunk = stsc.ChunkEntries[chunkEntryIndex].samplesPerChunk;
      for (; remainingChunkCount > 0; remainingChunkCount--)
        SampleCountsInChunk[trackIndex].Add(samplesPerChunk);
    }

    int SampleToChunkIndex(int trackIndex, uint sampleIndex)
    {
      uint currSampleCount = 0;
      for (int chunkIndex = 0; chunkIndex < SampleCountsInChunk[trackIndex].Count; chunkIndex++)
      {
        uint sampleCount = SampleCountsInChunk[trackIndex][chunkIndex];
        if ((sampleIndex >= currSampleCount) && (sampleIndex < (currSampleCount + sampleCount)))
        {
          return chunkIndex;
        }
        currSampleCount += sampleCount;
      }
      return int.MaxValue;
    }

    uint[] OffsetValues;

    uint GetNextOffset(int countTracks, out int track)
    {
      uint currOffset = uint.MaxValue;
      track = int.MaxValue;
      for (int i = 0; i < countTracks; i++)
      {
        if (currOffset > OffsetValues[i])
        {
          currOffset = OffsetValues[i];
          track = i;
        }
      }
      return currOffset;
    }

    public virtual void ProcessSample(ulong fileOffset, uint size)
    {
    }

    bool[] done;
    public void CollectAllPayload()
    {
      MovieMetadataBox movieMetadata = this.mmb;
      TrackBox[] tracks = movieMetadata.TrackBoxes;
      done = new bool[tracks.Length];
      SampleCountsInChunk = new List<uint>[tracks.Length];
      int[] NextChunkOffsetIndex = new int[tracks.Length];
      OffsetValues = new uint[tracks.Length];
      ChunkOffSetBox[] stco = new ChunkOffSetBox[tracks.Length];
      uint[] sample = new uint[tracks.Length];
      for (int i = 0; i < tracks.Length; i++)
      {
        InitializeSampleCountsInChunk(i);
        NextChunkOffsetIndex[i] = 0;
        stco[i] = tracks[i].MediaBox.MediaInformationBox.SampleTableBox.ChunkOffSetBox;
        sample[i] = 0;
      }
      for (int i = 0; i < tracks.Length; i++)
      {
        OffsetValues[i] = stco[i].ChunkOffsets[NextChunkOffsetIndex[i]];
      }
      int track;
      uint currOffset = GetNextOffset(tracks.Length, out track);
      Stream.Position = (long)currOffset;
      while (done.Any(d => d == false))
      {
        uint offset = GetNextOffset(tracks.Length, out track);
        if (offset != currOffset)
          throw new Exception("Input MP4 file has a problem with chunk offsets");
        NextChunkOffsetIndex[track]++;
        if (NextChunkOffsetIndex[track] == stco[track].ChunkOffsets.Length)
        {
          done[track] = true;
          OffsetValues[track] = uint.MaxValue;
        }
        else
        {
          OffsetValues[track] = stco[track].ChunkOffsets[NextChunkOffsetIndex[track]];
        }
        int chunk = SampleToChunkIndex(track, sample[track]);
        uint count = SampleCountsInChunk[track][chunk];
        SampleSizeBox stsz = tracks[track].MediaBox.MediaInformationBox.SampleTableBox.SampleSizeBox;
        for (int k = 0; k < count; k++, sample[track]++)
        {
          ProcessSample(currOffset, stsz.SampleSizeArray[sample[track]]);
          currOffset += stsz.SampleSizeArray[sample[track]];
        }
      }
    }
  }
}
