using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  public enum PictureType
  {
    I = 0,
    I_P,
    I_P_B,
    SI,
    SI_SP,
    I_SI,
    I_SI_P_SP,
    I_SI_P_SP_B
  }

  /// <summary>
  /// AccessUnitDelimiter - always the first NALU, holds the PictureType
  /// </summary>
  public class AccessUnitDelimiter : NetworkAbstractionLayerUnit
  {
    public PictureType PrimaryPicType;

    public AccessUnitDelimiter(uint size)
      : base((byte)0, NALUnitType.AccessUnitDelimiter, size)
    {
    }

    public override void Read(BitReader bitReader)
    {
      base.Read(bitReader);
      PrimaryPicType = (PictureType)bitReader.GetByteFromNBits(3);
      bitReader.DiscardTrailingBits(); // complete the byte
      while (MoreRBSPData(bitReader))
        bitReader.ReadByte();
    }
  }

  /// <summary>
  /// SupplementatlEnhancementMessage - has useful data
  /// </summary>
  public class SupplementatlEnhancementMessage : NetworkAbstractionLayerUnit
  {
    public uint PayloadType = 0U;
    public uint PayloadSize = 0U;

    public uint SeqParameterSetID;
    public uint[] InitialCPBRemovalDelay;
    public uint[] InitialCPBRemovalDelayOffset;
    public uint CpbRemovalDelay;
    public uint DpbOutputDelay;
    public byte PicStruct;
    public bool[] ClockTimestampFlag;
    public byte CTType;
    public bool UUITFieldBasedFlag;
    public byte CountingType;
    public bool FullTimestampFlag;
    public bool DiscontinuityFlag;
    public bool CountDroppedFlag;
    public ushort NFrames;
    public byte SecondsValue;
    public byte MinutesValue;
    public byte HoursValue;
    public bool SecondsFlag;
    public bool MinutesFlag;
    public bool HoursFlag;
    public uint TimeOffset;

    private readonly byte[] NumClockTS = new byte[] { 1, 1, 1, 2, 2, 3, 3, 2, 3 };
    private BitReader _bitReader;
    private SequenceParameterSet _sps;

    public SupplementatlEnhancementMessage(uint size)
      : base((byte)0, NALUnitType.SupplementalEnhancementInfo, size)
    {
    }

    public SupplementatlEnhancementMessage(SequenceParameterSet sps, uint size)
      : this(size)
    {
      _sps = sps;
    }

    public override void Read(BitReader bitReader)
    {
      base.Read(bitReader);
      _bitReader = bitReader;

      do
      {
        SEIMessage(bitReader);
      } while (MoreRBSPData(bitReader));
      SkipToEndOfNALU(bitReader);
    }

    private void SEIMessage(BitReader bitReader)
    {
      byte lastTypeByte;
      PayloadType = 0;
      while (0xFF == (lastTypeByte = bitReader.ReadByte()))
        PayloadType += 255;
      PayloadType += lastTypeByte;

      byte lastSizeByte;
      PayloadSize = 0;
      while (0xFF == (lastSizeByte = bitReader.ReadByte()))
        PayloadSize += 255;
      PayloadSize += lastSizeByte;

      SEIPayload();
    }

    private void SEIPayload()
    {
      switch (PayloadType)
      {
        case 0:
          BufferingPeriod();
          break;
        case 1:
          PicTiming();
          break;
        case 2:
          PanScanRect();
          break;
        case 3:
          FillerPayload();
          break;
        case 4:
          UserDataRegisteredITUTT35();
          break;
        case 5:
          UserDataUnregistered();
          break;
        case 6:
          RecoveryPoint();
          break;
        case 7:
          DecRefPicMarkingRepetition();
          break;
        case 8:
          SparePic();
          break;
        case 9:
          SceneInfo();
          break;
        case 10:
          SubSeqInfo();
          break;
        case 11:
          SubSeqLayerCharacteristics();
          break;
        case 12:
          SubSeqCharacteristics();
          break;
        case 13:
          FullFrameFreeze();
          break;
        case 14:
          FullFrameFreezeRelease();
          break;
        case 15:
          FullFrameSnapshot();
          break;
        case 16:
          ProgressiveRefinementSegmentStart();
          break;
        case 17:
          ProgressiveReginementSegmentEnd();
          break;
        case 18:
          MotionConstrainedSliceGroupSet();
          break;
        default:
          ReservedSEIMessage();
          break;
      }

      if (!_bitReader.ByteAligned)
      {
        while (!_bitReader.GetNextBit())
          ; // throw new Exception("Unexpected zero bit at end of SEI");
        while (!_bitReader.ByteAligned)
          if (_bitReader.GetNextBit())
            throw new Exception("Unexpected non-zero bit at end of SEI");
      }
    }

    private void BufferingPeriod()
    {
      SeqParameterSetID = _bitReader.DecodeUnsignedExpGolomb();
      if (SeqParameterSetID != _sps.gSPSID)
        throw new Exception("wrong sps ID in SEI");
      if (_sps.vuiParams.NALHRDParametersPresent)
      {
        ushort icpbBitLen = _sps.vuiParams.HRDParams.InitialCpbRemovalDelayLength;
        uint cpbCount = _sps.vuiParams.HRDParams.CpbCount;
        InitialCPBRemovalDelay = new uint[cpbCount];
        InitialCPBRemovalDelayOffset = new uint[cpbCount];
        for (int i = 0; i < cpbCount; i++)
        {
          InitialCPBRemovalDelay[i] = _bitReader.GetUIntFromNBits(icpbBitLen);
          InitialCPBRemovalDelayOffset[i] = _bitReader.GetUIntFromNBits(icpbBitLen);
        }
      }

      // FIXME: The following if statement should probably be "else if".
      // ITU-T Rec. H.264(05/2003) p. 259
      // FIXME: set to true for now
      bool needForPresenceOfBufferingPeriods = true; 
      if (_sps.vuiParams.VclHRDParametersPresent || needForPresenceOfBufferingPeriods)
      {
        ushort icpbBitLen = _sps.vuiParams.HRDParams.InitialCpbRemovalDelayLength;
        uint cpbCount = _sps.vuiParams.HRDParams.CpbCount;
        InitialCPBRemovalDelay = new uint[cpbCount];
        InitialCPBRemovalDelayOffset = new uint[cpbCount];
        for (int i = 0; i < cpbCount; i++)
        {
          InitialCPBRemovalDelay[i] = _bitReader.GetUIntFromNBits(icpbBitLen);
          InitialCPBRemovalDelayOffset[i] = _bitReader.GetUIntFromNBits(icpbBitLen);
        }
      }
    }

    private void PicTiming()
    {
      bool CpbDpbDelaysPresent = true; // based on application need, see p. 260 of ITU-T
      CpbDpbDelaysPresent = CpbDpbDelaysPresent || _sps.vuiParams.NALHRDParametersPresent || _sps.vuiParams.VclHRDParametersPresent;

      if (CpbDpbDelaysPresent)
      {
        CpbRemovalDelay = _bitReader.GetUIntFromNBits(_sps.vuiParams.HRDParams.CpbRemovalDelayLength);
        if (_sps.vuiParams.MaxDecFrameBuffering > 0)
          DpbOutputDelay = _bitReader.GetUIntFromNBits(_sps.vuiParams.HRDParams.DpbOutputDelayLength);
        else DpbOutputDelay = 0;
      }

      if (_sps.vuiParams.PicStructPresent)
      {
        PicStruct = _bitReader.GetByteFromNBits(4);
        byte count = NumClockTS[PicStruct];
        ClockTimestampFlag = new bool[count];
        for (int i = 0; i < count; i++)
        {
          ClockTimestampFlag[i] = _bitReader.GetNextBit();
          if (ClockTimestampFlag[i])
          {
            CTType = _bitReader.GetByteFromNBits(2);
            UUITFieldBasedFlag = _bitReader.GetNextBit();
            CountingType = _bitReader.GetByteFromNBits(5);
            FullTimestampFlag = _bitReader.GetNextBit();
            DiscontinuityFlag = _bitReader.GetNextBit();
            CountDroppedFlag = _bitReader.GetNextBit();
            NFrames = (ushort)_bitReader.GetUIntFromNBits(8);
            if (FullTimestampFlag)
            {
              SecondsValue = _bitReader.GetByteFromNBits(6);
              MinutesValue = _bitReader.GetByteFromNBits(6);
              HoursValue = _bitReader.GetByteFromNBits(5);
            }
            else
            {
              SecondsFlag = _bitReader.GetNextBit();
              if (SecondsFlag)
              {
                SecondsValue = _bitReader.GetByteFromNBits(6);
                MinutesFlag = _bitReader.GetNextBit();
                if (MinutesFlag)
                {
                  MinutesValue = _bitReader.GetByteFromNBits(6);
                  HoursFlag = _bitReader.GetNextBit();
                  if (HoursFlag)
                    HoursValue = _bitReader.GetByteFromNBits(5);
                }
              }
            }

            ushort offLen = _sps.vuiParams.HRDParams.TimeOffsetLength;
            if (offLen > 0)
              TimeOffset = _bitReader.GetUIntFromNBits(offLen);
          }
        }
      }
    }

    private void PanScanRect()
    {
    }

    private void FillerPayload()
    {
    }

    private void UserDataRegisteredITUTT35()
    {
    }

    private void UserDataUnregistered()
    {
    }

    private void RecoveryPoint()
    {
      uint RecoveryFrameCount = _bitReader.DecodeUnsignedExpGolomb();
      bool ExactMatchFlag = _bitReader.GetNextBit();
      bool BrokenLinkFlag = _bitReader.GetNextBit();
      byte ChangingSliceGroupIDC = _bitReader.GetByteFromNBits(2);
    }

    private void DecRefPicMarkingRepetition()
    {
    }

    private void SparePic()
    {
    }

    private void SceneInfo()
    {
    }

    private void SubSeqInfo()
    {
    }

    private void SubSeqLayerCharacteristics()
    {
    }

    private void SubSeqCharacteristics()
    {
    }

    private void FullFrameFreeze()
    {
    }

    private void FullFrameFreezeRelease()
    {
    }

    private void FullFrameSnapshot()
    {
    }

    private void ProgressiveRefinementSegmentStart()
    {
    }

    private void ProgressiveReginementSegmentEnd()
    {
    }
    private void MotionConstrainedSliceGroupSet()
    {
    }

    private void ReservedSEIMessage()
    {
    }
  }
}
