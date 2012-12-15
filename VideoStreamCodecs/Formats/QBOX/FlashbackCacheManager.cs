using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Media.Formats.Generic;

namespace Media.Formats.QBOX
{

  /// <summary>
  /// TimeAndPosition
  /// Very minimal data to keep track of time and position of every start of a block.
  /// </summary>
  public struct TimeAndPosition
  {
    public ulong cts;  // composition time (qbox marker)
    public ulong timeStamp;
    public long position; // file position or fragment index
  }

  /// <summary>
  /// FlashbackCacheManager
  /// QBox-specific implementation of PerTrackCacheManager.
  /// The whole purpose of this is to remember the start of blocks for the whole video stream.
  /// We don't want to store all qbox data in main memory: that amount of data would be overwhelming for
  /// large qbox files.
  /// </summary>
  public class FlashbackCacheManager : PerTrackCacheManager
  {

    private Dictionary<int, TimeAndPosition> TimeAndPos;

    public FlashbackCacheManager()
    {
      TimeAndPos = new Dictionary<int, TimeAndPosition>();
    }

    private void TrackFormat_FetchNextBatch(int requestedBoxCount, int sliceIndex)
    {
      if (requestedBoxCount > 0)
        stream.LazyRead(requestedBoxCount);
      else
      {
        long filePos;
        if (TimeAndPos.Keys.Contains(sliceIndex))
        {
          //track.NextIndexToRead = sliceIndex;
          filePos = TimeAndPos[sliceIndex].position;
        }
        else
        {
          int last = TimeAndPos.Keys.Last();
          // track.NextIndexToRead = last;
          filePos = TimeAndPos[last].position;
        }
        stream.Stream.Position = filePos;
        stream.LazyRead(GenericMediaStream.MAX_BOXES_TO_READ);
      }
    }

    public override void Initialize(GenericMediaTrack cachedTrack)
    {
      base.Initialize(cachedTrack);

      track.TrackFormat.FetchNextBatch -= base.GetMoreBoxes;
      track.TrackFormat.FetchNextBatch += new LazyRead(TrackFormat_FetchNextBatch); // replace

      // put initial blocks in dictionary
      for (int i = 0; i < CacheBufCount; i++)
      {
        if (cache[i].SampleStreamLocations.Count > 0)
          AddToDictionary(cache[i]);
      }
    }


    public override int PrepareSampleInfo(int sliceIndex)
    {
      int retVal = -1;
      int firstIndexInBlock;

      if (LookForCachedBlock(sliceIndex))
      {
        retVal = base.PrepareSampleInfo(sliceIndex);
      }
      else if ((firstIndexInBlock = LookForBox(sliceIndex)) > 0)
      {
        int lastSeen = TimeAndPos.Keys.Last();
        if ((lastSeen >= firstIndexInBlock) && !TimeAndPos.Keys.Contains(firstIndexInBlock))
          throw new Exception("FlashbackCacheManager.PrepareSampleInfo : inconsistent state");
        if (lastSeen < sliceIndex)
          retVal = MoveForwardBeyondLast(sliceIndex);
        else
          retVal = JumpToStreamPosition(sliceIndex);
      }
      else if ((TimeAndPos.Keys.Last() + BlockSize) > sliceIndex)
      {
        retVal = JumpToStreamPosition(sliceIndex);
      }
      else // new box to be read
      {
        retVal = MoveForwardBeyondLast(sliceIndex);
      }

      return retVal;
    }

    void AddToDictionary(Cache c)
    {
      TimeAndPosition tap = new TimeAndPosition();
      tap.timeStamp = c.CacheStart;
      tap.position = (long)c.SampleStreamLocations[0].StreamOffset;
      tap.cts = c.SampleStreamLocations[0].CTS;
      TimeAndPos.Add(c.StartIndex, tap);
    }

    // CacheAndPutInDictionary
    // param must be index of an IFrame or key frame
    private int CacheAndPutInDictionary(int index)
    {
      if (base.PrepareSampleInfo(index) < 0)
        return -1;
      Cache c = cache[readCache];
      if (index != c.StartIndex)
      {
        if (LookForCachedBlock(index))
          throw new Exception("FlashbackCacheManager.CacheAndPutInDictionary new slice index is not start of new block");
        return -1;
      }
      if (TimeAndPos.ContainsKey(c.StartIndex))
        return 0; // this is not an exception (it maybe that the user is coming back to the present from a flashback)
      AddToDictionary(c);
      return 0;
    }


    // LookForBox : the index we're looking for may already be in a box, but not cached yet (don't read from stream)
    // this case covers that in which another cache manager has already read the relevant boxes
    private int LookForBox(int index)
    {
      return format.SampleAvailable(index);
    }

    // All we know is that the user wants to move forward beyond all caches.
    // We dont' know where the stream position is. It can be way back, or
    // just ahead of the forward-most cache.
    private int MoveForwardBeyondLast(int sliceIndex)
    {
      int nextBlockStart = TimeAndPos.Keys.Last();
      long lastBlockPos = TimeAndPos[nextBlockStart].position;
      // Make sure that stream is at the right position.
      // If it is ahead of the most forward block, we can assume
      // that it is in the right position.
      if (stream.Stream.Position < lastBlockPos)
        Warp(nextBlockStart);
      // Now the stream is in posiition, either at the block just
      // before the desired qbox, or ahead of that but still behind the desired qbox itself.
      while (nextBlockStart < sliceIndex)
      {
        if (CacheAndPutInDictionary(nextBlockStart) < 0)
          return -1;
        nextBlockStart = cache[readCache].EndIndex + 1;
      }
      return base.PrepareSampleInfo(sliceIndex);
    }

    private int JumpToStreamPosition(int index)
    {
		 //if (index == 0) // DTT, Attempted fix for index 0.
			 //return TimeAndPos.Keys.First();

      int firstSliceInPriorBlock = TimeAndPos.Keys.Last(k => (k <= (index - BlockSize)));
      int firstSliceIndexInBlock = TimeAndPos.Keys.Last(k => (k <= index));
      long pos1 = TimeAndPos[firstSliceInPriorBlock].position;
      long pos2 = TimeAndPos[firstSliceIndexInBlock].position;
      if ((stream.Stream.Position > pos1) && (stream.Stream.Position <= pos2))
        return base.PrepareSampleInfo(firstSliceIndexInBlock);
      return Warp(firstSliceIndexInBlock);
    }

    private int Warp(int firstSliceIndexInBlock)
    {
      TimeAndPosition timPos = TimeAndPos[firstSliceIndexInBlock];
      if (stream.IsMediaStreamFragmented)
      {
        // FIXME: fragmented case undefined
        //GenericFragment frag = track.Fragments[(int)timPos.position]; // use "position" as fragment index
        //Slice slice = frag[firstSliceIndexInBlock - frag.FirstIndex];
        return 0;
      }
      else
      {
        stream.SynchronizeAllTracks(timPos.position, timPos.timeStamp);
        // LazyRead from this position
        stream.LazyRead(GenericMediaStream.MAX_BOXES_TO_READ);
        // finally, read-in as many blocks as caching allows
        base.PrepareSampleInfo(firstSliceIndexInBlock);
        return 0; // this[iFrameIndex];
      }
    }

    public int GetNearestKey(int index, out long pos)
    {
      int nearestKey = TimeAndPos.Keys.Last(k => (k <= index));
      pos = TimeAndPos[nearestKey].position;
      return nearestKey;
    }
  }
}
