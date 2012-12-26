using System.Text;
using System.Collections;
using System.Collections.Generic;
using Media.Formats.Generic;
using Media.H264;

namespace Media.Formats.MP4
{
  using System;
  using System.Text;
  using System.IO;

  public class Fragment : GenericFragment
  {
    private uint _timeScale;
    private ulong _baseDataOffset;
    private ulong _currentOffsetInBytes;
    private readonly ulong _startTime;
    private readonly int _startIndex;
    private string _trackType;
    private int _trackID;
    private List<StreamDataBlockInfo> _listOfSampleInfo;
		private BoxReader _reader;
    private SequenceParameterSet _sps; // sequence param set
    private PictureParameterSet _pps; // picture param set

    // _currentFrame is the running count of frames (or samples, or slices) across ALL fragments in the stream.
    // It is initialized to zero here, but is initialized again in one of the constructors to be the start time of this fragment.
    private int _currentFrame = 0;

    // _currentTime is the running time (in scaled units) across ALL fragments in stream.
    // It is initialized to zero here, but is initialized again in one of the constructors to be the start time of this fragment.
    private long _currentTime = 0;

  	public string CodecPrivateData;

    public Fragment() {
      base.Length = 0;
      this.MovieFragmentBox = new MovieFragmentBox(); // moof
      this.MediaDataBox = new MediaDataBox();
    }

    /// <summary>
    /// Constructor to use when reading in a fragment.
    /// </summary>
    /// <param name="timeScale">The track timescale (assume it's the same for all fragments in the same track)</param>
    /// <param name="payloadType">"soun" or "vide"</param>
    /// <param name="fragStart">running timestamp</param>
    public Fragment(uint timeScale, string payloadType, ulong fragStart)
      : this()
    {
      _timeScale = timeScale;
      _trackType = payloadType;
      _startTime = fragStart;
      _currentTime = (long)_startTime;
    }

    public Fragment(uint timeScale, string payloadType, ulong fragStart, int firstIndex)
      : this(timeScale, payloadType, fragStart)
    {
      _startIndex = firstIndex;
      _currentFrame = _startIndex;
    }

    /// <summary>
    /// Constructor to use when assembling a Fragment from scratch.
    /// This is normally used before calling Write to write out to a fragment.
    /// </summary>
    /// <param name="sequenceNum">start at 1 and increment for every fragment</param>
    /// <param name="trackID">use 1 for sourceAudio, and 2 for sourceVideo</param>
    /// <param name="sampleCount">count of samples in this fragment</param>
    /// <param name="fragRunFlags">rawTrack fragment run flags for every run in this fragment</param>
    /// <param name="defaultSampleFlags">default flags for every sample</param>
    /// <param name="sampleSize">set to non-zero only when defaultSampleFlags has DefaultSampleSizePresent bit set</param>
    public Fragment(uint sequenceNum, uint trackID, int sampleCount, uint fragRunFlags, uint defaultSampleFlags, uint sampleSize)
    {
      base.Length = sampleCount;
      this.MovieFragmentBox = new MovieFragmentBox(sequenceNum, trackID, sampleCount, fragRunFlags, defaultSampleFlags, sampleSize);
      this.MediaDataBox = new MediaDataBox();
    }

    // IEnumerable<Slice> accessor, assumes that this fragment has been read-in.
    // index starts at _startIndex (it is the index of the slice in the track, not index in this fragment)
    public override Slice this[int index]
    {
      get
      {
        if (index < _startIndex) return null;
        if (_listOfSampleInfo == null) return null;
        StreamDataBlockInfo sampleInfo = _listOfSampleInfo[index - _startIndex];
        Slice slice = new Slice();
        StreamDataBlockInfo sliceInfo = slice as StreamDataBlockInfo;
        sliceInfo.Copy(sampleInfo);
        _reader.BaseStream.Position = (long)sampleInfo.StreamOffset;
        slice.SliceBytes = _reader.ReadBytes(sampleInfo.SliceSize); // return the actual bits from MDAT
      	slice.SliceSize = slice.SliceBytes.Length;
        return slice;
      }
      set // this can work only with ODT fragments because each fragment is its own file
      {
        // if original frag has not been read-in yet, do nothing
        if (_listOfSampleInfo == null)
          return;
        FixupBoxesAndMDAT(index, value);
      }
    }

    // TimeScale property
    public uint TimeScale
    {
      get { return _timeScale; }
      set { _timeScale = value; }
    }

    // Size property
    public uint DataOffset
    {
      get {
        if (this.MovieFragmentBox.Size >= uint.MaxValue)
          throw new InvalidBoxException(this.MovieFragmentBox.Type, (long)this.MovieFragmentBox.Offset, "Size too large");
        return ((uint)this.MovieFragmentBox.Size + 8); 
      }
    }

    public MediaDataBox MediaDataBox { get; set; }
    public MovieFragmentBox MovieFragmentBox { get; set; }

    public uint Duration
    {
      get { return this.MovieFragmentBox.TrackFragmentBox.TrackFragmentRunBox.Duration; }
    }

    //public override void Read()
    //{
    //  //base.Read();
    //  BoxReader boxReader = new BoxReader(readStream);
    //  this.Read(boxReader);
    //}

    /// <summary>
    /// Read
    /// Read just the header boxes of a fragment, not the MDAT box data bits yet.
    /// </summary>
    /// <param name="reader"></param>
    public void Read(BoxReader inBoxReader) {
			// now we can once again read everything, but using our own reader...
			this._reader = inBoxReader;
			this.MovieFragmentBox.Read(_reader);
			this.MediaDataBox.Read(_reader);
      this._baseDataOffset = this.MediaDataBox.PayloadOffset;
      this._trackID = (int)this.MovieFragmentBox.TrackFragmentBox.TrackFragmentHeaderBox.TrackId;
      base.Length = (int)this.MovieFragmentBox.TrackFragmentBox.TrackFragmentRunBox.SampleCount;
      CollectSampleInfoStream(); // pre-assemble list of sample info blocks
    }

		public void SetReader(BoxReader inBoxReader) {
			_reader = inBoxReader;
		}

    /// <summary>
    /// GetMP4TrackID
    /// Get the track ID without reading all of the fragment. Instead of calling Read above, use this if all that is needed is the TrackID.
    /// If this fragment belongs to a track that in turn belongs to a Microsoft ISM smooth stream, then this method gets the LOCAL track ID
    /// within the ISMV file.
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    public int GetMP4TrackID(BoxReader reader)
    {
      long pos = reader.BaseStream.Position;
      _trackID = (int)this.MovieFragmentBox.GetTrackID(reader);
      // we advance to the next box after this fragment
      reader.BaseStream.Position = pos + (long)this.MovieFragmentBox.Size;
      this.MediaDataBox.Read(reader);
      return _trackID;
    }

    /// <summary>
    /// Write
    /// This does not write to MDAT yet, just to header boxes.
    /// </summary>
    /// <param name="writer"></param>
    public void Write(BoxWriter writer) {
        this.MovieFragmentBox.Write(writer);
        this.MediaDataBox.Write(writer);
    }

    /// <summary>
    /// CheckFilePosition
    /// This should be called just before writing to a fragment mdat.
    /// It checks whether the base data offset matches the current output file position.
    /// </summary>
    /// <param name="writer"></param>
    public void CheckFilePosition(BoxWriter writer)
    {
      if (this.MovieFragmentBox.TrackFragmentBox.TrackFragmentHeaderBox.BaseDataOffset != (ulong)writer.BaseStream.Position)
        throw new Exception("Fragment: base data offset does not match file position");
    }


    /// <summary>
    /// GetSampleStream
    /// Assemble the "sampleStream" which is the list of sample records that point to the sample bits in mdat, and also contain duration data, etc.
    /// Traverse the TrackFragmentRunBox in this fragment to collect the samples.
    /// The Iteration time span delimited by startTime and endTime may encompass several fragments, in which case this method
    /// will only put all samples in this one fragment. This method will not fetch the next fragment because that would mean another Fragment class instance.
    /// It is up to the caller whether to create another Fragment instance and collect samples from the next fragment.
    /// FIXME: Need to optimize this so that sampleStream is assembled from listOfSampleInfo, instead of reading boxes again.
    /// </summary>
    /// <param name="sampleStream">List of sample records to be assembled.</param>
    /// <param name="timeScale">Sampling rate (samples per second).</param>
    /// <param name="trackType">Which track does this fragment belong?</param>
    /// <param name="startTime">Start of Iteration time.</param>
    /// <param name="endTime">End of Iteration time.</param>
    /// <param name="lastEnd">Previous endTime.</param>
    /// <returns></returns>
    public bool GetSampleStream(List<StreamDataBlockInfo> sampleStream, uint timeScale, string trackType, ulong startTime, ulong endTime, ref ulong lastEnd)
    {
      if (startTime > (_startTime + Duration)) return false; // requested start time is ahead of this whole fragment
      if (_timeScale != timeScale) return false;
      _currentFrame = _startIndex;
      _currentTime = (long)_startTime;

      foreach (StreamDataBlockInfo sliceData in _listOfSampleInfo)
      {
        if (sliceData.TimeStampNew >= startTime) 
        {
          if ((sliceData.TimeStampNew > endTime) && (sliceData.SliceType != SliceType.DFrame) && (sliceData.SliceType != SliceType.BFrame))
          {
            break;
          }
          _currentTime += (long)sliceData.SliceDuration;
          lastEnd = (ulong)_currentTime;
          StreamDataBlockInfo sliceCopy = new StreamDataBlockInfo();
          sliceCopy.Copy(sliceData);
          sampleStream.Add(sliceCopy);
        }
        _currentFrame++;
      }

      return (true);
    }

    public bool GetSampleStream(List<StreamDataBlockInfo> sampleStream, int inStartSampleIndex, int inEndSampleIndex, ref ulong lastEnd)
    {
      if (inStartSampleIndex >= (_startIndex + Length)) return false; // requested index is beyond this fragment
      _currentFrame = _startIndex;
      _currentTime = (long)_startTime;

      foreach (StreamDataBlockInfo sliceData in _listOfSampleInfo)
      {
        if (sliceData.index >= inStartSampleIndex)
        {
          if ((sliceData.index > inEndSampleIndex) && (sliceData.SliceType != SliceType.DFrame) && (sliceData.SliceType != SliceType.BFrame))
          {
            break;
          }
          _currentTime += (long)sliceData.SliceDuration;
          lastEnd = (ulong)_currentTime;
          StreamDataBlockInfo sliceCopy = new StreamDataBlockInfo();
          sliceCopy.Copy(sliceData);
          sampleStream.Add(sliceCopy);
        }
        _currentFrame++;
      }

      return (true);
    }

    private void CollectSampleInfoStream()
    {
      this._currentOffsetInBytes = 0UL;
      this._listOfSampleInfo = new List<StreamDataBlockInfo>();
      List<TrackFragmentRunSample> samples = MovieFragmentBox.TrackFragmentBox.TrackFragmentRunBox.Samples;
      TrackFragmentHeaderBox tfhd = MovieFragmentBox.TrackFragmentBox.TrackFragmentHeaderBox;

      if (tfhd.BaseDataOffset > 0L)
        this._baseDataOffset = tfhd.BaseDataOffset;

      StreamDataBlockInfo sampleInfo;
      while (null != (sampleInfo = GetOneSample(samples, tfhd)))
      {
        _listOfSampleInfo.Add(sampleInfo);
      }
    }

    // FIXME: need to implement this
    private void FixupBoxesAndMDAT(int index, Slice slice)
    {
    }

    /// <summary>
    /// GetOneSample
    /// Get one frame or sample from the moof structures. There is no stss in a fragment, so we need to determine
    /// whether a frame is an IFrame or not by examining the IndependentAndDisposableSamplesBox.
    /// </summary>
    /// <param name="samples"></param>
    /// <param name="tfhd"></param>
    /// <returns></returns>
    private StreamDataBlockInfo GetOneSample(List<TrackFragmentRunSample> samples, TrackFragmentHeaderBox tfhd)
    {
        if (_currentFrame == samples.Count + _startIndex)
            return null;

        uint fixedFrameSizeInBytes = tfhd.DefaultSampleSize;
        if (fixedFrameSizeInBytes == 0)
        {
            fixedFrameSizeInBytes = samples[_currentFrame - _startIndex].SampleSize;
        }

        if (fixedFrameSizeInBytes == 0) // if it's still zero, then we have a problem
            throw new Exception("Sample size zero");

        // is there enough data left to read the next frame?
        if (this._baseDataOffset + _currentOffsetInBytes + fixedFrameSizeInBytes > (ulong)_reader.BaseStream.Length) 
			  return null;

        // currently DRM is not yet supported in this GetFrame routine, unlike the FragmentedMp4ParserImplementation
        //        if ((this.m_drmIVOffsets != null) && (this.m_numDrmIVs > this.m_frameIndex))
        //        {
        //            length = this.m_drmIVSizes[this.m_frameIndex];
        //            destinationArray = new byte[length];
        //            Array.Copy(this.m_headerBuffer, this.m_drmIVOffsets[this.m_frameIndex], destinationArray, 0, length);
        //        }

        uint fixedDuration = tfhd.DefaultSampleDuration;
        if (samples[_currentFrame - _startIndex].SampleDuration != 0)
        {
            fixedDuration = samples[_currentFrame - _startIndex].SampleDuration;
        }
        if (_timeScale > 0) // time scale is 1 for ODS assets
        {
          // scale time
          fixedDuration = (uint)TimeArithmetic.ConvertToStandardUnit(_timeScale, fixedDuration);
        }

        StreamDataBlockInfo oneFrameData = new StreamDataBlockInfo();

        //RawFrameData ans = new RawFrameData(CurrentTime, currentOffsetInBytes, fixedFrameSizeInBytes, fixedDuration, destinationArray);
        oneFrameData.SliceDuration = fixedDuration;
        oneFrameData.SliceSize = (int)fixedFrameSizeInBytes;
        oneFrameData.StreamOffset = this._baseDataOffset + _currentOffsetInBytes;
        GetSliceTypeAndFrameNum(oneFrameData);

        // for ISM, TimeStampNew will always have a value
        oneFrameData.TimeStampNew = (ulong)_currentTime;
        oneFrameData.index = _currentFrame;

        _currentOffsetInBytes += fixedFrameSizeInBytes;
        _currentTime += fixedDuration;
        _currentFrame++;

        return oneFrameData;
    }

    private void GetSliceTypeAndFrameNum(StreamDataBlockInfo oneFrameInfo)
    {
      if (_trackType == null)
        throw new Exception("Fragment: track type undetermined"); ;

      switch (_trackType)
      {
        case "avc1":
        case "vc-1":
        case "mp4v":
          GetSliceTypeAndFrameNumFromH264Payload(oneFrameInfo);
          break;
        case "mp4a":
          oneFrameInfo.SliceType = SliceType.MP4A;
          break;
        case "wma ":
          oneFrameInfo.SliceType = SliceType.WMA;
          break;
        //case "avc1":
        //  return SampleType.AVC1;
        //  break;
        default:
          return;
      }
    }

    private void GetSliceTypeAndFrameNumFromH264Payload(StreamDataBlockInfo oneFrameInfo)
    {
      int count = oneFrameInfo.SliceSize;
      _reader.BaseStream.Position = (long)oneFrameInfo.StreamOffset;
      BinaryReader binReader = new BinaryReader(_reader.BaseStream);
      while (count > 4)
      {
        ulong naluLen = _reader.ReadUInt32();
        long nextPos = _reader.BaseStream.Position + (long)naluLen;

        int c = _reader.PeekChar() & 0x1F;
        NALUnitType naluType = (NALUnitType)c;

        if ((naluLen > (ulong)count) || (naluLen < 2))
          throw new Exception("Fragment: H264Payload has invalid NALU length"); ;

        BitReader bitReader = new BitReader(_reader.BaseStream);
        switch (naluType)
        {
          case NALUnitType.Unspecified:
            break;
          case NALUnitType.NonIDRSlice: // non-IDR picture
          case NALUnitType.SlicePartitionA: // slice A partition
          case NALUnitType.IDRSlice: // IDR picture
            if (oneFrameInfo.SliceType == SliceType.Unknown)
            {
              SliceHeader header = new SliceHeader(_sps, _pps, (byte)2, naluType, (uint)naluLen);
              header.Read(bitReader);
              oneFrameInfo.CTS = (ulong)header.FrameNum;
              if ((header.SliceType == SliceTypes.B) || (header.SliceType == SliceTypes.BA))
                oneFrameInfo.SliceType = SliceType.BFrame;
              else if (naluType == NALUnitType.IDRSlice)
                oneFrameInfo.SliceType = SliceType.IFrame;
              else
                oneFrameInfo.SliceType = SliceType.DFrame;
            }
            else
            {
              int z = 0; // debug point
            }
            break;
          case NALUnitType.SlicePartitionB: // slice B partition
          case NALUnitType.SlicePartitionC: // slice C partition
            uint sliceID = bitReader.DecodeUnsignedExpGolomb();
            break;
          case NALUnitType.SupplementalEnhancementInfo:
            break;
          case NALUnitType.SequenceParamSet: // SPS
            _sps = new SequenceParameterSet((uint)naluLen);
            _sps.Read(bitReader);
            break;
          case NALUnitType.PictureParamSet:
            _pps = new PictureParameterSet((uint)naluLen);
            _pps.Read(bitReader);
            break;
          case NALUnitType.AccessUnitDelimiter:
            break;
          case NALUnitType.EndOfSequence:
            break;
          case NALUnitType.EndOfStream:
            break;
          case NALUnitType.FillerData:
            break;
          default:
            break;
        }

        count -= ((int)naluLen + 4);
        _reader.BaseStream.Position = nextPos;
      }
    }


    /// <summary>
    /// AddSampleStream
    /// This is the inverse of GetSampleStream above.
    /// Given a list of samples, assemble the TrackFragmentBox for this fragment.
    /// </summary>
    /// <param name="streamLocations"></param>
    /// <param name="trackTimeScale"></param>
    /// <param name="currMdatOffset"></param>
    public void AddSampleStream(List<StreamDataBlockInfo> streamLocations, uint trackTimeScale, ref ulong currMdatOffset)
    {
      // the time scale for every ISMV video fragment is 10,000,000
      this._timeScale = trackTimeScale;

      _currentOffsetInBytes = 0UL;
      TrackFragmentBox tfrag = this.MovieFragmentBox.TrackFragmentBox;
      TrackFragmentHeaderBox tfhd = tfrag.TrackFragmentHeaderBox;

      // now we know the file offset to the data bits, so this is where we set the base data offset
      tfhd.BaseDataOffset = currMdatOffset;

      uint defaultSampleDuration = tfhd.DefaultSampleDuration;
      uint defaultSampleSize = 0; // tfhd.DefaultSampleSize; <-- don't use default sample size
      uint defaultSampleFlags = tfhd.DefaultSampleFlags;

      TrackFragmentRunBox fragRunBox = tfrag.TrackFragmentRunBox;
      IndependentAndDisposableSamplesBox stdpBox = tfrag.IndependentAndDisposableSamplesBox;

      uint trackID = tfhd.TrackId;

      uint trunFlags = fragRunBox.Flags;
      if (((trunFlags & (uint)TrackFragmentRunBoxFlags.SampleDurationPresent) == 0) != (defaultSampleDuration > 0))
        throw new Exception("Fragment: Trun flag sample duration present inconsistent with TrackFragmentHeaderBox data");
      if (((trunFlags & (uint)TrackFragmentRunBoxFlags.SampleSizePresent) == 0) != (defaultSampleSize > 0))
        throw new Exception("Fragment: Trun flag sample size present inconsistent with TrackFragmentHeaderBox data");
      if (((trunFlags & (uint)TrackFragmentRunBoxFlags.SampleFlagsPresent) == 0) != (defaultSampleFlags != 0))
        throw new Exception("Fragment: Trun flag sample flags present inconsistent with TrackFragmentHeaderBox data");

      foreach (StreamDataBlockInfo data in streamLocations)
      {
        if ((defaultSampleSize > 0) && (data.SliceSize != defaultSampleSize))
          throw new Exception("Samples do not have the same size in input stream when they're supposed to");
        fragRunBox.AddOneSample(data, trackTimeScale, defaultSampleSize, defaultSampleFlags, ref currMdatOffset);
        stdpBox.AddOneSample(data);
      }

      // after we write all the data bits, we should know the size of the mdata box
      if (currMdatOffset < tfhd.BaseDataOffset)
        throw new Exception("Fragment: fragment mdat box size calculation error");
      this.MediaDataBox.Size += (ulong)(currMdatOffset - tfhd.BaseDataOffset);
    }


    public override string ToString()
    {
        StringBuilder xml = new StringBuilder();

        xml.Append("<fragment>");
        xml.Append(MediaDataBox.ToString());
        xml.Append(MovieFragmentBox.ToString());
        xml.Append("</fragment>");

        return (xml.ToString());
    }

  }
}
