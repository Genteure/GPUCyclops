using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Formats
{
  public interface IFragment : IEnumerable<Slice>
  {
    int Length
    {
      get;
    }

    int FirstIndex
    {
      get;
    }

    // this accessor needs to be overridden in derived class
    Slice this[int index]
    {
      get;
      set;
    }

    //void Read();
  }
}
