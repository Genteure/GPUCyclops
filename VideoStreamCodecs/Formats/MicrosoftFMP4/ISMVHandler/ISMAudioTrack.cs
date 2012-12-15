using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Media.Formats.Generic;

namespace Media.Formats.MicrosoftFMP4
{
  public class ISMAudioTrack : GenericAudioTrack
  {
    public ISMAudioTrack(ISMVTrackFormat audioFile, ISMVStream stream)
    {
      base.TrackFormat = audioFile; // this sets the PayloadType and other properties also
      ParentStream = stream;

      // set other properties
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
