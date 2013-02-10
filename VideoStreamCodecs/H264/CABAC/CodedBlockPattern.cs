using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  public class CodedBlockPattern : CABACBaseClass
  {
    public CodedBlockPattern(PictureParameterSet pps, SliceHeader header) : base(pps, header)
    {
    }

    public uint GetCodedBlockPattern(BitReader bitReader)
    {
      return 0;
    }

    public uint GetCodedBlockPattern()
    {
      if (_pps.EntropyCodingModeFlag)
      {
        return 0;  // FIXME: incomplete
      }
      else
      {
        return (uint)_bitReader.DecodeMappedExpGolomb(); // may need DecodeMappedIntraExpGolomb()
      }
    }
  }
}
