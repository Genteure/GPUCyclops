using System;
using System.Collections.Generic;
using System.IO;
using Media.Formats.Generic;
using Media.Formats.MP4;

namespace Media.Formats.MicrosoftFMP4 {
  public class ISMVFile {
    public string strFileName { get; set; }
    public string strDir { get; set; }
		public int TrackID { get; set; }
		public string CodecPrivateData { get; set; }
		public FileTypeBox ftb = new FileTypeBox();
		public MovieMetadataBox mmb = new MovieMetadataBox();
    public Fragment CurrentFragment;
    public ulong FragmentDuration; // current fragment duration

		public BoxReader boxReader { get; set; }
		public BoxReader boxReader2 { get; set; }
		public long tmpPosition = 0;
	  public int tmpIteration = 0;
	  public int bigTmpIteration = 0;

    public List<MediaDataBox> MediaDataBoxList = new List<MediaDataBox>();

    private bool audiovideoScanCompleted = false;
    private bool audioFound = false;
    private bool videoFound = false;
    private long nextVideoFragPosition = 0;
    private long nextAudioFragPosition = 0;
    private int audioTrackID;
    private int videoTrackID;
    private string _handlerType;

    public ISMVFile(string inDir, string inFileName) {
      strDir = inDir;
      strFileName = inFileName;
			boxReader = new BoxReader(File.Open(Path.Combine(inDir, inFileName), FileMode.Open, FileAccess.Read, FileShare.Read));
			boxReader2 = new BoxReader(File.Open(Path.Combine(inDir, inFileName), FileMode.Open, FileAccess.Read, FileShare.Read));

			ftb.Read(boxReader);
			mmb.Read(boxReader);
			BoxType nextType = boxReader.PeekNextBoxType();
    }

    public ISMVFile(string inDir, string inFileName, string handlerType)
      : this(inDir, inFileName)
    {
      _handlerType = handlerType;
    }

    string GetFragmentHandlerType(int trackID)
    {
      foreach (TrackBox trakBx in mmb.TrackBoxes)
      {
        if (trackID == trakBx.TrackHeaderBox.TrackID)
        {
          return (trakBx.MediaBox.HandlerReferenceBox.HandlerType);
        }
      }
      throw new Exception(string.Format("Invalid trakBx ID for fragment: {0}", trackID));
    }

    uint GetTimeScale(int trackID)
    {
      foreach (TrackBox trakBx in mmb.TrackBoxes)
      {
        if (trackID == trakBx.TrackHeaderBox.TrackID)
        {
          return (trakBx.MediaBox.MediaHeaderBox.TimeScale);
        }
      }
      throw new Exception(string.Format("Invalid trakBx ID for fragment: {0}", trackID));
    }

    string GetPayloadType(int trackID)
    {
      foreach (TrackBox trakBx in mmb.TrackBoxes)
      {
        if (trackID == trakBx.TrackHeaderBox.TrackID)
        {
          return (trakBx.PayloadType);
        }
      }
      throw new Exception(string.Format("Invalid trakBx ID for fragment: {0}", trackID));
    }

    // assumption this can ONLY be called when we know the next box is a fragment...
    private void ScanForAudioOrVideo()
    {
      long pos = boxReader.BaseStream.Position;
      do
      {
        long fragPos = boxReader.BaseStream.Position;
        Fragment frag = new Fragment();

        int trakID = (int)frag.GetTrackID(boxReader);
        if (GetFragmentHandlerType(trakID) == _handlerType)
        {
          if (_handlerType.Equals("soun"))
          {
            audioFound = true;
            nextAudioFragPosition = fragPos;
            audioTrackID = trakID;
          }
          else if (_handlerType.Equals("vide"))
          {
            videoFound = true;
            nextVideoFragPosition = fragPos;
            videoTrackID = trakID;
          }
          break;
        }
      } while (this.boxReader.PeekNextBoxType() == BoxTypes.MovieFragment);
      boxReader.BaseStream.Position = pos;
      audiovideoScanCompleted = true;
    }

    private Fragment GetNextVideoFrag()
    {
      if (nextVideoFragPosition < 0) return (null); // there are no more!!
      boxReader.BaseStream.Position = nextVideoFragPosition;

      // we know where at least the next frag is as we prepared this prior to the call of this function...
      Fragment answer = new Fragment(GetTimeScale(videoTrackID), GetPayloadType(videoTrackID));
      answer.Read(boxReader);

      nextVideoFragPosition = -1;
      while (this.boxReader.PeekNextBoxType() == BoxTypes.MovieFragment)
      {
        long fragPos = boxReader.BaseStream.Position;
        Fragment tmp = new Fragment();

        int trakID = (int)tmp.GetTrackID(boxReader);
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
      Fragment answer = new Fragment(GetTimeScale(audioTrackID), GetPayloadType(audioTrackID));
      answer.Read(boxReader);

      nextAudioFragPosition = -1;
      while (this.boxReader.PeekNextBoxType() == BoxTypes.MovieFragment)
      {
        long fragPos = boxReader.BaseStream.Position;
        Fragment tmp = new Fragment();

        int trakID = (int)tmp.GetTrackID(boxReader);
        if (GetFragmentHandlerType(trakID) == "soun")
        {
          nextAudioFragPosition = fragPos;
          break;
        }
      }

      return (answer);
    }

    public void ReadMP4Headers()
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

    public void InitializeForWriting(List<IsochronousTrackInfo> mediaTracks)
    {
      string[] brands = new string[3];
      brands[0] = "isml";
      brands[1] = "piff";
      brands[2] = "iso2";
      this.ftb = new FileTypeBox(brands); // overwrite base class's ftb
      this.ftb.MinorVersion = 1;

      uint[] matrix = new uint[9];
      matrix[0] = 0x10000; // 1.0
      matrix[4] = 0x10000; // 1.0
      matrix[8] = 0x40000000; // 1.0 (see description of RenderMatrix class)

      this.mmb = new MovieMetadataBox(mediaTracks, 1.0f, 1.0f, matrix);

      //CreateTracksForWriting<ISMVTrackFormat>(mediaTracks); // create tracks with ISMVTrackFormat

      // we can finalize the ftyp and moov boxes here, because they shouldn't change when moofs (fragments) are added
      //this.ftb.Write(m_writer);
      this.mmb.FinalizeBox();
      //this.mmb.Write(m_writer);

      //this.CurrMDatOffset = this.ftb.Size + this.mmb.Size; // for fragmented files, CurrMDatOffset is really the file offset

      InitializeForWriting(mediaTracks); // create our tracks (partial moov boxes, which should still exist, even for fragmented tracks)
    }

  }
}
