using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  class MacroBlockPrediction
  {
    public bool[] PrevIntra4x4PredModeFlag = new bool[16];
    public byte[] RemIntra4x4PredMode = new byte[16];
    public byte IntraChromaPredMode;
    public uint RefIdxl0;
    public uint RefIdxl1;
    public int[][][] MvdL0 = new int[4][][];


    private byte _mbType;
    private MacroBlockLayer _mbLayer;

    public MacroBlockPrediction(byte mbtype, MacroBlockLayer mbl)
    {
      _mbType = mbtype;
      _mbLayer = mbl;
    }

    public void Read(BitReader bitReader)
    {
      if ((_mbLayer.MbPartPredMode(_mbType, 0) == MacroBlockPartitionMode.Intra_4x4) ||
        (_mbLayer.MbPartPredMode(_mbType, 0) == MacroBlockPartitionMode.Intra_16x16))
      {
        if (_mbLayer.MbPartPredMode(_mbType, 0) == MacroBlockPartitionMode.Intra_4x4)
        {
          for (int luma4x4ids = 0; luma4x4ids < 16; luma4x4ids++)
          {
            PrevIntra4x4PredModeFlag[luma4x4ids] = bitReader.GetNextBit();
            if (!PrevIntra4x4PredModeFlag[luma4x4ids])
            {
              RemIntra4x4PredMode[luma4x4ids] = bitReader.GetByteFromNBits(3);
            }
          }
        }
        IntraChromaPredMode = (byte)bitReader.DecodeUnsignedExpGolomb();
      }
      else if (_mbLayer.MbPartPredMode(_mbType, 0) != MacroBlockPartitionMode.Direct)
      {
        int numPart = (int)_mbLayer.NumMbPart(_mbType);
        for (int mbPartIdx = 0; mbPartIdx < numPart; mbPartIdx++)
        {
        }
        for (int mbPartIdx = 0; mbPartIdx < numPart; mbPartIdx++)
        {
        }
        for (int mbPartIdx = 0; mbPartIdx < numPart; mbPartIdx++)
        {
        }
        for (int mbPartIdx = 0; mbPartIdx < numPart; mbPartIdx++)
        {
        }
      }
    }
  }
}
