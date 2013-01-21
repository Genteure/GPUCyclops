using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  class CodedSliceBase
  {
    public NetworkAbstractionLayerUnit Nalu { get; private set; }
    public SliceHeader Header { get; protected set; }
    public SliceData Data { get; protected set; }

    public CodedSliceBase(SequenceParameterSet sps, PictureParameterSet pps, Byte idc, NALUnitType naluType, uint size)
    {
      Nalu = new NetworkAbstractionLayerUnit(idc, naluType, size);
      Header = new SliceHeader(sps, pps, Nalu);
      Data = new SliceData(sps, pps, Header, Nalu);
    }

    public virtual void Read(BitReader bitReader)
    {
      Nalu.Read(bitReader);
    }
  }
}
