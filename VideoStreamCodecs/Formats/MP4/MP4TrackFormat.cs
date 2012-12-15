using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Media.Formats.Generic;

namespace Media.Formats.MP4
{
  /// <summary>
  /// MP4TrackFormat
  /// A rawTrack can come in any format. In this case it's the MP4 format.
  /// An MP4 rawTrack basically consists of a trak box (TrackBox).
  /// A TrackBox can contain either audio or video samples.
  /// A track may also contain fragments. Here we only keep track of the current fragment.
  /// </summary>
  public class MP4TrackFormat : GenericTrackFormat
  {
    public MP4TrackFormat()
    {
    }


    private TrackBox _trackBox;
    public TrackBox TrackBox  // a track format essentially consists of the TrackBox
    {
      get { return _trackBox; }
      set
      {
        _trackBox = value;
        // we are assuming there is only SampleDescriptionBox entry, so we better check that this is the case
        if (_trackBox.MediaBox.MediaInformationBox.SampleTableBox.SampleDescriptionsBox.EntryCount != 1)
          throw new Exception("SampleDescriptionsBox: assumption that there's only one entry does not hold for this input MP4 file");
      }
    }

    private AudioSampleEntry ase
    {
      get 
      {
        if (_trackBox.MediaBox.HandlerReferenceBox.HandlerType != "soun")
          return null;
        return (AudioSampleEntry)this.TrackBox.MediaBox.MediaInformationBox.SampleTableBox.SampleDescriptionsBox.Entries[0]; 
      }
    }

    private VisualSampleEntry vse
    {
      get 
      {
        if (_trackBox.MediaBox.HandlerReferenceBox.HandlerType != "vide")
          return null;
        return (VisualSampleEntry)this.TrackBox.MediaBox.MediaInformationBox.SampleTableBox.SampleDescriptionsBox.Entries[0]; 
      }
    }

    public override string PayloadType
    {
      get
      {
        return TrackBox.PayloadType;
      }
    }

    public override Codec Codec
    {
      get
      {
        Codec codec = null;
        if (ase != null) // this is an audio track
        {
          codec = new Codec(CodecTypes.Audio);
          if (ase.PrivDataBox != null)
            codec.PrivateCodecData = ase.PrivDataBox.CodecPrivateData;
          else if (ase.PrivDataFullBox != null)
            codec.PrivateCodecData = ase.PrivDataFullBox.CodecPrivateData;
        }
        else if (vse != null) // this is a video track
        {
          codec = new Codec(CodecTypes.Video);
          if (vse.PrivDataBox != null)
            codec.PrivateCodecData = vse.PrivDataBox.CodecPrivateData;
          else if (vse.PrivDataFullBox != null)
            codec.PrivateCodecData = vse.PrivDataFullBox.CodecPrivateData;
          else if (vse.AvcCBox != null)
            codec.PrivateCodecData = vse.AvcCBox.CodecPrivateData;
        }
        return codec;
      }
      set
      {
        if (ase != null) // this is an audio track
        {
          if (ase.PrivDataBox != null)
            ase.PrivDataBox.CodecPrivateData = value.PrivateCodecData;
          else if (ase.PrivDataFullBox != null)
            ase.PrivDataFullBox.CodecPrivateData = value.PrivateCodecData;
        }
        else if (vse != null) // this is a video track
        {
          if (vse.PrivDataBox != null)
            vse.PrivDataBox.CodecPrivateData = value.PrivateCodecData;
          else if (vse.PrivDataFullBox != null)
            vse.PrivDataFullBox.CodecPrivateData = value.PrivateCodecData;
          else if (vse.AvcCBox != null)
            vse.AvcCBox.CodecPrivateData = value.PrivateCodecData;
        }
      }
    }

    public override uint TimeScale
    {
      get
      {
        return this.TrackBox.MediaBox.MediaHeaderBox.TimeScale;
      }
    }

    public override ulong DurationIn100NanoSecs
    {
      get
      {
        return (ulong)TimeArithmetic.ConvertToStandardUnit(TimeScale, this.TrackBox.MediaBox.MediaHeaderBox.Duration);
      }
      set
      {
        this.TrackBox.MediaBox.MediaHeaderBox.Duration = (ulong)TimeArithmetic.ConvertToTimeScale(TimeScale, value);
        this.TrackBox.TrackHeaderBox.Duration = (ulong)TimeArithmetic.ConvertToTimeScale(this.TrackBox.parent.MovieHeaderBox.TimeScale, value);
      }
    }

    public override uint TrackID
    {
      get
      {
        return this.TrackBox.TrackHeaderBox.TrackID;
      }
    }

    public override int ChannelCount
    {
      get 
      {
        if (ase != null)
          return (int)ase.ChannelCount;
        else
          throw new Exception("MP4TrackFormat: no ChannelCount");
      }
    }

    public override int SampleSize
    {
      get
      {
        if (ase != null)
          return (int)ase.SampleSize;
        else
          throw new Exception("MP4TrackFormat: no SampleSize");
      }
    }

    public override int SampleRate
    {
      get
      {
        if (ase != null)
          return (int)ase.SampleRate;
        else
          throw new Exception("MP4TrackFormat: no SampleRate");
      }
    }

    public override Size FrameSize
    {
      get
      {
        if (vse != null)
        {
          Size size = new Size();
          size.Height = (int)vse.Height;
          size.Width = (int)vse.Width;
          return size;
        }
        else
          throw new Exception("MP4TrackFormat: no FrameSize");
      }
    }

    // MP4-specific property
    public bool IsAnamorphic
    {
      get
      {
        if (!TrackBox.MediaBox.HandlerReferenceBox.HandlerType.Equals("vide"))
          return false;
        VisualSampleEntry vse = TrackBox.MediaBox.MediaInformationBox.SampleTableBox.SampleDescriptionsBox.Entries[0] as VisualSampleEntry; // assume there's only one entry
        if (vse.PixelAspectRatioBox == null)
          return false;
        return vse.PixelAspectRatioBox.hSpacing != vse.PixelAspectRatioBox.vSpacing;
      }
    }

    public override List<StreamDataBlockInfo> PrepareSampleReading(UInt64 inStartSampleTime, UInt64 inEndSampleTime, ref ulong lastEnd)
    {
      //lastEnd = 0UL;
      string trackType = this.TrackBox.MediaBox.HandlerReferenceBox.Name; // FIXME: use this to set SampleType below
      SampleTableBox stb = this.TrackBox.MediaBox.MediaInformationBox.SampleTableBox;
      // if we are missing any of three box types, then we can't continue
      if ((stb.SampleSizeBox == null) || (stb.SampleToChunkBox == null) || (stb.ChunkOffSetBox == null))
      {
        throw new Exception("MP4 Track is non-standard (missing Sample Size, Sample to Chunk, or Chunk Offset)");
      }
      // either ctts or stts must be present also because we index on time
      if (stb.DecodingTimeToSampleBox == null)
      {
        throw new Exception("MP4 Track is missing Decoding Time To Sample box");
      }

      //float scale = (float)(TimeScale) / TimeSpan.FromSeconds(1.0).Ticks;
      return stb.InitSampleStreamFromSampleTableBox(this.TrackBox.EdtsBox, TimeScale, inStartSampleTime, inEndSampleTime, ref lastEnd);
    }

    public override List<StreamDataBlockInfo> PrepareSampleReading(int inStartSampleIndex, int inEndSampleIndex, ref ulong lastEnd)
    {
      // MP4 is one-based (not zero-based), so from this point on we are one-based (index starts at 1 instead of 0).
      inStartSampleIndex++;
      inEndSampleIndex++;

      //lastEnd = 0UL;
      string trackType = this.TrackBox.MediaBox.HandlerReferenceBox.Name; // FIXME: use this to set SampleType below
      SampleTableBox stb = this.TrackBox.MediaBox.MediaInformationBox.SampleTableBox;
      // if we are missing any of three box types, then we can't continue
      if ((stb.SampleSizeBox == null) || (stb.SampleToChunkBox == null) || (stb.ChunkOffSetBox == null))
      {
        throw new Exception("MP4 Track is non-standard (missing Sample Size, Sample to Chunk, or Chunk Offset)");
      }
      // either ctts or stts must be present also because we index on time
      if (stb.DecodingTimeToSampleBox == null)
      {
        throw new Exception("MP4 Track is missing Decoding Time To Sample box");
      }

      //float scale = (float)(TimeScale) / TimeSpan.FromSeconds(1.0).Ticks;
      return stb.InitSampleStreamFromSampleTableBox(TimeScale, inStartSampleIndex, inEndSampleIndex, ref lastEnd);
    }

    public override void PrepareSampleWriting(List<StreamDataBlockInfo> streamLocations, ref ulong currMdatOffset)
    {
      SampleTableBox stb = this.TrackBox.MediaBox.MediaInformationBox.SampleTableBox;
      stb.InitSampleTableBoxFromStreamLocations(streamLocations, ref currMdatOffset);
    }

    public override void WriteSamples(BinaryWriter bw, IEnumerable<Slice> slices)
    {
      throw new Exception("MP4TrackFormat.WriteSamples is not implemented");
    }
  }
}
