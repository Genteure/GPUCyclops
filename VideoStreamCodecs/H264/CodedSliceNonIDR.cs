using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  class CodedSliceNonIDR : CodedSliceBase
  {
    public CodedSliceNonIDR(SequenceParameterSet sps, PictureParameterSet pps, uint size)
      : base(sps, pps, (byte)3, NALUnitType.NonIDRSlice, size)
    {
    }

    public override void Read(BitReader bitReader)
    {
      base.Read(bitReader);
    }
  }
}
