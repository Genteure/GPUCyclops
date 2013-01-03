using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  class CodedSliceIDR : CodedSliceWithoutPartition
  {
    public CodedSliceIDR(SequenceParameterSet sps, PictureParameterSet pps, uint size)
      : base(sps, pps, (byte)1, NALUnitType.IDRSlice, size)
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
