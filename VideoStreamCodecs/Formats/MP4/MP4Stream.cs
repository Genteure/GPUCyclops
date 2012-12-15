using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Media.Formats.Generic;


namespace Media.Formats.MP4
{
  public class MP4Stream : GenericMediaStream
  {
    public uint VideoWidth = 0;
    public uint VideoHeight = 0;
    public uint VideoBitrate = 0;
    public string VideoPrivateData = "";
    public string VideoFourCC = "";

    public uint AudioBitrate = 0;
    public string AudioPrivateData = "";
    public string AudioFourCC = "";

    public FileTypeBox ftb { get; set; }
    public MovieMetadataBox mmb { get; set; }
    public MovieFragmentRandomAccessBox MovieFragmentRandomAccessBox { get; protected set; }
    public List<MediaDataBox> MediaDataBoxList = new List<MediaDataBox>();
    public List<Box> FreeBoxList = new List<Box>();

    protected GenericAudioTrack audioTrack
    {
      get 
      { 
        return (GenericAudioTrack)base.MediaTracks.FirstOrDefault(tr => tr is GenericAudioTrack); 
      }
    }

    protected GenericVideoTrack videoTrack
    {
      get { return (GenericVideoTrack)base.MediaTracks.FirstOrDefault(tr => tr is GenericVideoTrack); }
    }
  }
}
