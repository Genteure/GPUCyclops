using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{

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
}
