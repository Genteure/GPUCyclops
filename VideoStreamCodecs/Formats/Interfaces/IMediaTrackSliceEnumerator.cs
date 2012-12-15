using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Formats
{
  public interface IMediaTrackSliceEnumerator : IEnumerator<Slice>
  {
    CodecTypes CodecType
    {
      get;
    }

    ulong? CurrentTimeStampNew
    {
      get;
    }

    void SetCurrent(Slice slice);
  }
}
