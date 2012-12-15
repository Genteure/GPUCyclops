using System;
using System.Net;


namespace Media.H264
{
  public enum SliceTypes
  {
    P = 0, // P Slice
    B, // B Slice
    I, // I Slice
    SP, // SP Slice
    SI, // SI Slice
    PA, // P Slice (for all slices in picture)
    BA, // B Slice (for all slices in picture)
    IA, // I Slice (for all slices in picture)
    SPA, // SP Slice (for all slices in picture)
    SIA // SI Slice (for all slices in picture)
  }

  /// <summary>
  /// H264.SliceHeader - this is not the same as the ODS "slice", this is the H264 slice.
  /// An H264 Slice consists of MacroBlocks.
  /// Syntactically, a Slice comes as a header (SliceHeader) followed by a body (SliceBody).
  /// There are many MacroBlocks in a slice, and slices, in turn, make up a picture.
  /// </summary>
  public class SliceHeader : NetworkAbstractionLayerUnit
  {
    private SequenceParameterSet _sps;
    private PictureParameterSet _pps;

    private ushort _frameNumBits 
    { 
      get 
      { 
        if (_sps == null) return 6; // default is 6 bits
        return (ushort)(_sps.gLog2MaxFrameNumMinus4 + 4); 
      } 
    }
    
    // always present
    public uint FirstMBInSlice; // first macroblock in slice
    public SliceTypes SliceType;
    public uint PICParameterSetID;
    public uint FrameNum;

    public bool MBaffFrameFlag { get { return (_sps.MBAdaptiveFrameField && FieldPicFlag); } }

    // conditional
    public bool FieldPicFlag = false;
    public bool BottomFieldFlag;
    public uint IDRPictureID;
    public uint PictureOrderCountLSB;
    public int DeltaPictureOrderCountBottom;
    public int[] DeltaPictureOrderCount;
    public uint RedundantPictureCount;
    public bool DirectSpatialMVPredFlag;
    public bool NumRefIdxActiveOverrideFlag;
    public uint NumRefIdx10ActiveMinus1;
    public uint NumRefIdx11ActiveMinus1;

    // Reference Picture List Reordering
    public bool RefPicListReorderingFlagI0;
    public bool RefPicListReorderingFlagI1;
    public uint ReorderingOfPicNumsIDC;
    public uint AbsDiffPicNumMinus1;
    public uint LongTermPicNum;

    // Prediction Weight Table
    public uint LumaLog2WeightDenominator;
    public uint ChromaLog2WeightDenominator;
    public bool LumaWeightI0Flag;
    public int[] LumaWeightI0;
    public int[] LumaOffsetI0;
    public bool ChromaWeightI0Flag;
    public int[][] ChromaWeightI0;
    public int[][] ChromaOffsetI0;
    public bool LumaWeightI1Flag;
    public int[] LumaWeightI1;
    public int[] LumaOffsetI1;
    public bool ChromaWeightI1Flag;
    public int[][] ChromaWeightI1;
    public int[][] ChromaOffsetI1;

    // Decoded Reference Picture Marking
    public bool NoOutputOfPriorPicsFlag;
    public bool LongTermReferenceFlag;
    public bool AdaptiveRefPicMarkingModeFlag;
    public uint MemoryManagementControlOperation;
    public uint DifferenceOfPicNumsMinus1;
    public uint LongTermFrameIdx;
    public uint MaxLongTermFrameIdxPlus1;

    // more
    public uint CABACInitIDC;
    public int SliceQPDelta;
    public bool SP4SwitchFlag;
    public int SliceQSDelta;
    public uint DisableDeblockingFilterIDC;
    public int SliceAlphaC0OffsetDiv2;
    public int SliceBetaOffsetDiv2;
    public uint SliceGroupChangeCycle;
    public uint[] MbToSliceGroupMap;

    public SliceHeader(SequenceParameterSet sps, PictureParameterSet pps, Byte idc, NALUnitType naluType, uint size)
      : base(idc, naluType, size)
    {
      _sps = sps;
      _pps = pps;
    }

    public override void Read(BitReader bitReader)
    {
      base.Read(bitReader);

      FirstMBInSlice = bitReader.DecodeUnsignedExpGolomb();
      uint st = bitReader.DecodeUnsignedExpGolomb();
      SliceType = (SliceTypes)st;
      PICParameterSetID = bitReader.DecodeUnsignedExpGolomb();
      FrameNum = bitReader.GetUIntFromNBits(_frameNumBits);

      if (!_sps.FrameMBSOnly)
      {
        FieldPicFlag = bitReader.GetNextBit();
        if (FieldPicFlag)
          BottomFieldFlag = bitReader.GetNextBit();
      }

      if (NALUType == NALUnitType.IDRSlice)
      {
        IDRPictureID = bitReader.DecodeUnsignedExpGolomb();
      }

      if (_sps.gPicOrderCntType == 0)
      {
        PictureOrderCountLSB = bitReader.GetUIntFromNBits((ushort)(_sps.gMaxPicOrderCntLsbMinus4 + 4u));
        if (_pps.PICOrderPresentFlag && !FieldPicFlag)
          DeltaPictureOrderCountBottom = bitReader.DecodeSignedExpGolomb();
      }

      if ((_sps.gPicOrderCntType == 1) && !_sps.DeltaPicOrderAlwaysZero)
      {
        DeltaPictureOrderCount = new int[2];
        DeltaPictureOrderCount[0] = bitReader.DecodeSignedExpGolomb();
        if (_pps.PICOrderPresentFlag && !FieldPicFlag)
          DeltaPictureOrderCount[1] = bitReader.DecodeSignedExpGolomb();
      }

      if (_pps.RedundantPICCountPresentFlag)
        RedundantPictureCount = bitReader.DecodeUnsignedExpGolomb();

      if (SliceType == SliceTypes.B)
        DirectSpatialMVPredFlag = bitReader.GetNextBit();

      if ((SliceType == SliceTypes.P) || (SliceType == SliceTypes.SP) || (SliceType == SliceTypes.B))
      {
        NumRefIdxActiveOverrideFlag = bitReader.GetNextBit();
        if (NumRefIdxActiveOverrideFlag)
        {
          NumRefIdx10ActiveMinus1 = bitReader.DecodeUnsignedExpGolomb();
          if (SliceType == SliceTypes.B)
            NumRefIdx11ActiveMinus1 = bitReader.DecodeUnsignedExpGolomb();
        }
      }

      ReferencePictureListReordering(bitReader);

      if ((_pps.WeightedPredFlag && ((SliceType == SliceTypes.P) || (SliceType == SliceTypes.SP))) ||
        ((_pps.WeightedBiPredIDC == 1) && (SliceType == SliceTypes.B)))
      {
        PredictionWeightTable(bitReader);
      }

      if (NALRefIDC != 0)
        DecodedReferencePictureMarking(bitReader);

      if (_pps.EntropyCodingModeFlag && (SliceType != SliceTypes.I) && (SliceType != SliceTypes.SI))
        CABACInitIDC = bitReader.DecodeUnsignedExpGolomb();

      SliceQPDelta = bitReader.DecodeSignedExpGolomb();

      if ((SliceType == SliceTypes.SP) || (SliceType == SliceTypes.SI))
      {
        if (SliceType == SliceTypes.SP)
          SP4SwitchFlag = bitReader.GetNextBit();
        SliceQSDelta = bitReader.DecodeSignedExpGolomb();
      }

      if (_pps.DeblockingFilterControlPresentFlag)
      {
        DisableDeblockingFilterIDC = bitReader.DecodeUnsignedExpGolomb();
        if (DisableDeblockingFilterIDC != 1)
        {
          SliceAlphaC0OffsetDiv2 = bitReader.DecodeSignedExpGolomb();
          SliceBetaOffsetDiv2 = bitReader.DecodeSignedExpGolomb();
        }
      }

      if ((_pps.NumSliceGroupsMinus1 > 0) && (_pps.SliceGroupMapType >= 3) && (_pps.SliceGroupMapType == 5))
        SliceGroupChangeCycle = bitReader.GetUIntFromNBits(BitReader.CalcBitsNeededToRepresent(_pps.NumSliceGroupsMinus1 + 1));

      // once the header is read, we can derive the MacroBlock to SliceGroup mapping (Section 8.2.2 of H264 Spec)
      MbToSliceGroupMap = DeriveMB2SliceGroupMap();

      // FIXME: for now, we will just advance to the slice body because we don't really need slice group mapping for what we want
      base.SkipToEndOfNALU(bitReader);
    }

    // Use PPS and this header to derive macro block to slice group mapping
    private uint[] DeriveMB2SliceGroupMap()
    {
      uint[] mb2sgm = new uint[1];
      mb2sgm[0] = 0; // FIXME: single element with zero value for now
      return mb2sgm;
    }

    private void ReferencePictureListReordering(BitReader bitReader)
    {
      if ((SliceType != SliceTypes.I) && (SliceType != SliceTypes.SI))
      {
        RefPicListReorderingFlagI0 = bitReader.GetNextBit();
        if (RefPicListReorderingFlagI0)
          do
          {
            ReorderingOfPicNumsIDC = bitReader.DecodeUnsignedExpGolomb();
            if ((ReorderingOfPicNumsIDC == 0) || (ReorderingOfPicNumsIDC == 1))
              AbsDiffPicNumMinus1 = bitReader.DecodeUnsignedExpGolomb();
            else if (ReorderingOfPicNumsIDC == 2)
              LongTermPicNum = bitReader.DecodeUnsignedExpGolomb();
          } while (ReorderingOfPicNumsIDC != 3);
      }

      if (SliceType == SliceTypes.B)
      {
        RefPicListReorderingFlagI1 = bitReader.GetNextBit();
        if (RefPicListReorderingFlagI1)
          do
          {
            ReorderingOfPicNumsIDC = bitReader.DecodeUnsignedExpGolomb();
            if ((ReorderingOfPicNumsIDC == 0) || (ReorderingOfPicNumsIDC == 1))
              AbsDiffPicNumMinus1 = bitReader.DecodeUnsignedExpGolomb();
            else if (ReorderingOfPicNumsIDC == 2)
              LongTermPicNum = bitReader.DecodeUnsignedExpGolomb();
          } while (ReorderingOfPicNumsIDC != 3);
      }
    }

    private void PredictionWeightTable(BitReader bitReader)
    {
      LumaLog2WeightDenominator = bitReader.DecodeUnsignedExpGolomb();
      ChromaLog2WeightDenominator = bitReader.DecodeUnsignedExpGolomb();
      uint cnt = (NumRefIdx10ActiveMinus1 + 1);
      if (cnt > 0)
      {
        LumaWeightI0 = new int[cnt];
        LumaOffsetI0 = new int[cnt];
        ChromaWeightI0 = new int[cnt][];
        ChromaOffsetI0 = new int[cnt][];
        for (int i = 0; i < cnt; i++)
        {
          LumaWeightI0Flag = bitReader.GetNextBit();
          if (LumaWeightI0Flag)
          {
            LumaWeightI0[i] = bitReader.DecodeSignedExpGolomb();
            LumaOffsetI0[i] = bitReader.DecodeSignedExpGolomb();
          }
          ChromaWeightI0Flag = bitReader.GetNextBit();
          if (ChromaWeightI0Flag)
          {
            ChromaWeightI0[i] = new int[2];
            ChromaOffsetI0[i] = new int[2];
            for (int j = 0; j < 2; j++)
            {
              ChromaWeightI0[i][j] = bitReader.DecodeSignedExpGolomb();
              ChromaOffsetI0[i][j] = bitReader.DecodeSignedExpGolomb();
            }
          }
        }
      }

      if (SliceType == SliceTypes.B)
      {
        cnt = (NumRefIdx11ActiveMinus1 + 1);
        if (cnt > 0)
        {
          LumaWeightI1 = new int[cnt];
          LumaOffsetI1 = new int[cnt];
          ChromaWeightI1 = new int[cnt][];
          ChromaOffsetI1 = new int[cnt][];
          for (int i = 0; i < cnt; i++)
          {
            LumaWeightI1Flag = bitReader.GetNextBit();
            if (LumaWeightI1Flag)
            {
              LumaWeightI1[i] = bitReader.DecodeSignedExpGolomb();
              LumaOffsetI1[i] = bitReader.DecodeSignedExpGolomb();
            }
            ChromaWeightI1Flag = bitReader.GetNextBit();
            if (ChromaWeightI1Flag)
            {
              ChromaWeightI1[i] = new int[2];
              ChromaOffsetI1[i] = new int[2];
              for (int j = 0; j < 2; j++)
              {
                ChromaWeightI1[i][j] = bitReader.DecodeSignedExpGolomb();
                ChromaOffsetI1[i][j] = bitReader.DecodeSignedExpGolomb();
              }
            }
          }
        }
      }
    }

    private void DecodedReferencePictureMarking(BitReader bitReader)
    {
      if (NALUType == NALUnitType.IDRSlice)
      {
        NoOutputOfPriorPicsFlag = bitReader.GetNextBit();
        LongTermReferenceFlag = bitReader.GetNextBit();
      }
      else
      {
        AdaptiveRefPicMarkingModeFlag = bitReader.GetNextBit();
        if (AdaptiveRefPicMarkingModeFlag)
        {
          do
          {
            MemoryManagementControlOperation = bitReader.DecodeUnsignedExpGolomb();
            if ((MemoryManagementControlOperation == 1) || (MemoryManagementControlOperation == 3))
              DifferenceOfPicNumsMinus1 = bitReader.DecodeUnsignedExpGolomb();
            if (MemoryManagementControlOperation == 2)
              LongTermPicNum = bitReader.DecodeUnsignedExpGolomb();
            if ((MemoryManagementControlOperation == 3) || (MemoryManagementControlOperation == 6))
              LongTermFrameIdx = bitReader.DecodeUnsignedExpGolomb();
            if (MemoryManagementControlOperation == 4)
              MaxLongTermFrameIdxPlus1 = bitReader.DecodeUnsignedExpGolomb();
          } while (MemoryManagementControlOperation != 0);
        }
      }
    }
  }
}
