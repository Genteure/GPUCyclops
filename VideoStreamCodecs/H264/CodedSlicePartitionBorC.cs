using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  /// <summary>
  /// CodedSlicePartitionBorC
  /// This is almost the same as CodedSlicePartitionA except that it has no Header.
  /// </summary>
  class CodedSlicePartitionBorC : CodedSliceBase
  {
    private PictureParameterSet _pps;

    public uint SliceID { get; private set; }
    public uint RedundantPictureCount { get; private set; }
    public SliceData Data { get; protected set; }

    public CodedSlicePartitionBorC(SequenceParameterSet sps, PictureParameterSet pps,
      byte idc, NALUnitType naluType, uint size)
      : base(sps, pps, (byte)0, NALUnitType.SlicePartitionA, size)
    {
      _pps = pps;
    }

    public override void Read(BitReader bitReader)
    {
      base.Read(bitReader); // read-in the NALU

      // NOTE: this coded slice has no Header (it should remain null)
      SliceID = bitReader.DecodeUnsignedExpGolomb();
      if (_pps.RedundantPICCountPresentFlag)
        RedundantPictureCount = bitReader.DecodeUnsignedExpGolomb();
      Data.Read(bitReader);

      // FIXME: SliceData above is not complete, so for now let's just skip the rest of data
      Nalu.SkipToEndOfNALU(bitReader);

      Nalu.CheckSize(bitReader);
    }
  }
}
