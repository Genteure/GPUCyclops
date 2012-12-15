using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  public class CodedSliceNonIDR : CodedSliceWithoutPartitioning
  {
    public CodedSliceNonIDR(SequenceParameterSet sps, PictureParameterSet pps, uint size)
      : base(sps, pps, (byte)0, NALUnitType.NonIDRSlice, size)
    {
    }
  }
}
