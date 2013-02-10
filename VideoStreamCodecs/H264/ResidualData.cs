using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  class ResidualData
  {
    private delegate void Residual(byte coefflevel, byte maxNumCoeff);
    private Residual ResidualBlock;

    private bool _bEntropyCodingModeFlag;

    public ResidualData(bool entropyCodingModeFlag)
    {
      _bEntropyCodingModeFlag = entropyCodingModeFlag;
    }

    public void Read(BitReader bitReader)
    {
      if (_bEntropyCodingModeFlag)
        ResidualBlock = ResidualBlockCABAC;
      else
        ResidualBlock = ResidualBlockCAVLC;
    }

    private void ResidualBlockCABAC(byte coeffLevel, byte maxNumCoeff)
    {
    }

    private void ResidualBlockCAVLC(byte coefflevel, byte maxNumCoeff)
    {
    }
  }
}
