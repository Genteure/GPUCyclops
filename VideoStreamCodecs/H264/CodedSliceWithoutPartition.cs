using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  class CodedSliceWithoutPartition : CodedSliceBase
  {
    public CodedSliceWithoutPartition(SequenceParameterSet sps, PictureParameterSet pps, 
      Byte idc, NALUnitType naluType, uint size)
      : base(sps, pps, idc, naluType, size)
    {
    }

    public override void Read(BitReader bitReader)
    {
      base.Read(bitReader); // read-in the NALU

      Header.Read(bitReader);
      Data.Read(bitReader);
    }
  }
}
