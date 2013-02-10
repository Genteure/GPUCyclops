using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  public class MBQPDelta : CABACBaseClass
  {
    public MBQPDelta(PictureParameterSet pps, SliceHeader header) : base(pps, header)
    {
    }

    public int GetMBQPDelta(BitReader bitReader)
    {
      if (_pps.EntropyCodingModeFlag)
      {
        return 0;  // FIXME: incomplete
      }
      else
      {
        return _bitReader.DecodeSignedExpGolomb();
      }
    }
  }
}
