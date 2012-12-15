using System;
using System.IO;

namespace Media.Formats.Generic
{
  /// <summary>
  /// H264SPS - Sequence Parameter Set
  /// Properties with prefix "g" means Golomb-encoded in the H264 stream.
  /// Implemented by C. Tapang.
  /// </summary>
  public class H264SPS
  {
    public int Profile;
    public int Constraints;
    public int Level;
    public uint gSPSID; // sequence parameter set ID
    public uint gMaxFrameNumMinus4;
    public uint gPicOrderCntType;
    public uint gMaxPicOrderCntLsbMinus4;
    public bool DeltaPicOrderAlwaysZero;
    public int  gOffsetForNonRefPic;
    public int  gOffsetForTopToBottomField;
    public uint gNumRefFramesInPicOrderCntCycle;
    public int[] gOffsetsForRefFrames;
    public uint gNumRefFrames;
    public bool GapsInFrameNumValueAllowed;
    public uint gWidth;
    public uint WidthInSamples;
    public uint gHeight;
    public uint SizeInMapUnits;
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

    public void Read(BitReader bitReader)
    {
      bitReader.ReadByte(); // discard NALU type
      Profile = bitReader.ReadByte();
      Constraints = bitReader.ReadByte();
      Level = bitReader.ReadByte();
      gSPSID = bitReader.DecodeUnsignedExpGolomb();
      gMaxFrameNumMinus4 = bitReader.DecodeUnsignedExpGolomb();
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
      gWidth = bitReader.DecodeUnsignedExpGolomb() + 1;
      gHeight = bitReader.DecodeUnsignedExpGolomb() + 1;
      FrameMBSOnly = bitReader.GetNextBit();
      uint interlaceFactor = (FrameMBSOnly) ? 1U : 2U;
      gWidth *= 16;
      gHeight *= (16 * interlaceFactor);
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
        //if (vuiParams.AspectRatioInfoPresent)
        //{
        //  if ((vuiParams.AspectRatio == VUIParams.Extended_SAR) && (vuiParams.SARWidth > 0) && (vuiParams.SARHeight > 0))
        //    gWidth = (uint)(gWidth * ((double)vuiParams.SARWidth) / ((double)vuiParams.SARHeight));
        //  else if ((vuiParams.AspectRatio > 0) && (vuiParams.AspectRatio < 17))
        //    gWidth = (uint)(gWidth * ((double)vui_aspect_x[vuiParams.AspectRatio]) / ((double)vui_aspect_y[vuiParams.AspectRatio]));
        //}
      }
    }
  }

  public class VUIParams
  {
    readonly int[] vui_aspect_x = { 0, 1, 12, 10, 16, 40, 24, 20, 32, 80, 18, 15, 64, 160, 4, 3, 2 };
    readonly int[] vui_aspect_y = { 1, 1, 11, 11, 11, 33, 11, 11, 11, 33, 11, 11, 33, 99, 3, 2, 1 };

    public const byte Extended_SAR = 255;

    public bool AspectRatioInfoPresent;
    //if( aspect_ratio_info_present_flag ) {
    public byte AspectRatio;
    //if( aspect_ratio_idc == Extended_SAR ) {
    public ushort SARWidth; //u(16)
    public ushort SARHeight; //u(16)
    //}
    //}
    public bool OverscanInfoPresent; // 0 u(1)
    //if( overscan_info_present_flag )
    public bool OverscanAppropriate; // 0 u(1)
    public bool VideoSignalTypePresent; // 0 u(1)
    //if( video_signal_type_present_flag ) {
    public byte VideoFormat; // 0 u(3)
    public bool VideoFullRange; // 0 u(1)
    public bool ColourDescriptionPresent; // 0 u(1)
    //if( colour_description_present_flag ) {
    public byte ColourPrimaries; // 0 u(8)
    public byte TransferCharacteristics; // 0 u(8)
    public byte MatrixCoefficients; // 0 u(8)
    //}
    //}
    public bool ChromaLocInfoPresent; // 0 u(1)
    //if( chroma_loc_info_present_flag ) {
    public uint ChromaSampleLocTypeTopField; // 0 ue(v)
    public uint ChromaSampleLocTypeBottomField; // 0 ue(v)
    //}
    public bool TimingInfoPresent; // 0 u(1)
    //if( timing_info_present_flag ) {
    public uint NumUnitsInTick; // 0 u(32)
    public uint TimeScale; // 0 u(32)
    public bool FixedFrameRate; // 0 u(1)
    //}
    public bool NALHRDParametersPresent; // 0 u(1)
    //if( nal_hrd_parameters_present_flag )
    //hrd_parameters()
    public HRDParams HRDParams;
    public bool VclHRDParametersPresent; // 0 u(1)
    //if( vcl_hrd_parameters_present_flag )
    //hrd_parameters()
    //if( nal_hrd_parameters_present_flag | | vcl_hrd_parameters_present_flag )
    public bool LowDelayHRDFlag; // 0 u(1)
    public bool PicStructPresent; // 0 u(1)
    public bool BitstreamRestriction; // 0 u(1)
    //if( bitstream_restriction_flag ) {
    public bool MotionVectorsOverPicBoundaries; // 0 u(1)
    public uint MaxBytesPerPicDenom; // 0 ue(v)
    public uint MaxBitsPerMBDenom; // 0 ue(v)
    public uint Log2MaxMVLengthHorizontal;// 0 ue(v)
    public uint Log2MaxMVLengthVertical; // 0 ue(v)
    public uint NumReorderFrames; // 0 ue(v)
    public uint MaxDecFrameBuffering; // 0 ue(v)
    //}
    public int AspectRatioX;
    public int AspectRatioY;

    public void Read(BitReader bitReader)
    {
      AspectRatioInfoPresent = bitReader.GetNextBit();
      if (AspectRatioInfoPresent)
      {
        AspectRatio = bitReader.GetByteFromNBits(8);
        if (AspectRatio == Extended_SAR)
        {
          SARWidth = (ushort)bitReader.GetUIntFromNBits(16);
          SARHeight = (ushort)bitReader.GetUIntFromNBits(16);
        }
      }
      OverscanInfoPresent = bitReader.GetNextBit();
      if (OverscanInfoPresent)
        OverscanAppropriate = bitReader.GetNextBit();
      VideoSignalTypePresent = bitReader.GetNextBit();
      if (VideoSignalTypePresent)
      {
        VideoFormat = bitReader.GetByteFromNBits(3);
        VideoFullRange = bitReader.GetNextBit();
        ColourDescriptionPresent = bitReader.GetNextBit();
        if (ColourDescriptionPresent)
        {
          ColourPrimaries = bitReader.GetByteFromNBits(8);
          TransferCharacteristics = bitReader.GetByteFromNBits(8);
          MatrixCoefficients = bitReader.GetByteFromNBits(8);
        }
      }
      ChromaLocInfoPresent = bitReader.GetNextBit();
      if (ChromaLocInfoPresent)
      {
        ChromaSampleLocTypeTopField = bitReader.DecodeUnsignedExpGolomb();
        ChromaSampleLocTypeBottomField = bitReader.DecodeUnsignedExpGolomb();
      }
      TimingInfoPresent = bitReader.GetNextBit();
      if (TimingInfoPresent)
      {
        NumUnitsInTick = bitReader.GetUIntFromNBits(32);
        TimeScale = bitReader.GetUIntFromNBits(32);
        FixedFrameRate = bitReader.GetNextBit();
      }
      NALHRDParametersPresent = bitReader.GetNextBit();
      if (NALHRDParametersPresent)
      {
        HRDParams = new HRDParams();
        HRDParams.Read(bitReader);
      }
      VclHRDParametersPresent = bitReader.GetNextBit();
      if (VclHRDParametersPresent)
      {
        HRDParams = new HRDParams();
        HRDParams.Read(bitReader);
      }
      if (NALHRDParametersPresent || VclHRDParametersPresent)
        LowDelayHRDFlag = bitReader.GetNextBit();
      PicStructPresent = bitReader.GetNextBit();
      BitstreamRestriction = bitReader.GetNextBit();
      if (BitstreamRestriction)
      {
        MotionVectorsOverPicBoundaries = bitReader.GetNextBit();
        MaxBytesPerPicDenom = bitReader.DecodeUnsignedExpGolomb();
        MaxBitsPerMBDenom = bitReader.DecodeUnsignedExpGolomb();
        Log2MaxMVLengthHorizontal = bitReader.DecodeUnsignedExpGolomb();
        Log2MaxMVLengthVertical = bitReader.DecodeUnsignedExpGolomb();
        NumReorderFrames = bitReader.DecodeUnsignedExpGolomb();
        MaxDecFrameBuffering = bitReader.DecodeUnsignedExpGolomb();
      }

      if ((AspectRatio > 0) && (AspectRatio < 17))
      {
        AspectRatioX = vui_aspect_x[AspectRatio];
        AspectRatioY = vui_aspect_y[AspectRatio];
      }
      //      gWidth = (uint)(gWidth * ((double)vui_aspect_x[vuiParams.AspectRatio]) / ((double)vui_aspect_y[vuiParams.AspectRatio]));
    }
  }

  /// <summary>
  /// H264PPS - Picture Parameter Set
  /// </summary>
  public class H264PPS
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

    public void Read(BitReader bitReader)
    {
      bitReader.ReadByte(); // discard NALU type
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

  /// <summary>
  /// H264SliceHeader - this is not the same as the ODS "slice", this is the H264 slice.
  /// </summary>
  public class H264SliceHeader
  {
    private ushort frameNumBits;

    public uint FirstMBInSlice;
    public uint SliceType;
    public uint PICParameterSetID;
    public uint FrameNum;

    public H264SliceHeader(H264SPS sps)
    {
      if (sps == null)
        frameNumBits = 6;
      else
        frameNumBits = (ushort)(sps.gMaxFrameNumMinus4 + 4);
    }

    // FIXME: incomplete

    public void Read(BitReader bitReader)
    {
      bitReader.ReadByte(); // discard NALU type
      FirstMBInSlice = bitReader.DecodeUnsignedExpGolomb();
      SliceType = bitReader.DecodeUnsignedExpGolomb();
      PICParameterSetID = bitReader.DecodeUnsignedExpGolomb();
      FrameNum = bitReader.GetUIntFromNBits(frameNumBits);
    }
  }

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


  /// <summary>
  /// BitReader
  /// Read one bit at a time from a byte stream.
  /// For every byte, leftmost bit comes first. (Like reading from left to right.)
  /// by: CCT
  /// </summary>
  public class BitReader
  {
    BinaryReader _binaryReader;
    byte _probeShifter;
    byte _currByte;

    public BitReader(Stream byteStream)
    {
      _binaryReader = new BinaryReader(byteStream);
      _probeShifter = 0;
      End = false;
    }

    public bool End { get; private set; }

    public bool GetNextBit()
    {
      _probeShifter = (byte)((int)_probeShifter >> 1);
      if (_probeShifter == 0)
      {
        try
        {
          _currByte = _binaryReader.ReadByte();
        }
        catch (Exception ex)
        {
          if (ex is EndOfStreamException)
            End = true;
          else throw ex;
        }
        _probeShifter = 0x80;
        return ((_currByte & _probeShifter) != 0);
      }
      return ((_currByte & _probeShifter) != 0);
    }

    public byte GetByteFromNBits(ushort N) // N should be from 1 to 8
    {
      byte shifter = (byte)(1U << (N - 1));
      byte retVal = 0;
      for (int i = 0; i < N; i++)
      {
        if (GetNextBit())
          retVal += (byte)((uint)shifter >> i);
      }
      return retVal;
    }

    public uint GetUIntFromNBits(ushort N)
    {
      uint shifter = 1U << (N - 1);
      uint retVal = 0;
      for (int i = 0; i < N; i++)
      {
        if (GetNextBit())
          retVal += shifter >> i;
      }

      return retVal;
    }

    public uint DecodeUnsignedExpGolomb()
    {
      ushort q = 0;
      while (!GetNextBit()) q++;
      if (q > 31)
        throw new Exception("Bad RBSP");
      uint m = GetUIntFromNBits(q);
      return (1U << q) - 1 + m;
    }

    public int DecodeSignedExpGolomb()
    {
      int retVal = (int)DecodeUnsignedExpGolomb();
      if ((retVal % 2) > 0)
        return (retVal + 1) / 2;
      else
        return (-retVal) / 2;
    }

    /// <summary>
    /// ReadByte
    /// Read a byte when the current bit is byte-aligned.
    /// </summary>
    /// <returns></returns>
    public byte ReadByte()
    {
      return _binaryReader.ReadByte();
    }

    public void DiscardTrailingBits()
    {
      if (GetNextBit() != true)
        throw new Exception("BitReader: bad trailing RBSP bits");
      while (_probeShifter > 1)
      {
        if (GetNextBit() != false)
          throw new Exception("BitReader: bit must be zero in trailing RBSP bits");
      }
    }

    #region Static Methods

    public static ushort CalcBitsNeededToRepresent(uint maxVal)
    {
      uint bitProbe = 0x80000000U;
      ushort count = 32;
      while ((maxVal & bitProbe) == 0U)
      {
        bitProbe = bitProbe >> 1;
        count--;
      }

      return count;
    }

    public static uint GetUIntValue(uint endianValue)
    {
      if (BitConverter.IsLittleEndian == false) return (endianValue);
      byte[] bbbb = BitConverter.GetBytes(endianValue);
      byte tmp = bbbb[0];
      bbbb[0] = bbbb[3];
      bbbb[3] = tmp;
      tmp = bbbb[1];
      bbbb[1] = bbbb[2];
      bbbb[2] = tmp;
      return BitConverter.ToUInt32(bbbb, 0);
    }

    #endregion
  }
}
