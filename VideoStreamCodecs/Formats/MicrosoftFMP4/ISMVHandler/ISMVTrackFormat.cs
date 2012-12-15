using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Media.Formats.Generic;
using Media.Formats.MP4;

namespace Media.Formats.MicrosoftFMP4 {
  public class ISMVTrackFormat : GenericTrackFormat {
    // declare fragment sequence number
    private uint fragSequenceNum;

    public Fragment CurrentFragment; // some track formats may contain fragments (ISMV)
    public ulong FragmentDuration { get; private set; }
    public string strFileName { get; set; }
    public string strDir { get; set; }
    public FileTypeBox ftb = new FileTypeBox();
    public MovieMetadataBox mmb = new MovieMetadataBox();

    public BoxReader boxReader { get; set; }
    public long tmpPosition = 0;
    public int tmpIteration = 0;
    public int bigTmpIteration = 0;

    public List<MediaDataBox> MediaDataBoxList = new List<MediaDataBox>();

    private bool audiovideoScanCompleted = false;
    private long nextVideoFragPosition = 0;
    private long nextAudioFragPosition = 0;
    private int audioTrackID;
    private int videoTrackID;
    private string _handlerType;
    private ISMElement _ismElement; // holds a lot of info about this track format

    public override Codec Codec
    {
      get { return _ismElement.Codec; }
      set { _ismElement.Codec = value; }
    }

    public override uint TrackID
    {
      get { return (uint)_ismElement.TrackID; }
      set { _ismElement.TrackID = (int)value; }
    }

    public override uint TimeScale
    {
      get { return _ismElement.TimeScale; }
      set { _ismElement.TimeScale = value; }
    }

    public override string PayloadType
    {
      get { return _ismElement.FourCC; }
    }

    public override ulong DurationIn100NanoSecs
    {
      get;
      set;
    }

    // properties specific to video

    private Size _frameSize;
    public override Size FrameSize
    {
      get { return _frameSize; }
    }

    // properties specific to audio

    private int _channelCount;
    public override int ChannelCount { get { return _channelCount; } }

    private int _samplesize;
    public override int SampleSize { get { return _samplesize; } }

    private int _sampleRate;
    public override int SampleRate { get { return _sampleRate; } }


    /// <summary>
    /// Default constructor
    /// </summary>
    public ISMVTrackFormat() {
      this.CurrentFragment = null;
      fragSequenceNum = 1;
    }

    /// <summary>
    /// Constructor to use when reading.
    /// </summary>
    /// <param name="inDir">Folder path</param>
    /// <param name="inFileName">ISMV file name</param>
    public ISMVTrackFormat(string inDir, string inFileName)
    {
      strDir = inDir;
      strFileName = inFileName;
      boxReader = new BoxReader(File.Open(Path.Combine(inDir, inFileName), FileMode.Open, FileAccess.Read, FileShare.Read));

      ftb.Read(boxReader);
      mmb.Read(boxReader);
      //BoxType nextType = boxReader.PeekNextBoxType();
      //ReadMP4Headers();  // just read the rest?
      _frameSize = new Size();
    }

    public ISMVTrackFormat(string inDir, string inFileName, ISMElement element)
      : this(inDir, inFileName)
    {
      _ismElement = element;
      if (element.FragmentType == FragmentType.Video)
      {
        _handlerType = "vide";
        FrameSize.Height = element.Height;
        FrameSize.Width = element.Width;
      }
      else if (element.FragmentType == FragmentType.Audio)
      {
        _handlerType = "soun";
        _channelCount = _ismElement.ChannelCount;
        _samplesize = _ismElement.SampleSize;
        _sampleRate = _ismElement.SampleRate;
      }
      DurationIn100NanoSecs = (ulong)_ismElement.FragmentDurations.Sum((uint u) => { return (long)u; });
    }

    /// <summary>
    /// GetFragmentHandlerType - local private method
    /// </summary>
    /// <param name="ismvTrackID">Track ID in this ISMV file</param>
    /// <returns></returns>
    string GetFragmentHandlerType(int ismvTrackID)
    {
      foreach (TrackBox trakBx in mmb.TrackBoxes)
      {
        if (ismvTrackID == trakBx.TrackHeaderBox.TrackID)
        {
          return (trakBx.MediaBox.HandlerReferenceBox.HandlerType);
        }
      }
      throw new Exception(string.Format("Invalid trakBx ID for fragment: {0}", ismvTrackID));
    }

    uint GetTimeScale(int ismvTrackID)
    {
      foreach (TrackBox trakBx in mmb.TrackBoxes)
      {
        if (ismvTrackID == trakBx.TrackHeaderBox.TrackID)
        {
          return (trakBx.MediaBox.MediaHeaderBox.TimeScale);
        }
      }
      throw new Exception(string.Format("Invalid trakBx ID for fragment: {0}", ismvTrackID));
    }

    string GetPayloadType(int ismvTrackID)
    {
      foreach (TrackBox trakBx in mmb.TrackBoxes)
      {
        if (ismvTrackID == trakBx.TrackHeaderBox.TrackID)
        {
          return (trakBx.PayloadType);
        }
      }
      throw new Exception(string.Format("Invalid trakBx ID for fragment: {0}", ismvTrackID));
    }

    // assumption: this can ONLY be called when we know the next box is a fragment...
    private void ScanForAudioOrVideo()
    {
      long pos = boxReader.BaseStream.Position;
      do
      {
        long fragPos = boxReader.BaseStream.Position;
        Fragment frag = new Fragment();

        int trakID = (int)frag.GetMP4TrackID(boxReader);
        if (GetFragmentHandlerType(trakID) == _handlerType)
        {
          if (_handlerType.Equals("soun"))
          {
            nextAudioFragPosition = fragPos;
            audioTrackID = trakID;
          }
          else if (_handlerType.Equals("vide"))
          {
            nextVideoFragPosition = fragPos;
            videoTrackID = trakID;
          }
          break;
        }
      } while (this.boxReader.PeekNextBoxType() == BoxTypes.MovieFragment);
      boxReader.BaseStream.Position = pos;
      audiovideoScanCompleted = true;
    }

    private ulong runningTimeIn100NanoSecs = 0UL;
    private int runningSliceIndex = 0;

    private Fragment GetNextVideoFrag()
    {
      if (nextVideoFragPosition < 0) return (null); // there are no more!!
      boxReader.BaseStream.Position = nextVideoFragPosition;

      // we know where at least the next frag is as we prepared this prior to the call of this function...
      Fragment answer = new Fragment(GetTimeScale(videoTrackID), GetPayloadType(videoTrackID), runningTimeIn100NanoSecs, runningSliceIndex);
      answer.Read(boxReader);
      runningTimeIn100NanoSecs += (ulong)TimeArithmetic.ConvertToStandardUnit(answer.TimeScale, (decimal)answer.Duration);
      runningSliceIndex += answer.Length;

      nextVideoFragPosition = -1;
      while (this.boxReader.PeekNextBoxType() == BoxTypes.MovieFragment)
      {
        long fragPos = boxReader.BaseStream.Position;
        Fragment tmp = new Fragment();

        int trakID = (int)tmp.GetMP4TrackID(boxReader);
        if (GetFragmentHandlerType(trakID) == "vide")
        {
          nextVideoFragPosition = fragPos;
          break;
        }
      }

      return (answer);
    }

    private Fragment GetNextAudioFrag()
    {
      if (nextAudioFragPosition < 0) return (null); // there are no more!!
      boxReader.BaseStream.Position = nextAudioFragPosition;

      // we know where at least the next frag is as we prepared this prior to the call of this function...
      Fragment answer = new Fragment(GetTimeScale(audioTrackID), GetPayloadType(audioTrackID), runningTimeIn100NanoSecs, runningSliceIndex);
      answer.Read(boxReader);
      runningTimeIn100NanoSecs += (ulong)TimeArithmetic.ConvertToStandardUnit(answer.TimeScale, (decimal)answer.Duration);
      runningSliceIndex += answer.Length;

      nextAudioFragPosition = -1;
      while (this.boxReader.PeekNextBoxType() == BoxTypes.MovieFragment)
      {
        long fragPos = boxReader.BaseStream.Position;
        Fragment tmp = new Fragment();

        int trakID = (int)tmp.GetMP4TrackID(boxReader);
        if (GetFragmentHandlerType(trakID) == "soun")
        {
          nextAudioFragPosition = fragPos;
          break;
        }
      }

      return (answer);
    }

    void ReadMP4Headers()
    {
      BoxType boxType;

      while (this.boxReader.BaseStream.Position < this.boxReader.BaseStream.Length)
      {

        boxType = this.boxReader.PeekNextBoxType();
        if (boxType == BoxTypes.Free)
        {
          FreeBox freeb = new FreeBox();
          freeb.Read(this.boxReader);
          //FreeBoxList.Add(freeb);
        }
        else if (boxType == BoxTypes.MediaData) // mdat
        {
          MediaDataBox mdb = new MediaDataBox();
          mdb.Read(this.boxReader);  // this doesn't really read all of mdat: payload is skipped
          MediaDataBoxList.Add(mdb);
        }
        else if (boxType == BoxTypes.MovieFragmentRandomAccess)
        {
          MovieFragmentRandomAccessBox mfrab = new MovieFragmentRandomAccessBox();
          mfrab.Read(this.boxReader);
        }
        else if (boxType == BoxTypes.Free)
        {
          FreeBox freeBox = new FreeBox();
          freeBox.Read(this.boxReader);
        }
        else
        {
          // invalid box, just stop reading
          break;
          //Box box2 = new Box(boxType);
          //box2.Read(this.boxReader);
          //FreeBoxList.Add(box2);
          //Debug.WriteLine(string.Format("Unknown BoxType: {0}", box2.Type.ToString()));
        }
      } // end of while
    }


    /// <summary>
    /// Read
    /// Everytime we get here and base.Read() is called, a new fragment is created and becomes CurrentFragment of MP4TrackFormat.
    /// For a fragmented file, the first 'read' will really just scan for if there is sourceAudio and/or sourceVideo
    /// and set positions for the start of each type of fragment, prepping for another read to actually find the
    /// sourceAudio and sourceVideo fragment.
    /// </summary>
    public void Read()
    {
      if (!audiovideoScanCompleted)
      {
        ReadMP4Headers();

        if (mmb == null) throw new Exception("Moov box must come before moof");
        ScanForAudioOrVideo();
      }

      // read only one fragment at a time into each trakBx
      {
        // this means we have already done prep work, etc. and now all we care about are fragments in 
        // non-linear order...
        bool moreFragments = false;

        CurrentFragment = null; // we're done with the current fragment, get rid of it

        foreach (TrackBox trakBx in mmb.TrackBoxes)
        {
          string handler = trakBx.MediaBox.HandlerReferenceBox.HandlerType;
          if ((handler != null) && (handler.Equals(_handlerType)))
          {
            if (handler == "soun")
            {
              CurrentFragment = GetNextAudioFrag();
            }
            else if (handler == "vide")
            {
              CurrentFragment = GetNextVideoFrag();
            }

            moreFragments |= (CurrentFragment != null);

            ulong oneSecondTicks = (ulong)TimeSpan.FromSeconds(1.0).Ticks;
            if (CurrentFragment != null)
              FragmentDuration = (ulong)((CurrentFragment.Duration * oneSecondTicks) / trakBx.MediaBox.MediaHeaderBox.TimeScale);
          }
        }

        //if (!moreFragments)
        //  base.EOF = true; // end of file
      }
    }


    /// <summary>
    /// PrepareSampleReading
    /// Unlike MP4TrackFormat, ISMVTrackFormat deals with fragments. So here we prepare the list of samples FROM fragments
    /// instead of from the TrackBox. Don't call base.PrepareSampleReading because that's only for reading using mdat offsets in the TrackBox.
    /// </summary>
    /// <param name="inStartSampleTime"></param>
    /// <param name="inEndSampleTime"></param>
    /// <param name="lastEnd"></param>
    /// <param name="SampleCount"></param>
    /// <returns></returns>
    public override List<StreamDataBlockInfo> PrepareSampleReading(UInt64 inStartSampleTime, UInt64 inEndSampleTime, ref ulong lastEnd) {
      List<StreamDataBlockInfo> sampleList = new List<StreamDataBlockInfo>();

      // Don't just deal with the current fragment here: search for the correct fragment.
      // We basically ignore the end sample time, and just end the current block with the end of the target fragment.

      ulong start = inStartSampleTime;
      if (inStartSampleTime < lastEnd)
        start = lastEnd;

      ulong diff = start - inStartSampleTime;

      if (CurrentFragment == null)
        return null;

      while (CurrentFragment != null)
      {
        CurrentFragment.GetSampleStream(sampleList, TimeScale, PayloadType, start, inEndSampleTime + diff, ref lastEnd);
        if (sampleList.Count > 0)
        {
          break;
        }
        Read();
      }

      return sampleList;
    }


    public override List<StreamDataBlockInfo> PrepareSampleReading(int inStartSampleIndex, int inEndSampleIndex, ref ulong lastEnd)
    {
      List<StreamDataBlockInfo> sampleList = new List<StreamDataBlockInfo>();

      // We don't just deal with the current fragment here: search for the correct fragment.

      if (CurrentFragment == null)
        return null;

      while (CurrentFragment != null)
      {
        CurrentFragment.GetSampleStream(sampleList, inStartSampleIndex, inEndSampleIndex, ref lastEnd);
        if (sampleList.Count > 0)
          break;
        Read();
      }

      return sampleList;
    }

    bool IsAudio(SliceType sampType) {
      if (sampType == SliceType.AAC)
        return true;
      if (sampType == SliceType.MP4A)
        return true;
      if (sampType == SliceType.WMA)
        return true;

      return false;
    }

    /// <summary>
    /// PrepareSampleWriting
    /// Do not call the base method from here because fragment processing is separate from moov box processing.
    /// </summary>
    /// <param name="streamLocations"></param>
    /// <param name="currMdatOffset"></param>
    public override void PrepareSampleWriting(List<StreamDataBlockInfo> streamLocations, ref ulong currMdatOffset) {
      if (this.CurrentFragment != null)
        throw new Exception("CurrentFragment must be null when ISMVTrackFormat.PrepareSampleWriting is entered");

      // prepare parameters for creating a fragment
      // our output will always use track ID 1 for audio
      //uint trackID = (uint)((streamLocations[0].SampleType == SampleType.MP4A || streamLocations[0].SampleType == SampleType.WMA) ? 1 : 2);
      //uint trackID = (uint)(IsAudio(streamLocations[0].SampleType) ? 1 : 2);

      // this is what fragment run flags should be for ISMV file output
      // FIXME: this is adhoc, need to figure out exactly which bits are correct for these flags
      uint fragmentRunFlags;
      uint defaultSampleFlags;
      if (IsAudio(streamLocations[0].SliceType)) {
        fragmentRunFlags = (uint)0x301; // magic number for audio
        defaultSampleFlags = (uint)0x8002;
      } else {
        fragmentRunFlags = (uint)0xb05; // magic number for video
        defaultSampleFlags = (uint)0x4001;
      }

      // for audio, all samples are of the same size
      uint sampleSize = (IsAudio(streamLocations[0].SliceType)) ? (uint)streamLocations[0].SliceSize : 0;

      // first, create the fragment boxes
      CurrentFragment = new Fragment(fragSequenceNum, base.TrackID, streamLocations.Count, fragmentRunFlags, defaultSampleFlags, sampleSize);
      if (CurrentFragment.Duration != _ismElement.FragmentDurations[(int)fragSequenceNum])
        throw new Exception("ISMVTrackFormat: mismatch in fragment duration between ISMC file and MP4");
      currMdatOffset += this.CurrentFragment.DataOffset;

      fragSequenceNum++;

      // now prepare the samples in this fragment, without copying the sample bits
      this.CurrentFragment.AddSampleStream(streamLocations, base.TimeScale, ref currMdatOffset);
    }
  }
}
