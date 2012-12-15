using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  public class CodedSliceWithoutPartitioning : SliceHeader
  {
    public SliceData Data { get; private set; }

    public CodedSliceWithoutPartitioning(SequenceParameterSet sps, PictureParameterSet pps, Byte idc, NALUnitType naluType, uint size)
      : base(sps, pps, idc, naluType, size)
    {
      Data = new SliceData(sps, pps, this);
    }

    public override void Read(BitReader bitReader)
    {
      base.Read(bitReader); // read header

      Data.Read(bitReader);
      base.CheckSize(bitReader);
    }
  }
}
