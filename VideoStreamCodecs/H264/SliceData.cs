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

    bool CABACAlignmentOneBit;
    uint CurrMbAddr; // current macro block address
    bool moreDataFlag;
    bool prevMbSkipped; // previous macro block skipped?
    uint MBSkipRun;

    public SliceData(SequenceParameterSet sps, PictureParameterSet pps, SliceHeader header)
    {
      _sps = sps;
      _pps = pps;
      _header = header;
    }

    public void Read(BitReader bitReader)
    {
      if (_pps.EntropyCodingModeFlag)
      {
        while (!bitReader.ByteAligned)
          CABACAlignmentOneBit = bitReader.GetNextBit();
      }

      //CurrMbAddr = _header.FirstMBInSlice * (1u + (_header.MBaffFrameFlag ? 1u : 0u));
      //moreDataFlag = true;
      //prevMbSkipped = false;
      //do
      //{
      //  if ((_header.SliceType != SliceTypes.I) && (_header.SliceType != SliceTypes.SI))
      //  {
      //    if (!_pps.EntropyCodingModeFlag)
      //    {
      //      MBSkipRun = bitReader.DecodeUnsignedExpGolomb();
      //      prevMbSkipped = (MBSkipRun > 0);
      //      for (int i = 0; i < MBSkipRun; i++)
      //      {
      //        CurrMbAddr = NextMBAddress(CurrMbAddr);
      //      }
      //      moreDataFlag = _header.MoreRBSPData(bitReader);
      //    }
      //    else
      //    {
      //      CABACParser cabac = new CABACParser();
      //      //MBSkipFlag = bitReader.DecodeCABAC(_header.SliceType, SyntaxElement.MBSkipFlag);
      //    }
      //  }
      //} while (moreDataFlag);
    }

    // See Section 8.2.2 of H264 Spec, equation 8-17
    // FIXME: incomplete
    private uint NextMBAddress(uint addr)
    {
      uint i = addr + 1;
      //while ((i < _sps.PicSizeInMBs) && (_pps.map
      return 0;
    }
  }
}
