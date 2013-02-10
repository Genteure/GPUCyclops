using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  public class SliceData
  {
    private SequenceParameterSet _sps;
    private PictureParameterSet _pps;
    private SliceHeader _header;
    private NetworkAbstractionLayerUnit _nalu;

    public MBTypeCABACParser MBTypeParser
    {
      get;
      private set;
    }

    public CodedBlockPattern CBP
    {
      get;
      private set;
    }

    public MBQPDelta MBQPD
    {
      get;
      private set;
    }

    bool CABACAlignmentOneBit;
    uint CurrMbAddr; // current macro block address
    bool moreDataFlag;
    bool prevMbSkipped; // previous macro block skipped?
    uint MBSkipRun;
    bool mbSkipFlag;
    bool mbFieldDecodingFlag;
    bool endOfSliceFlag;

    public SliceData(SequenceParameterSet sps, PictureParameterSet pps, SliceHeader header)
    {
      _sps = sps;
      _pps = pps;
      _header = header;
      if (_pps.EntropyCodingModeFlag)
      {
        MBTypeParser = new MBTypeCABACParser(_pps, header);
        CBP = new CodedBlockPattern(_pps, _header);
        MBQPD = new MBQPDelta(_pps, _header);
      }
    }

    public SliceData(SequenceParameterSet sps, PictureParameterSet pps, SliceHeader header, NetworkAbstractionLayerUnit nalu) : this(sps, pps, header)
    {
      _nalu = nalu;
    }

    public void Read(BitReader bitReader)
    {
      if (_pps.EntropyCodingModeFlag)
      {
        while (!bitReader.ByteAligned)
          if ((CABACAlignmentOneBit = bitReader.GetNextBit()) == false)
            throw new Exception("Invalid entropy coding start for slice data");
      }

      CurrMbAddr = _header.FirstMBInSlice * (1u + (_header.MBaffFrameFlag ? 1u : 0u));
      moreDataFlag = true;
      prevMbSkipped = false;
      do
      {
        if ((_header.SliceType != SliceTypes.I) && (_header.SliceType != SliceTypes.SI))
        {
          if (!_pps.EntropyCodingModeFlag)
          {
            MBSkipRun = bitReader.DecodeUnsignedExpGolomb();
            prevMbSkipped = (MBSkipRun > 0);
            for (int i = 0; i < MBSkipRun; i++)
            {
              CurrMbAddr = NextMBAddress(CurrMbAddr);
            }
            moreDataFlag = _nalu.MoreRBSPData(bitReader);
          }
          else
          {
            mbSkipFlag = false; // FIXME: CABAC value bitReader.GetNextBit();
            moreDataFlag = !mbSkipFlag;
          }
        }

        if (moreDataFlag)
        {
          if (_header.MBaffFrameFlag && (CurrMbAddr % 2 == 0 || (CurrMbAddr % 2 == 1 && prevMbSkipped)))
          {
            mbFieldDecodingFlag = false; // FIXME: CABAC value bitReader.GetNextBit();
          }
          MacroBlockLayer mbLayer = new MacroBlockLayer(_sps, _pps, _header, this);
          mbLayer.Read(bitReader);
        }

        if (!_pps.EntropyCodingModeFlag)
          moreDataFlag = _nalu.MoreRBSPData(bitReader);
        else
        {
          if (_header.SliceType != SliceTypes.I && _header.SliceType != SliceTypes.SI)
            prevMbSkipped = mbSkipFlag;
          if (_header.MBaffFrameFlag && CurrMbAddr % 2 == 0)
            moreDataFlag = true;
          else
          {
            endOfSliceFlag = bitReader.GetNextBit(); // FIXME: random bit bec. residual is not read
            moreDataFlag = !endOfSliceFlag;
          }
        }

        CurrMbAddr = NextMBAddress(CurrMbAddr);

      } while (moreDataFlag);
    }

    // See Section 8.2.2 of H264 Spec, equation 8-17
    // FIXME: incomplete
    private uint NextMBAddress(uint addr)
    {
      uint i = addr + 1;

      if (_pps.NumSliceGroupsMinus1 > 0)
      {
        while (i < _sps.PicSizeInMBs && !_header.MBlocksInSameSlice(i, addr)) i++;
      }

      return i;
    }
  }
}
