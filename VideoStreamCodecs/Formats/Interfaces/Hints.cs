using System;
using System.Net;

namespace Media
{
  /// <summary>
  /// What are Hints? It's just a list of variables passed from the input stream reader to the output stream writer during
  /// recoding from one format to another.
  /// </summary>
  public class Hints
  {
    public uint StreamTimeScale = 1000U; // set to desired time scale in output 
    public string[] CompatibleBrands; // MP4 specific
    public object object1; // something
    public object object2; // another something (both of these are kludges that need to be removed)
  }
}
