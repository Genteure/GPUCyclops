using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  /// <summary>
  /// MBTypeCABACParser
  /// One instance of this is created for every slice.
  /// </summary>
  public class MBTypeCABACParser : CABACBaseClass
  {
    private bool _firstMacroBlock; // if true, no macroblock has been processed for slice

    public MBTypeCABACParser(PictureParameterSet pps, SliceHeader header) : base(pps, header)
    {
      _firstMacroBlock = true;
    }

    private void MainInit(BitReader bitReader)
    {
      _bitReader = bitReader;
      if (_firstMacroBlock)
      {
        InitContextVariables(276);
        InitDecodeEngine();
      }
    }

    void InitContextVariables(int ctxIdx)
    {
      if (ctxIdx == 276)
      {
        pStateIdx = 63;
        valMPS = 0;
        return;
      }

      byte initIdc = _header.CABACInitIDC;
      if (((ctxIdx < 11) && (initIdc != 0)) || (initIdc > 4))
        throw new Exception("MBTypeCABACParser: invalid initIdc");
      MN tmn = CABACTables.T9[ctxIdx, initIdc];
      int preCtxState = Clip3(1, 126, ((tmn.m * _header.SliceQPy) >> 4) + tmn.n);
      if (preCtxState <= 63)
      {
        pStateIdx = 63 - preCtxState;
        valMPS = 0;
      }
      else
      {
        pStateIdx = preCtxState - 64;
        valMPS = 1;
      }
    }

    void InitDecodeEngine()
    {
      codIRange = 0x01FE;
      codIOffset = (short)_bitReader.GetUIntFromNBits(9);
    }


    public uint GetMacroBlockType(BitReader bitReader)
    {
      MainInit(bitReader);
      if (_pps.EntropyCodingModeFlag)
        switch (_header.SliceType)
        {
          case SliceTypes.SI:
            maxBinIdxCtx_p = 0; // prefix
            ctxIdxOffset_p = 0; // prefix
            maxBinIdxCtx_s = 6; // suffix
            ctxIdxOffset_s = 3; // suffix
            return GetSIMacroBlockType();
          case SliceTypes.I:
            maxBinIdxCtx_p = 0; // prefix
            ctxIdxOffset_p = 0; // prefix
            maxBinIdxCtx_s = 6; // suffix only
            ctxIdxOffset_s = 3; // suffix only
            return GetIMacroBlockType();
          case SliceTypes.P:
          case SliceTypes.SP:
            maxBinIdxCtx_p = 2; // prefix
            ctxIdxOffset_p = 14; // prefix
            maxBinIdxCtx_s = 5; // suffix
            ctxIdxOffset_s = 17; // suffix
            return GetPnSPMacroBlockType();
          case SliceTypes.B:
            maxBinIdxCtx_p = 3; // prefix
            ctxIdxOffset_p = 27; // prefix
            maxBinIdxCtx_s = 5; // suffix
            ctxIdxOffset_s = 32; // suffix
            return GetBMacroBlockType();
          default:
            throw new Exception("MacroBlockLayer: bad slice type"); ;
        }
      else
        return _bitReader.DecodeUnsignedExpGolomb();
    }

    private uint GetSIMacroBlockType()
    {
      int ctxIdx = GetCtxIdx(0, maxBinIdxCtx_p, ctxIdxOffset_p);
      bool b = DecodeBin(ctxIdx);
      if (!b)
        return 0;
      return GetIMacroBlockType() + 1;
    }

    private uint GetIMacroBlockType()
    {
      short state = 0;
      for (int binIdx = 0; binIdx < maxBinIdxCtx_s; binIdx++)
      {
        int ctxIdx = GetCtxIdx(binIdx, maxBinIdxCtx_s, ctxIdxOffset_s);
        bool b = DecodeBin(ctxIdx);
        switch (state)
        {
          case 0:
            if (b) state = 1; // b0
            else return 0;
            break;
          case 1:
            if (b) return 25; // b1
            else state = 2;
            break;
          case 2:
            if (b) state = 4; // b2
            else state = 3;
            break;
          case 3:
            if (b) state = 6; // b3
            else state = 5;
            break;
          case 4:
            if (b) state = 8; // b3
            else state = 7;
            break;
          case 5:
            if (b) state = 10; // b4
            else state = 9;
            break;
          case 6:
            if (b) state = 12; // b4
            else state = 11;
            break;
          case 7:
            if (b) state = 14; // b4
            else state = 13;
            break;
          case 8:
            if (b) state = 16; // b4
            else state = 15;
            break;
          case 9:
            if (b) return 2; // b5   <-- 1 & 2
            else return 1;
          case 10:
            if (b) return 4; // b5   <-- 3 & 4
            else return 3;
          case 11:
            if (b) state = 18; // b5
            else state = 17;
            break;
          case 12:
            if (b) state = 20; // b5
            else state = 19;
            break;
          case 13:
            if (b) return 14; // b5  <-- 13 & 14
            else return 13;
          case 14:
            if (b) return 16; // b5  <-- 15 & 16
            else return 15;
          case 15:
            if (b) state = 22; // b5
            else state = 21;
            break;
          case 16:
            if (b) state = 24; // b5
            else state = 23;
            break;
          case 17:
            if (b) return 6;  // b6  <-- 5 & 6
            else return 5;
          case 18:
            if (b) return 8;  // b6  <-- 7 & 8
            else return 7;
          case 19:
            if (b) return 10; // b6  <-- 9 & 10
            else return 9;
          case 20:
            if (b) return 12; // b6  <-- 11 & 12
            else return 11;
          case 21:
            if (b) return 18; // b6  <-- 17 & 18
            else return 17;
          case 22:
            if (b) return 20; // b6  <-- 19 & 20
            else return 19;
          case 23:
            if (b) return 22; // b6  <-- 21 & 22
            else return 21;
          case 24:
            if (b) return 24; // b6  <-- 23 & 24
            else return 23;
          default:
            throw new Exception("MBTypeCABACParser: bad state, I slice");
        }
      }
      throw new Exception("MBTypeCABACParser: no match (I)");
    }

    private uint GetPnSPMacroBlockType()
    {
      short state = 0;
      for (int binIdx = 0; binIdx < maxBinIdxCtx_p; binIdx++)
      {
        int ctxIdx = GetCtxIdx(binIdx, maxBinIdxCtx_p, ctxIdxOffset_p);
        bool b = DecodeBin(ctxIdx);
        switch (state)
        {
          case 0:
            if (b) return GetIMacroBlockType() + 5; // b0
            else state = 1;
            break;
          case 1:
            if (b) state = 3;  // b1
            else state = 2;
            break;
          case 2:
            if (b) return 3;   // b2
            else return 0;
          case 3:
            if (b) return 1;   // b2
            else return 2;
          default:
            throw new Exception("MBTypeCABACParser: invalid state in GetPnSPMacroBlockType");
        }
      }
      throw new Exception("MBTypeCABACParser: no match in GetPnSPMacroBlockType");
    }

    private uint GetBMacroBlockType()
    {
      short state = 0;
      for (int binIdx = 0; binIdx < maxBinIdxCtx_p; binIdx++)
      {
        int ctxIdx = GetCtxIdx(binIdx, maxBinIdxCtx_p, ctxIdxOffset_p);
        bool b = DecodeBin(ctxIdx);
        switch (state)
        {
          case 0:
            if (b) state = 1; // b0
            else return 0;
            break;
          case 1:
            if (b) state = 3; // b1  11
            else state = 2;   //     10
            break;
          case 2:
            if (b) return 2;  // b2  <-- 1 & 2
            else return 1;
          case 3:
            if (b) state = 5; // b2  111
            else state = 4;   //     110
            break;
          case 4:
            if (b) state = 7; // b3  1101
            else state = 6;   //     1100
            break;
          case 5:
            if (b) state = 9; // b3  1111
            else state = 8;   //     1110
            break;
          case 6:
            if (b) state = 11; // b4 11001
            else state = 10;   //    11000
            break;
          case 7:
            if (b) state = 13; // b4 11011
            else state = 12;   //    11010
            break;
          case 8:
            if (b) state = 15; // b4 11101
            else state = 14;   //    11100
            break;
          case 9:
            if (b) state = 17; // b4 11111
            else state = 16;   //    11110
            break;
          case 10:
            if (b) return 4;  // b5   <-- 3 & 4
            else return 3;
          case 11:
            if (b) return 6;  // b5  <-- 5 & 6
            else return 5;
          case 12:
            if (b) return 8;  // b5  <-- 7 & 8
            else return 7;
          case 13:
            if (b) return 10; // b5  <-- 9 & 10
            else return 9;
          case 14:
            if (b) state = 19; // b5  111001
            else state = 18;   //     111000
            break;
          case 15:
            if (b) state = 21; // b5  111011
            else state = 20;   //     111010
            break;
          case 16:
            if (b) return GetIMacroBlockType() + 23; // b5 111101 --> out
            else state = 22;   //    111100
            break;
          case 17:
            if (b) return 22;  // b6  <-- 22
            else return 11;     //    <-- 11
          case 18:
            if (b) return 13;  // b6  <-- 12 & 13
            else return 12;
          case 19:
            if (b) return 15; // b6  <-- 14 & 15
            else return 14;
          case 20:
            if (b) return 17; // b6  <-- 16 & 17
            else return 16;
          case 21:
            if (b) return 19; // b6  <-- 18 & 19
            else return 18;
          case 22:
            if (b) return 21; // b6  <-- 20 & 21
            else return 20;
          default:
            throw new Exception("MBTypeCABACParser: bad state, B slice"); ;
        }
      }
      throw new Exception("MBTypeCABACParser: no match (B)");
    }

    private int GetCtxIdx(int binIdx, int maxBinIdxCtx, int ctxIdxOffset)
    {
      byte initIdc = _header.CABACInitIDC;
      int ctxIdx = 0;
      switch (binIdx)
      {
        case 0:
          if ((ctxIdxOffset == 0) || (ctxIdxOffset == 3) || (ctxIdxOffset == 27))
            ctxIdx = ctxIdxOffset + GetContextFromNeighbors(ctxIdxOffset);
          else if ((ctxIdxOffset == 14) || (ctxIdxOffset == 17) || (ctxIdxOffset == 32))
            ctxIdx = ctxIdxOffset;
          else
            throw new Exception("MBTypeCABACParser: when binIdx == 0, ctxIdxOffset cannot be " + ctxIdxOffset);
          break;
        case 1:
          switch (ctxIdxOffset)
          {
            case 3:
            case 17:
            case 32:
              ctxIdx = 276;
              break;
            case 14:
              ctxIdx = ctxIdxOffset + 1;
              break;
            case 27:
              ctxIdx = ctxIdxOffset + 3;
              break;
            default:
              throw new Exception("MBTypeCABACParser: bad ctxIdxOffset when binIdx == 1");
          }
          break;
        case 2:
          if (ctxIdxOffset == 3)
            ctxIdx = ctxIdxOffset + 3;
          else if ((ctxIdxOffset == 14) || (ctxIdxOffset == 27))
            ctxIdx = ctxIdxOffset + GetContextFromPrevious(ctxIdxOffset);
          else if ((ctxIdxOffset == 17) || (ctxIdxOffset == 32))
            ctxIdx = ctxIdxOffset + 1;
          else
            throw new Exception("MBTypeCABACParser: when binIdx == 2, ctxIdxOffset cannot be " + ctxIdxOffset);
          break;
        case 3:
          if (ctxIdxOffset == 3) ctxIdx = ctxIdxOffset + 4;
          else if (ctxIdxOffset == 17) ctxIdx = ctxIdxOffset + 2;
          else if (ctxIdxOffset == 27) ctxIdx = ctxIdxOffset + 5;
          else if (ctxIdxOffset == 32) ctxIdx = ctxIdxOffset + 2;
          else
            throw new Exception("MBTypeCABACParser: when binIdx == 3, ctxIdxOffset cannot be " + ctxIdxOffset);
          break;
        case 4:
          if (ctxIdxOffset == 27) ctxIdx = ctxIdxOffset + 5;
          else if ((ctxIdxOffset == 3) || (ctxIdxOffset == 17) || (ctxIdxOffset == 32))
            ctxIdx = ctxIdxOffset + GetContextFromPrevious(ctxIdxOffset);
          else
            throw new Exception("MBTypeCABACParser: when binIdx == 4, ctxIdxOffset cannot be " + ctxIdxOffset);
          break;
        case 5:
          if (ctxIdxOffset == 3)
            ctxIdx = ctxIdxOffset + GetContextFromPrevious(ctxIdxOffset);
          else if ((ctxIdxOffset == 17) || (ctxIdxOffset == 32))
            ctxIdx = ctxIdxOffset + 3;
          else if (ctxIdxOffset == 27)
            ctxIdx = ctxIdxOffset + 5;
          else
            throw new Exception("MBTypeCABACParser: when binIdx == 5, ctxIdxOffset cannot be " + ctxIdxOffset);
          break;
        case 6:
          if ((ctxIdxOffset == 17) || (ctxIdxOffset == 32))
            ctxIdx = ctxIdxOffset + 3;
          else if (ctxIdxOffset == 3)
            ctxIdx = ctxIdxOffset + 7;
          else if (ctxIdxOffset == 27)
            ctxIdx = ctxIdxOffset + 5;
          else
            throw new Exception("MBTypeCABACParser: when binIdx == 6, ctxIdxOffset cannot be " + ctxIdxOffset);
          break;
        default:
          throw new Exception("MBTypeCABACParser: invalid binIdx");
      }

      if (_firstMacroBlock)
      {
        _firstMacroBlock = false;
      }

      return ctxIdx;
    }

    private bool DecodeBin(int ctxIdx)
    {
      byte initIdc = _header.CABACInitIDC;
      return false;
    }

    /// <summary>
    /// GetContextFromNeighbors - Section 9.3.3.1.1.3
    /// </summary>
    /// <param name="ctxIdxOffset"></param>
    /// <returns></returns>
    private int GetContextFromNeighbors(int ctxIdxOffset)
    {
      return 0;
    }

    /// <summary>
    /// GetContextFromPrevious - Section 9.3.3.1.2
    /// </summary>
    /// <param name="ctxIdxOffset"></param>
    /// <returns></returns>
    private int GetContextFromPrevious(int ctxIdxOffset)
    {
      return 0;
    }
  }
}
