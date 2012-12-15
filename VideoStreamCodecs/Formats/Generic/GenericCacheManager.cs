using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Media.Formats.QBOX;

namespace Media.Formats.Generic
{
  /// <summary>
  /// GenericCacheManager
  /// The whole purpose of this is to allow for CacheManagers that are each specific to a format.
  /// MediaFileInfo should be set by GenericMediaStreamFactory.
  /// </summary>
  public class GenericCacheManager
  {
    public static FileInfo MediaFileInfo;

    public static PerTrackCacheManager CreateCacheManager()
    {
      if (MediaFileInfo == null)
        throw new Exception("Cannot create cache manager without input file");

      PerTrackCacheManager cacheMgr = null;

      switch (MediaFileInfo.Extension.ToLower())
      {
        case ".ism":
        case ".ismv":
          cacheMgr = (PerTrackCacheManager)(new PerTrackCacheManager());
          break;
        case ".3gp":
        case ".mp4":
          //asm = Assembly.GetExecutingAssembly();
          //cacheMgr = (PerTrackCacheManager)asm.CreateInstance("MediaHandling.PerTrackCacheManager");
          cacheMgr = (PerTrackCacheManager)(new PerTrackCacheManager());
          break;
        case ".qbox":
          cacheMgr = (PerTrackCacheManager)(new FlashbackCacheManager());
          break;
        case ".jpg":
          break;
        default:
          throw new Exception("Unknown media file extension");
      }

      return cacheMgr;
    }
  }
}
