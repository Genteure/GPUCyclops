using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Media.Formats.Generic
{
  /// <summary>
  /// Cache Manager
  /// by: CCT
  /// 
  /// The CacheManager and PerTrackCacheManager classes allow the user to access slices in a track in a truly
  /// enumerable manner. This basically implements the iterator for accessing a particular slice in a track (eg track[i]).
  /// When these classes are NOT used (GenericMediaStream.CachingEnabled = false), track[i] can only access a slice 
  /// within a batch, and i is limited to the indices of slices existing in that batch. 
  /// When used (GenericMediaStream.CachingEnabled is set to true), i covers ALL indices in a track.
  /// 
  /// We use the existing Read/Write operations in GenericMediaStream to do the reads and writes.
  /// An instance of this CacheManager class is created only when CachingEnabled is set to true in GenericMediaStream.
  /// CachingEnabled is set either in stream.Open (for reading)  or in stream.Create (for writing).
  /// Note 1: it is not media data itself that is cached, but rather just the sample (slice) data infos.
  /// Note 2: All times are in units of 100 Nanosec. Otherwise, variable naming should indicate what time scale
  /// a time variable is in.
  /// </summary>
  public class CacheManager
  {
    readonly GenericMediaStream stream;
    List<IsochronousTrackInfo> trackDefs;

    public CacheManager(GenericMediaStream cachedStream)
    {
      stream = cachedStream;
      if (!stream.IsForReading)
        return;
      stream.Stream.Position = 0L;
      //stream.Read(); // first read (cache all header data in the file)
      stream.LazyRead(GenericMediaStream.MAX_BOXES_TO_READ);
      foreach (GenericMediaTrack track in stream.MediaTracks)
      {
        track.CacheMgr = GenericCacheManager.CreateCacheManager(); // the cache manager maybe format-specific
        track.CacheMgr.Initialize(track);
      }
    }

    public void SetupForWrite(List<IsochronousTrackInfo> sourceTrackDefs)
    {
      this.trackDefs = sourceTrackDefs; // unused for now
      foreach (GenericMediaTrack track in stream.MediaTracks)
      {
        track.CacheMgr = new PerTrackCacheManager();
        track.CacheMgr.Initialize(track);
        track.CacheMgr.CurrentSliceList = new List<StreamDataBlockInfo>();
      }
    }
  }

  public class Cache
  {
    public Cache()
    {
      SampleStreamLocations = new List<StreamDataBlockInfo>();
    }

    public List<StreamDataBlockInfo> SampleStreamLocations;
    public int Relevancy = 1200; // used for determining which cache buffer to get rid of

    public int StartIndex
    {
      get
      {
        if (SampleStreamLocations.Count == 0)
          return -1;
        return SampleStreamLocations[0].index;
      }
    }

    public int EndIndex
    {
      get
      {
        if (SampleStreamLocations.Count == 0)
          //throw new Exception("Cache is empty");
          return 0;
        return SampleStreamLocations[SampleStreamLocations.Count - 1].index;
      }
    }

    /// <summary>
    /// The time, in units of 100 NanoSecs, that the first slice in a cache should be presented.
    /// </summary>
    public ulong CacheStart
    {
      get 
      {
				//throw new Exception("Cache is empty");
				if (SampleStreamLocations.Count == 0) return 0UL;
          
        return SampleStreamLocations[0].TimeStampNew.Value; 
      }
    }

    /// <summary>
    /// CacheEnd - the time, in units of 100 NanoSecs, that a cache ends
    /// </summary>
    public ulong CacheEnd
    {
      get 
      {
        if (SampleStreamLocations.Count == 0)
          //throw new Exception("Cache is empty");
          return 0UL;

      	int x = 1;
				while (SampleStreamLocations.Count - x > 0) {
					StreamDataBlockInfo last = SampleStreamLocations[SampleStreamLocations.Count - x];
					if (last.TimeStampNew.HasValue) 
					  return (last.TimeStampNew.Value + (ulong) (last.SliceDuration/2)); // divide slice duration by 2
					x++;
				}
      	return 0UL;
      }
    }
  }


  public class PerTrackCacheManager
  {
    protected IMediaTrack track;
    protected ITrackFormat format;
    protected IMediaStream stream;
    protected Cache[] cache = new Cache[CacheBufCount];
    protected int readCache = 0;
    int writeCache = 0;
    int previousRequest = -1;

    bool isEndOfTrack // end of track
    {
      get
      {
        Cache c = cache[readCache];
        return ((c.SampleStreamLocations == null) || (c.SampleStreamLocations.Count == 0));
      }
    }

//nbl		ulong _readExtent = 90000000; // default is 9 seconds
		ulong _readExtent = 20000000; // default is 2 seconds
		public ulong ReadAheadExtent
    {
      get { return _readExtent; }
      set { _readExtent = value; }
    }

    protected const int CacheBufCount = 4;

    public PerTrackCacheManager()
    {
      for (int i = 0; i < CacheBufCount; i++)
      {
        cache[i] = new Cache();
      }
    }

    public void GetMoreBoxes(int requestedBoxCount, int sliceIndex)
    {
      stream.LazyRead(requestedBoxCount);
    }

    public virtual void Initialize(GenericMediaTrack cachedTrack)
    {
      track = cachedTrack;
      format = track.TrackFormat;
      stream = track.ParentStream;

      if (stream.IsForReading)
      {
        track.BlockWithSlice += new NextBlock(track_BlockWithSlice);
        track.TrackFormat.FetchNextBatch += new LazyRead(GetMoreBoxes);

        if ((writeCache == 0) && (readCache == 0))
          PrepareSampleInfo(0UL); // fill up the cache with first four blocks
      }
      else
      {
        track.PrepareMediaHeaders += new SlicePutRequest(track_PrepareMediaHeaders);
      }
    }

    public List<StreamDataBlockInfo> CurrentSliceList
    {
      get { return cache[readCache].SampleStreamLocations; }
      set
      {
        cache[writeCache] = new Cache();
        cache[writeCache].SampleStreamLocations = value;
      }
    }

    // BlockSize is the count of slices in a block
    public int BlockSize
    {
      get;
      private set;
    }

    /// <summary>
    /// If access is sequential, current cache buf must give up its score in favor
    /// of bufs whose range of indices is higher than that of current buf.
    /// If access is random, current cache must increment its score at the expense of
    /// the rest of the bufs.
    /// </summary>
    /// <param name="sliceIndex"></param>
    void UpdateRelevancyScores(int sliceIndex)
    {
      bool sequentialAccess = false;
      if ((sliceIndex - previousRequest) == 1)
        sequentialAccess = true; // if not sequential, it is random

      UpdateScores(sequentialAccess);

      previousRequest = sliceIndex;
    }

    /// <summary>
    /// UpdateRelevancyScores (with ulong time parameter)
    /// When this method is called, we have already found the requested slice, and
    /// readCache is now set to the correct buf.
    /// </summary>
    /// <param name="sliceTime"></param>
    void UpdateRelevancyScores(ulong sliceTime)
    {
      if (sliceTime == 0UL)
      {
        UpdateRelevancyScores(0);
        return;
      }

      // first, look for the slice in order to get its index
      StreamDataBlockInfo sliceInfo = CurrentSliceList[0]; // get any slice
      uint halfOfDuration = (uint)(sliceInfo.SliceDuration / 2); // shift to the right by one
      sliceInfo = CurrentSliceList.First(
				s => s.TimeStampNew.HasValue && ((s.TimeStampNew.Value > sliceTime - halfOfDuration) && (s.TimeStampNew.Value < sliceTime + halfOfDuration)));

      UpdateRelevancyScores(sliceInfo.index);
    }

    List<Cache> GetSortList()
    {
      List<Cache> sortList = new List<Cache>(4);
      for (int i = 0; i < CacheBufCount; i++)
      {
        if ((cache[i].SampleStreamLocations != null) && (cache[i].SampleStreamLocations.Count > 0))
          sortList.Add(cache[i]);
      }
      return sortList;
    }

    void UpdateScores(bool sequentialAccess)
    {
      List<Cache> sortList = GetSortList();

      if (sortList.Count == 1)
      {
        Cache c = sortList[0];
        c.Relevancy++;
        return;
      }

      sortList.Sort(delegate(Cache a, Cache b) { return a.StartIndex - b.StartIndex; });

      bool found = false;
      for (int i = 0; i < sortList.Count; i++)
      {
        Cache c = sortList[i];
        if (i == readCache)
        {
          found = true;
          c.Relevancy = c.Relevancy + (sequentialAccess ? -1 : +1);
        }
        else if (sequentialAccess)
          c.Relevancy = c.Relevancy + (found ? +1 : -1);
        else // random access
          c.Relevancy--;
      }
    }

    void PrepareSlices(ulong time)
    {
      cache[writeCache].SampleStreamLocations.Clear();
      cache[writeCache].Relevancy = 1200;
      if (track.PrepareSampleReading(time, time + ReadAheadExtent))
      {
#if BLOCK_START_CHECK
        if (cache[writeCache].SampleStreamLocations[0].SliceType == SliceType.DFrame)
          throw new Exception("Internal error: cache starts with a DFrame");
#endif
        if (BlockSize == 0)
        {
          CalculateBlockSizeFromInitialSlices();
        }
      }
      else
      {
        // end of file?
        cache[writeCache].Relevancy = -1200;
      }
    }


    /// <summary>
    /// PrepareSlices
    /// Clear out current write cache, and then attempt to fill it.
    /// </summary>
    /// <param name="index">int index of slice of interest</param>
    void PrepareSlices(int index)
    {
      if (cache[writeCache].SampleStreamLocations == null)
        cache[writeCache].SampleStreamLocations = new List<StreamDataBlockInfo>();
      cache[writeCache].SampleStreamLocations.Clear();
      if (BlockSize == 0)
      {
        // just use any block size for now
        if (track.PrepareSampleReading(index, index + 8))
        {
          CalculateBlockSizeFromInitialSlices();
          int remaining = BlockSize - 8;
          int remainingStart = index + 8;
          track.PrepareSampleReading(remainingStart, remainingStart + remaining);
#if BLOCK_START_CHECK
          if (cache[writeCache].SampleStreamLocations[0].SliceType == SliceType.DFrame)
            throw new Exception("Internal error: cache starts with a DFrame");
#endif
        }
      }
      else
      {
        track.PrepareSampleReading(index, index + BlockSize);
#if BLOCK_START_CHECK
        if (cache[writeCache].SampleStreamLocations[0].SliceType == SliceType.DFrame)
          throw new Exception("Internal error: cache starts with a DFrame");
#endif
      }
    }

    /// <summary>
    /// ReadAhead - ensure that all cache buffers are full.
    /// The cached blocks must always contain the slice being requested.
    /// If slice requested is too far away from any of the cached blocks,
    /// then refill all cached blocks.
    /// </summary>
    /// <param name="sliceTime">Current Requested Slice</param>
    void ReadAhead(ulong sliceTime)
    {
      // fill all cache buffers
      ulong currTime = sliceTime;
      readCache = writeCache = 0;
      for (int i = 0; i < CacheBufCount; i++)
      {
        PrepareSlices(currTime);
        Cache c = cache[writeCache];
        if ((c.SampleStreamLocations == null) || (c.SampleStreamLocations.Count == 0))
          break;
        //ulong timeLen = c.CacheEnd - c.CacheStart;
        //if (timeLen == 0)
        //  break;
        currTime = c.CacheEnd;
        writeCache = (writeCache + 1) % CacheBufCount;
      }
      CalculateBlockSizeFromInitialSlices();
    }

    void ReadAhead(int index)
    {
      // fill all cache buffers
      int currIndex = index;
      readCache = writeCache = 0;
      for (int i = 0; i < CacheBufCount; i++)
      {
        PrepareSlices(currIndex);
        Cache c = cache[writeCache];
        if ((c.SampleStreamLocations == null) || (c.SampleStreamLocations.Count == 0))
          break;
        currIndex = c.StartIndex + c.SampleStreamLocations.Count; // point to next block
        writeCache = (writeCache + 1) % CacheBufCount;
      }
      CalculateBlockSizeFromInitialSlices();
    }

    bool LookForCachedBlock(ulong sliceTime)
    {
      for (int i = 0; i < CacheBufCount; i++)
      {
        readCache = (readCache + 1) % CacheBufCount; // most probably in next block
        Cache c = cache[readCache];
        if (isEndOfTrack)
        {
          continue;
        }
        if ((sliceTime >= c.CacheStart) && (sliceTime <= c.CacheEnd))
          return true;
      }
      return false;
    }

    protected bool LookForCachedBlock(int index)
    {
      for (int i = 0; i < CacheBufCount; i++)
      {
        readCache = (readCache + 1) % CacheBufCount; // most probably in next block
        Cache c = cache[readCache];
        if (isEndOfTrack)
        {
          continue;
        }
        if ((index >= c.StartIndex) && (index <= c.EndIndex))
          return true;
      }
      return false;
    }

    void SetStaleCache()
    {
      // first, determine which buf to replace based on relevancy scores and any clash with readCache
      List<Cache> sortedList = GetSortList();

      // if not all cache bufs are being used, set writeCache to one that is not being used
      if (sortedList.Count < CacheBufCount)
      {
        for (int i = 0; i < CacheBufCount; i++ )
        {
          Cache c = cache[i];
          if ((c.SampleStreamLocations == null) || (c.SampleStreamLocations.Count == 0))
          {
            writeCache = i;
            return;
          }
        }
      }

      sortedList.Sort(delegate(Cache x, Cache y) { return x.Relevancy - y.Relevancy; });
      Cache cs = sortedList.First(); // pick the one with the lowest score (first one in list)
      // determine index of chosen cache buf
      for (int i = 0; i < cache.Length; i++)
      {
        if (cs.StartIndex == cache[i].StartIndex)
        {
          writeCache = i;
          break;
        }
      }
    }

    void ReplaceStaleCacheBuf(int sliceIndex)
    {
      SetStaleCache();
      PrepareSlices(sliceIndex);
    }

    void ReplaceStaleCacheBuf(ulong sliceTimeStamp)
    {
      SetStaleCache();
      PrepareSlices(sliceTimeStamp);
    }

    bool IsAtBoundary()
    {
      return cache.Any(c => c.EndIndex == previousRequest);
    }

    int AverageIFrameCountPerBlockTimesTwo()
    {
      SliceType typeOfAnySlice = cache[readCache].SampleStreamLocations[0].SliceType;
      if (typeOfAnySlice != SliceType.IFrame && typeOfAnySlice != SliceType.DFrame)
        return 0;

      int iFrameCount = 0;
      for (int i = 0; i < CacheBufCount; i++)
      {
        iFrameCount += cache[i].SampleStreamLocations.Count(s => s.SliceType == SliceType.IFrame);
      }
      return (iFrameCount << 1) / CacheBufCount;
    }

    int checkCount = 0;
    /// <summary>
    /// CheckAndReplaceStaleCacheBuf
    /// First, determine whether it's time to replace a cache buffer.
    /// Second, determine which slice to load next.
    /// Third, replace stale buffer with that that starts with next slice.
    /// NOTE: This is NOT used at the moment.
    /// </summary>
    void CheckAndReplaceStaleCacheBuf()
    {
      checkCount++;
      if (IsAtBoundary() || checkCount > AverageIFrameCountPerBlockTimesTwo())
      {
        checkCount = 0;
        int lastIndex = cache.Max(c => c.EndIndex);
        ReplaceStaleCacheBuf(lastIndex + 1);
      }
    }

    ulong EndTime = ulong.MaxValue;
    public int PrepareSampleInfo(ulong requestedTimeStamp)
    {
      if (requestedTimeStamp >= EndTime)
        return -1;

      if ((requestedTimeStamp == 0UL) && ((CurrentSliceList == null) || (CurrentSliceList.Count == 0)))
      {
        ReadAhead(requestedTimeStamp);
      }

      Cache c = cache[readCache];
      if ((requestedTimeStamp < c.CacheStart) || (requestedTimeStamp > c.CacheEnd))
      {
        if (LookForCachedBlock(requestedTimeStamp)) // if requested slice is in any of cached blocks, just read ahead by one more
          UpdateRelevancyScores(requestedTimeStamp);
        else
        {
          // replace least relevant buf with slices needed
          ReplaceStaleCacheBuf(requestedTimeStamp);
          if (!LookForCachedBlock(requestedTimeStamp))
          {
            EndTime = requestedTimeStamp;
            return -1;
          }
        }
      }
      else
        UpdateRelevancyScores(requestedTimeStamp);

      return 0;
    }

    int EndIndex = int.MaxValue;
    public virtual int PrepareSampleInfo(int index)
    {
      if (index >= EndIndex)
        return -1;

      if ((index == 0) && ((CurrentSliceList == null) || (CurrentSliceList.Count == 0)))
      {
        ReadAhead(index); // even if cache has been initialized, do it again
      }

      Cache c = cache[readCache];
      if ((index < c.StartIndex) || (index > c.EndIndex))
      {
        if (LookForCachedBlock(index)) // if requested slice is in any of cached blocks, just read ahead by one more
          UpdateRelevancyScores(index);
        else
        {
          // replace least relevant buf with slices needed
          ReplaceStaleCacheBuf(index);
          if (!LookForCachedBlock(index)) // if desired index is still not anywhere to be found, return -1
          {
            EndIndex = index;
            return -1;
          }
        }
      }
      else
        UpdateRelevancyScores(index);

      return 0; // return value is now unused
    }

    void CalculateBlockSizeFromInitialSlices()
    {
      Cache c = cache[readCache];
      ulong timeLen = c.CacheEnd - c.CacheStart;
      int num = 1;
      if ((format.PayloadType == "AAC") && (c.SampleStreamLocations.Count == 8))
        num = 2;

      uint averageSliceDuration = (uint)(timeLen / (uint)(c.SampleStreamLocations.Count - num));
		  BlockSize = (int)(ReadAheadExtent / averageSliceDuration);

      // CCT: There is definitely something wrong if BlockSize is zero at this point.
      // Instead of changing the code below, please investigate what is wrong.
      if (BlockSize == 0)
      {
        throw new Exception("Blocksize cannot be zero");
      }
      else if (BlockSize == 8) // modify if 8 because 8 is special
        BlockSize = 9;
    }

    int track_BlockWithSlice(int sliceIndex)
    {
      int retVal = PrepareSampleInfo(sliceIndex);
      return retVal;
    }

    int track_PrepareMediaHeaders(int sliceIndex)
    {
      StreamDataBlockInfo lastData = null;

      if (sliceIndex == 0)
      {
        // hypothesize a BlockSize (we don't know it at this point)
        BlockSize = 8;
        return 0;
      }
      else if (CurrentSliceList.Count >= BlockSize)
      {
        if (BlockSize == 8)
        {
          CalculateBlockSizeFromInitialSlices();
          if (BlockSize > 8) // if less than or equal to 8, process it
            return 0;
        }
        int i = CurrentSliceList.Count - 1;
        if (CurrentSliceList[i].SliceType == SliceType.IFrame)
        {
          lastData = CurrentSliceList[i];
          CurrentSliceList.RemoveAt(i);
        }
        else if (CurrentSliceList[i].SliceType == SliceType.DFrame)
        {
          return 0; // extend current block until next IFrame is found
        }
      }
      else return 0; // don't set lists (see below) for any other values of sliceIndex

      ulong localCurrMDatOffset = track.ParentStream.CurrMDatOffset;
      track.TrackFormat.PrepareSampleWriting(CurrentSliceList, ref localCurrMDatOffset);
      track.ParentStream.CurrMDatOffset = localCurrMDatOffset;
      track.ParentStream.WriteSamples(CurrentSliceList.Cast<Slice>(), track.Codec.CodecType); // second param is ineffective (unnecessary)

      // when writing to destination file, we only need and use one cache buffer
      cache[readCache].SampleStreamLocations = new List<StreamDataBlockInfo>();
      // last IFrame should be part of next block
      if (lastData != null)
      {
        CurrentSliceList.Add(lastData);
        return sliceIndex;
      }

      return sliceIndex + 1; // we only get here if sliceIndex == 0 or slice is not video
    }
  }
}
