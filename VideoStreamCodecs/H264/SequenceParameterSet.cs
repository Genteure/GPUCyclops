using System;
using System.Net;

namespace Media.H264
{
  public class SequenceParameterSet : NetworkAbstractionLayerUnit
  {

    public int Profile;
    public int Constraints;
    public int Level;
    public uint gSPSID; // sequence parameter set ID of this instance
    public uint gLog2MaxFrameNumMinus4; // count of bits (less 4) needed to represent frame numbers
    public uint gPicOrderCntType;
    public uint gMaxPicOrderCntLsbMinus4;
    public bool DeltaPicOrderAlwaysZero;
    public int gOffsetForNonRefPic;
    public int gOffsetForTopToBottomField;
    public uint gNumRefFramesInPicOrderCntCycle;
    public int[] gOffsetsForRefFrames;
    public uint gNumRefFrames;
    public bool GapsInFrameNumValueAllowed;
    public uint gPicWidthInMBsMinus1;
    public uint Width;
    public uint WidthInSamples;
    public uint gPicHeightInMBsMinus1;
    public uint Height;
    public uint SizeInMapUnits;
    public uint PicSizeInMBs;
    public bool FrameMBSOnly;
    public bool MBAdaptiveFrameField;
    public bool Direct8x8Inference;
    public bool FrameCropping;
    public uint FrameCropLeftOffset;
    public uint FrameCropRightOffset;
    public uint FrameCropTopOffset;
    public uint FrameCropBottomOffset;
    public bool VUIParametersPresent;
    public VUIParams vuiParams;

    public SequenceParameterSet(uint size)
      : base((byte)1, NALUnitType.SequenceParamSet, size)
    {
    }

    public override void Read(BitReader bitReader)
    {
      base.Read(bitReader);

      Profile = bitReader.ReadByte();
      Constraints = bitReader.ReadByte();
      Level = bitReader.ReadByte();
      gSPSID = bitReader.DecodeUnsignedExpGolomb();
      gLog2MaxFrameNumMinus4 = bitReader.DecodeUnsignedExpGolomb();
      gPicOrderCntType = bitReader.DecodeUnsignedExpGolomb();
      if (gPicOrderCntType == 0)
        gMaxPicOrderCntLsbMinus4 = bitReader.DecodeUnsignedExpGolomb();
      else if (gPicOrderCntType == 1)
      {
        DeltaPicOrderAlwaysZero = bitReader.GetNextBit();
        gOffsetForNonRefPic = bitReader.DecodeSignedExpGolomb();
        gOffsetForTopToBottomField = bitReader.DecodeSignedExpGolomb();
        gNumRefFramesInPicOrderCntCycle = bitReader.DecodeUnsignedExpGolomb();
        gOffsetsForRefFrames = new int[gNumRefFramesInPicOrderCntCycle];
        for (int i = 0; i < gNumRefFramesInPicOrderCntCycle; i++)
          gOffsetsForRefFrames[i] = bitReader.DecodeSignedExpGolomb();
      }
      gNumRefFrames = bitReader.DecodeUnsignedExpGolomb();
      GapsInFrameNumValueAllowed = bitReader.GetNextBit();
      gPicWidthInMBsMinus1 = bitReader.DecodeUnsignedExpGolomb();
      Width = gPicWidthInMBsMinus1 + 1;
      gPicHeightInMBsMinus1 = bitReader.DecodeUnsignedExpGolomb();
      Height = gPicHeightInMBsMinus1 + 1;
      PicSizeInMBs = Height * Width;
      FrameMBSOnly = bitReader.GetNextBit();
      uint interlaceFactor = (FrameMBSOnly) ? 1U : 2U;
      Width *= 16;
      Height *= (16 * interlaceFactor);
      if (!FrameMBSOnly)
        MBAdaptiveFrameField = bitReader.GetNextBit();
      Direct8x8Inference = bitReader.GetNextBit();
      FrameCropping = bitReader.GetNextBit();
      if (FrameCropping)
      {
        FrameCropLeftOffset = bitReader.DecodeUnsignedExpGolomb();
        FrameCropRightOffset = bitReader.DecodeUnsignedExpGolomb();
        FrameCropTopOffset = bitReader.DecodeUnsignedExpGolomb();
        FrameCropBottomOffset = bitReader.DecodeUnsignedExpGolomb();
      }
      VUIParametersPresent = bitReader.GetNextBit();
      if (VUIParametersPresent)
      {
        vuiParams = new VUIParams();
        vuiParams.Read(bitReader);
      }
    }
  }
}
