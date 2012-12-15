using System.IO;
using Media.Formats.Generic;

namespace Media.Formats.QBOX
{
  public class NaluDelimiterBlockInfo : Slice
  {
    public byte[] AccessUnitDelimiter;
  }

  public class QBoxVideoTrack : GenericVideoTrack
  {
    public QBoxVideoTrack(QBoxTrackFormat format, QBoxStream stream)
      : base(format, stream)
    {
    }

    public override Slice GetSample(StreamDataBlockInfo SampleInfo)
    {
      int delimiterLength = 0;
      Slice ans = new Slice();
      ans.Copy(SampleInfo);
      ans.SliceBytes = new byte[SampleInfo.SliceSize];
#if REMOVE_EXTRA_SPS
      NaluDelimiterBlockInfo blockInfo = SampleInfo as NaluDelimiterBlockInfo;
      if (blockInfo.AccessUnitDelimiter != null)
      {
        delimiterLength = blockInfo.AccessUnitDelimiter.Length + 4; // access unit delimiter length is always 2
        ans.SliceBytes[3] = (byte)(delimiterLength - 4); // assume that SliceBytes[0 to 2] are all zeroes, we only need to set LSB
        blockInfo.AccessUnitDelimiter.CopyTo(ans.SliceBytes, 4);
      }
#endif
      //ParentStream.Stream.Position = (long)SampleInfo.StreamOffset;

      // remove empty NALUs (length == 0)
      // also remove trailing bytes, if any, from each NALU
      Slice inSlice = SampleInfo as Slice;
      BinaryReader br = new BinaryReader(new MemoryStream(inSlice.SliceBytes));
      //BinaryReader br = new BinaryReader(ParentStream.Stream);
      int totalSize = SampleInfo.SliceSize - delimiterLength;
      int offset = delimiterLength;
      while (totalSize > 4)
      {
        ulong naluLen = QBox.BE32(br.ReadUInt32());
        if (naluLen > 0UL)
        {
          br.BaseStream.Position -= 4;
          int readLen = (int)naluLen + 4;
          br.Read(ans.SliceBytes, offset, readLen);
          offset += readLen;
          totalSize -= readLen;
        }
        else naluLen = 0; // debugging break point
      }
      return (ans);
    }

    // NextIndexToRead is used for marking each sample with an index
    public int NextIndexToRead;
  }
}
