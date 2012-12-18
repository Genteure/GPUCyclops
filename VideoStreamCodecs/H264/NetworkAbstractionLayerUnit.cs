using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  public enum NALUnitType
  {
    Unspecified = 0,
    NonIDRSlice,
    SlicePartitionA,
    SlicePartitionB,
    SlicePartitionC,
    IDRSlice,
    SupplementalEnhancementInfo, // SEI
    SequenceParamSet,
    PictureParamSet,
    AccessUnitDelimiter,
    EndOfSequence,
    EndOfStream,
    FillerData,
    Reserved0,
    Reserved1,
    Reserved2,
    Reserved3,
    Reserved4,
    Reserved5,
    Reserved6,
    Reserved7,
    Reserved8,
    Reserved9,
    Reserved10
  }

  /// <summary>
  /// NetworkAbstractionLayerUnit (NALU)
  /// The size is intentionally not read from serialized bits so that we can use this same base class
  /// for both NALU stream formatted (with byte count) and byte stream formatted data (no byte count).
  /// Implemented by CCT.
  /// </summary>
  public class NetworkAbstractionLayerUnit
  {
    private long _positionInStream; // for size verification

    public Byte NALRefIDC { get; private set; }
    public NALUnitType NALUType { get; private set; }
    public uint NumBytes { get; private set; } // byte count

    public NetworkAbstractionLayerUnit()
    {
      NALRefIDC = 0;
      NALUType = NALUnitType.Unspecified;
      NumBytes = 0u;
    }

    public NetworkAbstractionLayerUnit(uint size) : this()
    {
      NumBytes = size;
    }

    public NetworkAbstractionLayerUnit(byte idc, NALUnitType naluType, uint size) : this(size)
    {
      NALRefIDC = idc;
      NALUType = naluType;
    }

    // start reading at a byte boundary
    public virtual void Read(BitReader bitReader)
    {
      _positionInStream = bitReader.Position;
      Byte firstByte = bitReader.ReadByte();
      Byte idc = (Byte)(firstByte >> 5);
      if ((idc & NALRefIDC) != idc) // if it's not as expected, then throw exception
        throw new Exception("NALU base class: unexpected Ref IDC");
      NALUnitType naluType = (NALUnitType)(firstByte & 0x1F);
      if (naluType != NALUType)
        throw new Exception("NALU base class: unexpected NALU type");
    }

    protected void CheckSize(BitReader bitReader)
    {
      if (NumBytes != (uint)(bitReader.Position - _positionInStream))
        throw new Exception("NALU: CheckSize NOT ok");
    }

    public void SkipToEndOfNALU(BitReader bitReader)
    {
      bitReader.Position = _positionInStream + NumBytes;
    }

    public bool MoreRBSPData(BitReader bitReader)
    {
      long end = _positionInStream + NumBytes;
      if (bitReader.Position < end)
        return true;
      bitReader.DiscardTrailingBits();
      return false;
    }
  }
}
