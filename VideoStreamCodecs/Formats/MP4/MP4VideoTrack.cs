using System;
using System.IO;
using Media.Formats.Generic;
using Media.H264;

namespace Media.Formats.MP4
{
  class MP4VideoTrack : GenericVideoTrack
  {
    public MP4VideoTrack()
    {
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
      ParentStream.Stream.Position = (long)ans.StreamOffset;
      BitReader br = new BitReader(ParentStream.Stream);
      int totalSize = SampleInfo.SliceSize;
      int offset = 0;
      H264Sample sample = new H264Sample(totalSize);
      while (totalSize > 4)
      {
        int naluLen = (int)br.GetUIntFromNBits(32);
        if (naluLen > totalSize)
          break; // throw new Exception("H264 parsing: wrong byte count encountered");
        if (naluLen > 0)
        {
          sample.ParseNalu(br, naluLen);
          offset += naluLen;
          totalSize -= (naluLen + 4);
        }
        else naluLen = 0; // debugging break point
      }
      return (ans);
    }
  }
}
