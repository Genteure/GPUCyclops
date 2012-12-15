using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Media.Formats.Generic
{
  public class GenericFragment : IFragment
  {
    protected Stream readStream;

    public int Length
    {
      get;
      protected set;
    }

    public int FirstIndex
    {
      get;
      private set;
    }

    public GenericFragment()
    {
    }

    public GenericFragment(Stream stream)
    {
      readStream = stream;
    }

    // this accessor needs to be overridden in derived class
    public virtual Slice this[int index]
    {
      get { throw new Exception("Getter for GenericFragment not implemented in derived class"); }
      set { throw new Exception("Setter for GenericFragment not implemented in derived class"); }
    }

    public IEnumerator GetEnumerator()
    {
      return ((IEnumerator)new GenericFragmentEnumerator(this));
    }

    //public virtual void Read()
    //{
    //  throw new NotImplementedException("Read with params must be implemented");
    //}

    IEnumerator<Slice> IEnumerable<Slice>.GetEnumerator()
    {
      return ((IEnumerator<Slice>)GetEnumerator());
    }
  }

  public class GenericFragmentEnumerator : IEnumerator<Slice>
  {
    GenericFragment fragment;
    int currentIndex = -1;

    public GenericFragmentEnumerator(GenericFragment inFrag)
    {
      fragment = inFrag;
    }

    public void Reset() { currentIndex = -1; }
    Slice IEnumerator<Slice>.Current { get { return (fragment[currentIndex]); } }
    object IEnumerator.Current { get { return ((object)fragment[currentIndex]); } }
    public bool MoveNext()
    {
      currentIndex++;
      if (currentIndex >= fragment.Length) return (false);
      return (true);
    }

    public void Dispose()
    {
      fragment = null;
    }
  }
}
