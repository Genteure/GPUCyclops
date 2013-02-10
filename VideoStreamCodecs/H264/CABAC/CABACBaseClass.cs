using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  enum I_MacroBlockType
  {
    I_4x4 = 0,
    I_16x16_0_0_0 = 1,
    I_16x16_1_0_0 = 2,
    I_16x16_2_0_0 = 3,
    I_16x16_3_0_0 = 4,
    I_16x16_0_1_0 = 5,
    I_16x16_1_1_0 = 6,
    I_16x16_2_1_0 = 7,
    I_16x16_3_1_0 = 8,
    I_16x16_0_2_0 = 9,
    I_16x16_1_2_0 = 10,
    I_16x16_2_2_0 = 11,
    I_16x16_3_2_0 = 12,
    I_16x16_0_0_1 = 13,
    I_16x16_1_0_1 = 14,
    I_16x16_2_0_1 = 15,
    I_16x16_3_0_1 = 16,
    I_16x16_0_1_1 = 17,
    I_16x16_1_1_1 = 18,
    I_16x16_2_1_1 = 19,
    I_16x16_3_1_1 = 20,
    I_16x16_0_2_1 = 21,
    I_16x16_1_2_1 = 22,
    I_16x16_2_2_1 = 23,
    I_16x16_3_2_1 = 24,
    I_PCM = 25
  }

  enum MacroBlockPartitionMode
  {
    None,
    Intra_4x4,
    Intra_16x16,
    Pred_L0,
    Pred_L1,
    BitPred,
    Direct
  }

  public class CABACBaseClass
  {
    protected BitReader _bitReader;
    protected PictureParameterSet _pps;
    protected SliceHeader _header;

    protected int maxBinIdxCtx_p; // maxBinIdxCtx prefix
    protected int ctxIdxOffset_p; // ctxIdxOffset prefix
    protected int maxBinIdxCtx_s; // maxBinIdxCtx suffix
    protected int ctxIdxOffset_s; // ctxIdxOffset suffix
    protected bool bypassFlag;
    protected int pStateIdx;
    protected int valMPS;
    protected short codIRange;
    protected short codIOffset;

    public CABACBaseClass(PictureParameterSet pps, SliceHeader sliceHeader)
    {
      _pps = pps;
      _header = sliceHeader;
    }

    public int Clip3(int x, int y, int z)
    {
      if (z < x) return x;
      if (z > y) return y;
      return z;
    }
  }
}
