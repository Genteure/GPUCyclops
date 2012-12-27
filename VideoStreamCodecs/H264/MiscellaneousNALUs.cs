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

    public SupplementatlEnhancementMessage(uint size)
      : base((byte)0, NALUnitType.SupplementalEnhancementInfo, size)
    {
    }

    public override void Read(BitReader bitReader)
    {
      base.Read(bitReader);

      do
      {
        SEIMessage(bitReader);
      } while (MoreRBSPData(bitReader));
      SkipToEndOfNALU(bitReader);
    }

    private void SEIMessage(BitReader bitReader)
    {
      byte lastTypeByte;
      while (0xFF == (lastTypeByte = bitReader.ReadByte()))
        PayloadType += 255;
      PayloadType += lastTypeByte;
      byte lastSizeByte;
      while (0xFF == (lastSizeByte = bitReader.ReadByte()))
        PayloadSize += 255;
      PayloadSize += lastSizeByte;
      // SEIPayload(PayloadType, PayloadSize);
    }
  }
}
