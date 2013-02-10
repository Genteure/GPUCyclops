using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  class SubMacroBlockPrediction
  {
    private byte _mbType;

    public SubMacroBlockPrediction(byte mbtype)
    {
      _mbType = mbtype;
    }

    public void Read(BitReader bitReader)
    {
    }
  }
}
