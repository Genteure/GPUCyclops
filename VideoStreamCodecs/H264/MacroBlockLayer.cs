using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  class MacroBlockLayer
  {
    public uint MBType;
    public byte[] PCMByte;
    public uint CodedBlockPattern;
    public uint MBQPDelta;

    public MacroBlockLayer()
    {
    }

    public void Read(BitReader bitReader)
    {
    }
  }
}
