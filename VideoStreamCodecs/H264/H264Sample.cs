using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  public class H264Sample
  {
    private int _totalSize;

    public H264Sample(int size)
    {
      _totalSize = size;
    }

    public void ParseNalu(BitReader br, int len)
    {
      byte firstByte = br.ReadByte();
      br.Position += (len - 1);
    }
  }
}
