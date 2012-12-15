using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Linq;
using Media.Formats.JPGFileSequence;
using Media.Formats.MicrosoftFMP4;
using Media.Formats.MP4;
using Media.Formats.QBOX;
using Media.Formats.Generic;
using System.Collections.Generic;

namespace Media.Formats {
  public enum MediaStreamHandlersTypes { unknown, mp4, qbox, ismv, odt, ism }

  public class GenericMediaStreamFactory {
	 //static FileInfo MediaFileInfo;
	 //static IMediaStream igms;
    static Assembly asm;
    static string path;

    /// <summary>
    /// ExecutableDir
    /// The ExecutableDir is the location of the assembly being loaded.
    /// The assembly being loaded is assumed to be in the same folder/directory as the other
    /// assemblies in the program.
    /// </summary>
    public static string ExecutableDir {
      get {
        Assembly thisAsm = Assembly.GetExecutingAssembly();
        return Path.GetDirectoryName(thisAsm.Location);
      }
    }

    public static IMediaStream Create(string inMediaFileName, FileMode mode, bool cachingEnabled /*false*/) {
      path = inMediaFileName;
      return (Create(new FileInfo(inMediaFileName), mode, cachingEnabled));
    }

	 public static IMediaStream Create(FileInfo inFileInfo, FileMode mode, bool cachingEnabled /*false*/)
	 {
		 GenericCacheManager.MediaFileInfo = inFileInfo; /*MediaFileInfo =*/ 

		 if (mode == FileMode.Open && inFileInfo.Exists == false)
			 throw new Exception(inFileInfo.FullName + " does not exist.");

		 FileStream fs;
		 if (mode == FileMode.Open)
		 {
			 fs = inFileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
		 }
		 else
		 {
			 fs = inFileInfo.Open(mode, FileAccess.ReadWrite, FileShare.ReadWrite);
		 }

		 return Create(fs, inFileInfo.Extension, mode, cachingEnabled);
	 }

	 /// <summary>
	 /// Made a small change to this method so that it can work directly using the Stream instance.
	 /// </summary>
	 /// <param name="fileExtension">Extension of the input/output file, for ex. ".h3d" or ".mp4"</param>
	 public static IMediaStream Create(Stream fs, string fileExtension, FileMode mode, bool cachingEnabled)
	 {
		 IMediaStream igms = null;
      switch (fileExtension.ToLower()) {
        case ".ism":
        case ".ismv":
          //#if SILVERLIGHT
          //                    LoadAssembly("ISMVHandler");
          //#else
          //                    LoadAssembly("ISMVHandlerWin");
          //#endif
          //if (mode == FileMode.Open) {
          //  igms = (GenericMediaStream)asm.CreateInstance("MediaHandling.ISMVHandler.ISMVStreamReader");
          //  igms.Open(fs, cachingEnabled);
          //} else {
          //  igms = (GenericMediaStream)asm.CreateInstance("MediaHandling.ISMVHandler.ISMVStreamWriter");
          //  igms.Create(fs, cachingEnabled);
          //}
          if (mode == FileMode.Open) {
            fs.Close(); // we don't need it in this case
            igms = (IMediaStream)(new ISMVStream());
            igms.Open(path, cachingEnabled);
          } else {
            throw new Exception("ISM stream creation unsupported for now");
          }
        break;
        case ".3gp":
        case ".mp4":
          if (mode == FileMode.Open) {
            igms = (IMediaStream)(new MP4StreamReader());
            igms.Open(fs, cachingEnabled);
          } else {
            igms = (IMediaStream)(new MP4StreamWriter());
            igms.Create(fs, cachingEnabled);
          }
        break;
        case ".qbox":
          if (mode == FileMode.Open) {
            igms = (IMediaStream)(new QBoxStream());
            igms.Open(fs, cachingEnabled);
          } else {
            igms = (IMediaStream)(new QBoxStream());
            igms.Create(fs, cachingEnabled);
          }
        break;
        case ".jpg":
          if (mode == FileMode.Open) {
            igms = (IMediaStream)(new FileSequence());
            //igms.Open(fs); // noop
          } else {
            igms = (IMediaStream)(new FileSequence());
            //igms.Create(fs);
          }
        break;
        default:
          throw new Exception("Unknown media file extension");
      }
      return (igms);
    }

    // Load assembly and find asset and fragment formatters there
    // FIXME: All available constructors must be set (see ConstructorInfo declarations above).
    static void LoadAssembly(string fname) {
#if SILVERLIGHT
          asm = Assembly.Load(string.Format("{0}, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", fname));
#else
      // try to load assembly
      asm = Assembly.Load(fname);
#endif
    }
  }

}
