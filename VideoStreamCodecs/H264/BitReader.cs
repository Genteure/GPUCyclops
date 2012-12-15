using System;
using System.IO;
using System.Net;

namespace Media.H264
{
  /// <summary>
  /// SyntaxElement enum from Table 9-11 of the H264 Standard.
  /// </summary>
  public enum SyntaxElement
  {
    MBSkipFlag = 0,
    MBFieldDecodingFlag,
    MBType,
    LumaCodedBlockPattern,
    ChromaCodedBlockPattern,
    MBQPDelta,
    PrevIntra4x4PredModeFlag,
    RemIntra4x4PredMode,
    IntraChromaPredMode,
    RefIdxI0,
    RefIdxI1,
    MVDI00,
    MVDI10,
    MVDI01,
    MVDI11,
    SubMBType,
    CodedBlockFlag,
    SignificantCoeffFlag,
    LastSignificantCoeffFlag,
    CoeffAbsLevelMinus1
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

    public bool ByteAligned { get { return (_probeShifter == 1); } }

    public long Position
    {
      get { return _binaryReader.BaseStream.Position; }
      set { _binaryReader.BaseStream.Position = value; }
    }

    public bool GetNextBit()
    {
      _probeShifter = (byte)((int)_probeShifter >> 1);
      if (_probeShifter == 0)
      {
        try
        {
          if (End) // if we've already read beyond the end, don't go any further
            throw new Exception("BitReader: attempt to read beyond end for the second time");
          _currByte = _binaryReader.ReadByte();
        }
        catch (Exception ex)
        {
          if (ex is EndOfStreamException)
          {
            End = true;
            _currByte = 0x80;
          }
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

    public int DecodeCABAC(SliceHeader header, SyntaxElement se)
    {
      // FIXME: do nothing for now
      return 0;
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

    public void Close()
    {
      _binaryReader.Close();
    }

    public void DiscardTrailingBits()
    {
      if (GetNextBit() != true)
        throw new Exception("BitReader: bad trailing RBSP bits");
      while (_probeShifter > 1)
      {
        if (GetNextBit() == true)
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
