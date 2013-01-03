using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  class CodedSliceNonIDR : CodedSliceWithoutPartition
  {
    public CodedSliceNonIDR(SequenceParameterSet sps, PictureParameterSet pps, byte idc, uint size)
      : base(sps, pps, idc, NALUnitType.NonIDRSlice, size)
    {
    }

    public override void Read(BitReader bitReader)
    {
      base.Read(bitReader);

      // FIXME: SliceData above is not complete, so for now let's just skip the rest of data
      Nalu.SkipToEndOfNALU(bitReader);

      Nalu.CheckSize(bitReader);
    }
  }
}
