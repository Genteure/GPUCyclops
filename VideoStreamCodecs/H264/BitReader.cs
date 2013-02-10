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

    // Table 9-4, p. 156 of the H264 standard
    byte[][] CBP = new byte[][]
    {
      new byte[2] {47, 0},
      new byte[2] {31, 16},
      new byte[2] {15, 1},
      new byte[2] {0, 2},
      new byte[2] {23, 4},
      new byte[2] {27, 8},
      new byte[2] {29, 32},
      new byte[2] {30, 3},
      new byte[2] {7, 5},
      new byte[2] {11, 10},
      new byte[2] {13, 12},
      new byte[2] {14, 15},
      new byte[2] {39, 47},
      new byte[2] {43, 7},
      new byte[2] {45, 11},
      new byte[2] {46, 13},
      new byte[2] {16, 14},
      new byte[2] {3, 6},
      new byte[2] {5, 9},
      new byte[2] {10, 31},
      new byte[2] {12, 35},
      new byte[2] {19, 37},
      new byte[2] {21, 42},
      new byte[2] {26, 44},
      new byte[2] {28, 33},
      new byte[2] {35, 34},
      new byte[2] {37, 36},
      new byte[2] {42, 40},
      new byte[2] {44, 39},
      new byte[2] {1, 43},
      new byte[2] {2, 45},
      new byte[2] {4, 46},
      new byte[2] {8, 17},
      new byte[2] {17, 18},
      new byte[2] {18, 20},
      new byte[2] {20, 24},
      new byte[2] {24, 19},
      new byte[2] {6, 21},
      new byte[2] {9, 26},
      new byte[2] {22, 28},
      new byte[2] {25, 23},
      new byte[2] {32, 27},
      new byte[2] {33, 29},
      new byte[2] {34, 30},
      new byte[2] {36, 22},
      new byte[2] {40, 25},
      new byte[2] {38, 38},
      new byte[2] {41, 41}
    };

    public BitReader(Stream byteStream)
    {
      _binaryReader = new BinaryReader(byteStream);
      _probeShifter = 1; // byte aligned
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
          if (Position == _binaryReader.BaseStream.Length)
            End = true;
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
          retVal += shifter;
        shifter >>= 1;
      }

      return retVal;
    }

    public uint DecodeUnsignedExpGolomb()
    {
      ushort q = 0;
      while (!GetNextBit()) q++;
      if (q == 0)
        return 0;
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

    public int DecodeMappedIntraExpGolomb()
    {
      uint val = DecodeUnsignedExpGolomb();
      if (val > 47)
        throw new Exception("Bad index for coded block pattern, Intra");
      return (int)CBP[val][0]; // [zero]
    }

    public int DecodeMappedExpGolomb()
    {
      uint val = DecodeUnsignedExpGolomb();
      if (val > 47)
        throw new Exception("Bad index for coded block pattern");
      return (int)CBP[val][1]; // [one]
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
      if (!ByteAligned)
      {
        throw new Exception("BitReader: reading byte at non-aligned position");
      }
      return _binaryReader.ReadByte();
    }

    public byte PeekByte()
    {
      return (byte)_binaryReader.PeekChar();
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
          ; // throw new Exception("BitReader: bit must be zero in trailing RBSP bits");
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
