using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  class MacroBlockLayer
  {
    public uint MBType;
    public byte[] PCMByte;
    public uint CodedBlockPattern;
    public int MBQPDelta;

    public uint CodedBlockPatternLuma { get { return CodedBlockPattern % 16; } }

    public uint CodedBlockPatternChroma { get { return CodedBlockPattern / 16; } }

    const float ChromaFormatFactor = 1.5f;

    private SliceTypes _sliceType;
    private SequenceParameterSet _sps;
    private PictureParameterSet _pps;
    private SliceHeader _header;
    private SliceData _data;
    private BitReader _bitReader;

    // CABAC stuff
    private MBTypeCABACParser _mbTypeParser;
    private CodedBlockPattern _cbp;
    private MBQPDelta _mbQPD;

    private byte[] _numPart = new byte[6] { 1, 2, 2, 4, 4, 1 };

    public MacroBlockLayer(SequenceParameterSet sps, PictureParameterSet pps, SliceHeader header, SliceData data)
    {
      _sliceType = header.SliceType;
      _sps = sps;
      _pps = pps;
      _header = header;
      _data = data;
      _mbTypeParser = _data.MBTypeParser;
      _cbp = _data.CBP;
      _mbQPD = _data.MBQPD;
    }

    public void Read(BitReader bitReader)
    {
      _bitReader = bitReader;
      MBType = _mbTypeParser.GetMacroBlockType(_bitReader);
      if (MBType > 48)
        throw new Exception("MacroBlockLayer: bad macro block type");
      I_MacroBlockType mbt = (I_MacroBlockType)MBType;
      if (mbt == I_MacroBlockType.I_PCM)
      {
        while (!bitReader.ByteAligned)
        {
          if (bitReader.GetNextBit())
            throw new Exception("MacroBlockLayer: bad alignment");
        }
        bitReader.Position += (long)(256 * ChromaFormatFactor); // skip data (don't need it)
      }
      else
      {
        byte mbtype = (byte)MBType;
        if ((MbPartPredMode(mbtype, 0) != MacroBlockPartitionMode.Intra_4x4) &&
          (MbPartPredMode(mbtype, 0) != MacroBlockPartitionMode.Intra_16x16) &&
          (NumMbPart(mbtype) == 4))
        {
          SubMacroBlockPrediction smbp = new SubMacroBlockPrediction(mbtype);
        }
        else
        {
          MacroBlockPrediction mbpred = new MacroBlockPrediction(mbtype, this);
          mbpred.Read(bitReader);
        }
        if (MbPartPredMode(mbtype, 0) != MacroBlockPartitionMode.Intra_16x16)
          CodedBlockPattern = _cbp.GetCodedBlockPattern(bitReader);
        if ((CodedBlockPatternLuma > 0) || (CodedBlockPatternChroma > 0) ||
          (MbPartPredMode(mbtype, 0) == MacroBlockPartitionMode.Intra_16x16))
        {
          MBQPDelta = _mbQPD.GetMBQPDelta(bitReader);
          ResidualData residual = new ResidualData(_pps.EntropyCodingModeFlag);
          residual.Read(bitReader);
        }
      }
    }

    public byte NumMbPart(byte mbtype)
    {
      return _numPart[mbtype];
    }

    public MacroBlockPartitionMode MbPartPredMode(byte mbType, byte field)
    {
      switch (_sliceType)
      {
        case SliceTypes.I:
          if (field > (byte)0)
            throw new Exception("MacroBlockLayer: MbPartPredMode() invalid 2nd param, I");
          if (mbType == (byte)0)
            return MacroBlockPartitionMode.Intra_4x4;
          else if (mbType < (byte)25)
            return MacroBlockPartitionMode.Intra_16x16;
          else
            return MacroBlockPartitionMode.None;
        case SliceTypes.SI:
          if (field > (byte)0)
            throw new Exception("MacroBlockLayer: MbPartPredMode() invalid 2nd param, SI");
          return MacroBlockPartitionMode.Intra_4x4;
        case SliceTypes.P:
        case SliceTypes.SP:
          if (field == (byte)0)
          {
            if ((mbType != (byte)3) && (mbType != (byte)4))
              return MacroBlockPartitionMode.Pred_L0;
          }
          else if (field == (byte)1)
          {
            if ((mbType == (byte)1) || (mbType == (byte)2))
              return MacroBlockPartitionMode.Pred_L0;
          }
          return MacroBlockPartitionMode.None;
        case SliceTypes.B:
          if (field == (byte)0)
          {
            if (mbType == 22)
              return MacroBlockPartitionMode.None;
            if ((mbType == 0) || (mbType > 22))
              return MacroBlockPartitionMode.Direct;
            if ((mbType == 1) || (mbType == 4) || (mbType == 5) ||
              (mbType == 8) || (mbType == 9) || (mbType == 12) || (mbType == 13))
              return MacroBlockPartitionMode.Pred_L0;
            if ((mbType == 2) || (mbType == 6) || (mbType == 7) ||
              (mbType == 10) || (mbType == 11) || (mbType == 14) || (mbType == 15))
              return MacroBlockPartitionMode.Pred_L1;
            if ((mbType >= 16) && (mbType <= 21))
              return MacroBlockPartitionMode.BitPred;
          }
          else if (field == (byte)1)
          {
            if (((mbType >= 0) && (mbType <= 3)) || (mbType > 21))
              return MacroBlockPartitionMode.None;
            if ((mbType == 4) || (mbType == 5) ||
              (mbType == 10) || (mbType == 11) || (mbType == 16) || (mbType == 17))
              return MacroBlockPartitionMode.Pred_L0;
            if ((mbType == 6) || (mbType == 7) ||
              (mbType == 8) || (mbType == 9) || (mbType == 18) || (mbType == 19))
              return MacroBlockPartitionMode.Pred_L1;
            if ((mbType == 12) || (mbType == 13) ||
              (mbType == 14) || (mbType == 15) || (mbType == 20) || (mbType == 21))
              return MacroBlockPartitionMode.BitPred;
          }
          return MacroBlockPartitionMode.None;
        default:
          break; // should be unexecuted
      }
      return MacroBlockPartitionMode.None;
    }
  }
}
