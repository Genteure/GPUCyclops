using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Media.Formats.Generic;
using Media.H264;

namespace Media.Formats.MP4
{
  class MP4VideoTrack : GenericVideoTrack
  {
    private List<H264Sample> samples = new List<H264Sample>();
    private SequenceParameterSet _sps;
    private PictureParameterSet _pps;

    public override IMediaStream ParentStream
    {
      get { return (IMediaStream)base.ParentStream; }
      set
      {
        base.ParentStream = value;
        MP4Stream mp4stream = base.ParentStream as MP4Stream;
        TrackBox trkbox = mp4stream.mmb.TrackBoxes.First(t => t.MediaBox.HandlerReferenceBox.HandlerType.Equals("vide"));
        VisualSampleEntry vse = (VisualSampleEntry)trkbox.MediaBox.MediaInformationBox.SampleTableBox.SampleDescriptionsBox.Entries[0];
        _sps = vse.AvcCBox.SPS[0]; // FIXME: we are assuming there's only one SPS, PPS
        _pps = vse.AvcCBox.PPS[0];
      }
    }

    public MP4VideoTrack()
    {
      samples.Clear();
    }

    public MP4VideoTrack(MP4TrackFormat format, MP4Stream stream)
      : base(format, stream)
    {
    }

    public override Slice GetSample(StreamDataBlockInfo SampleInfo)
    {
      Slice ans = new Slice();
      ans.Copy(SampleInfo);
      ans.SliceBytes = new byte[SampleInfo.SliceSize];
      // read H264 payload for display and processing --
      // for display:
      ParentStream.Stream.Position = (long)ans.StreamOffset;
      ParentStream.Stream.Read(ans.SliceBytes, 0, ans.SliceSize);
      // for processing:
      // (hand-off the payload processing to a separate thread so this method
      // can return immediately)
#if MV_Centerus
      H264Sample sample = new H264Sample(_sps, _pps, ans.SliceSize);
      sample.SampleDoneEvent += CompletionCallback;
      sample.ParseSample(ans.SliceBytes); // async call
      samples.Add(sample);
#endif
      return (ans);
    }

    private void CompletionCallback(H264Sample sample)
    {
      samples.Remove(sample);
    }
  }
}
