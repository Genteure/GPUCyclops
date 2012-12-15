using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{

  public class HRDParams
  {
    public uint CpbCount; // cpb_cnt_minus1 0 ue(v)
    public byte BitRateScale; // 0 u(4)
    public byte CpbSizeScale; // 0 u(4)
    //for( SchedSelIdx = 0; SchedSelIdx <= cpb_cnt_minus1; SchedSelIdx++ ) {
    public uint[] BitRateValue; // bit_rate_value_minus1; //[ SchedSelIdx ] 0 ue(v)
    public uint[] CpbSizeValue; // cpb_size_value_minus1; //[ SchedSelIdx ] 0 ue(v)
    public bool[] Cbr; // cbr_flag; //[ SchedSelIdx ] 0 u(1)
    //}
    public byte InitialCpbRemovalDelayLength; // minus1; // 0 u(5)
    public byte CpbRemovalDelayLength; //_minus1; // 0 u(5)
    public byte DpbOutputDelayLength; //_minus1; // 0 u(5)
    public byte TimeOffsetLength; // 0 u(5)
    //}

    public void Read(BitReader bitReader)
    {
      CpbCount = bitReader.DecodeUnsignedExpGolomb() + 1;
      BitRateScale = bitReader.GetByteFromNBits(4);
      CpbSizeScale = bitReader.GetByteFromNBits(4);
      BitRateValue = new uint[CpbCount];
      CpbSizeValue = new uint[CpbCount];
      Cbr = new bool[CpbCount];
      for (int i = 0; i < CpbCount; i++)
      {
        BitRateValue[i] = bitReader.DecodeUnsignedExpGolomb() + 1;
        CpbSizeValue[i] = bitReader.DecodeUnsignedExpGolomb() + 1;
        Cbr[i] = bitReader.GetNextBit();
      }
      InitialCpbRemovalDelayLength = (byte)(bitReader.GetByteFromNBits(5) + 1);
      CpbRemovalDelayLength = (byte)(bitReader.GetByteFromNBits(5) + 1);
      DpbOutputDelayLength = (byte)(bitReader.GetByteFromNBits(5) + 1);
      TimeOffsetLength = bitReader.GetByteFromNBits(5);
    }
  }

}
