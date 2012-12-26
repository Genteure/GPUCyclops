using System;
using System.Net;

namespace Media.H264
{
  public class PictureParameterSet : NetworkAbstractionLayerUnit
  {
    public uint PICParamSetID;
    public uint SEQParamSetID;
    public bool EntropyCodingModeFlag;
    public bool PICOrderPresentFlag;
    public uint NumSliceGroupsMinus1;
    public uint SliceGroupMapType;
    public uint[] RunLengthMinus1;
    public uint[] TopLeft;
    public uint[] BottomRight;
    public bool SliceGroupChangeDirectionFlag;
    public uint SliceGroupChangeRateMinus1;
    public uint PICSizeInMapUnitsMinus1;
    public uint[] SliceGroupID;
    public uint NumRefIDx10ActiveMinus1;
    public uint NumRefIDx11ActiveMinus1;
    public bool WeightedPredFlag;
    public ushort WeightedBiPredIDC;
    public int PICInitQPMinus26;
    public int PICInitQSMinus26;
    public int ChromaQPIndexOffset;
    public bool DeblockingFilterControlPresentFlag;
    public bool ConstrainedIntraPredFlag;
    public bool RedundantPICCountPresentFlag;

    public PictureParameterSet(uint size) : base((byte)3, NALUnitType.PictureParamSet, size)
    {
    }

    public override void Read(BitReader bitReader)
    {
      base.Read(bitReader);

      PICParamSetID = bitReader.DecodeUnsignedExpGolomb();
      SEQParamSetID = bitReader.DecodeUnsignedExpGolomb();
      EntropyCodingModeFlag = bitReader.GetNextBit();
      PICOrderPresentFlag = bitReader.GetNextBit();
      NumSliceGroupsMinus1 = bitReader.DecodeUnsignedExpGolomb();
      RunLengthMinus1 = new uint[NumSliceGroupsMinus1 + 1];
      TopLeft = new uint[NumSliceGroupsMinus1 + 1];
      BottomRight = new uint[NumSliceGroupsMinus1 + 1];
      if (NumSliceGroupsMinus1 > 0)
      {
        SliceGroupMapType = bitReader.DecodeUnsignedExpGolomb();
        switch (SliceGroupMapType)
        {
          case 0:
            for (int grp = 0; grp <= NumSliceGroupsMinus1; grp++)
            {
              RunLengthMinus1[grp] = bitReader.DecodeUnsignedExpGolomb();
            }
            break;
          case 1:
            break;
          case 2:
            for (int grp = 0; grp <= NumSliceGroupsMinus1; grp++) // standards doc, this reads grp < NumSliceGroupsMinus1
            {
              TopLeft[grp] = bitReader.DecodeUnsignedExpGolomb();
              BottomRight[grp] = bitReader.DecodeUnsignedExpGolomb();
            }
            break;
          case 3:
          case 4:
          case 5:
            SliceGroupChangeDirectionFlag = bitReader.GetNextBit();
            SliceGroupChangeRateMinus1 = bitReader.DecodeUnsignedExpGolomb();
            break;
          case 6:
            PICSizeInMapUnitsMinus1 = bitReader.DecodeUnsignedExpGolomb();
            SliceGroupID = new uint[PICSizeInMapUnitsMinus1 + 1];
            ushort bitCount = BitReader.CalcBitsNeededToRepresent(NumSliceGroupsMinus1 + 1);
            for (int grp = 0; grp <= PICSizeInMapUnitsMinus1; grp++)
            {
              SliceGroupID[grp] = bitReader.GetUIntFromNBits(bitCount);
            }
            break;
          default:
            throw new Exception("BitReader: bad slice group map type");
        }
      }
      NumRefIDx10ActiveMinus1 = bitReader.DecodeUnsignedExpGolomb();
      NumRefIDx11ActiveMinus1 = bitReader.DecodeUnsignedExpGolomb();
      WeightedPredFlag = bitReader.GetNextBit();
      WeightedBiPredIDC = (ushort)bitReader.GetUIntFromNBits(2);
      PICInitQPMinus26 = bitReader.DecodeSignedExpGolomb();
      PICInitQSMinus26 = bitReader.DecodeSignedExpGolomb();
      ChromaQPIndexOffset = bitReader.DecodeSignedExpGolomb();
      DeblockingFilterControlPresentFlag = bitReader.GetNextBit();
      ConstrainedIntraPredFlag = bitReader.GetNextBit();
      RedundantPICCountPresentFlag = bitReader.GetNextBit();
      bitReader.DiscardTrailingBits();
    }
  }
}
