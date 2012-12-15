using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Media.Formats.Generic;
//using Media.Formats.MicrosoftFMP4;

namespace Media.Formats.MicrosoftFMP4
{
  public class ISMVideoTrack : GenericVideoTrack
  {
    public ISMVideoTrack(ISMVTrackFormat videoFile, ISMVStream stream)
    {
      base.TrackFormat = videoFile; // sets payload type and other properties also
      ParentStream = stream;

      // set other properties of this track
    }

    public override Slice GetSample(StreamDataBlockInfo SampleInfo)
    {
      Slice ans = new Slice();
      ans.SliceBytes = new byte[SampleInfo.SliceSize];

      //ParentStream.EnterMutex();
      ISMVTrackFormat ismvFormat = TrackFormat as ISMVTrackFormat;
      ismvFormat.boxReader.BaseStream.Position = (long)SampleInfo.StreamOffset; // if this GetSample call follows another one, file should be in position
      ismvFormat.boxReader.BaseStream.Read(ans.SliceBytes, 0, SampleInfo.SliceSize);
      //ParentStream.LeaveMutex();

      ans.Copy(SampleInfo);
      return (ans);
    }
  }
}
