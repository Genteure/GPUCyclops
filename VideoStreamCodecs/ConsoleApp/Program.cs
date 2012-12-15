using System;
using System.IO;
using System.Reflection;
using System.Linq;
using Media;
using Media.Formats.Generic;
using Media.Formats;
using Driver;
using System.Threading;

namespace ConsoleApp
{
  class Program
  {
    static bool IsOneOf(char c, char[] carray)
    {
      foreach (char cc in carray)
      {
        if (c == cc)
          return true;
      }
      return false;
    }

    public static void PrintUsage()
    {
      Console.WriteLine("Converts a portion of a video input file to an ouput file of a different or same format. (Available formats listed below.)");
      Console.WriteLine("");
      Console.WriteLine("RecodingFilter [input file] [start time] [end time] [output file to create] [trackid] [vo|so] [cts]");
      Console.WriteLine("");
      Console.WriteLine("If [trackid] is zero, then ALL video tracks in the input file are included.");
      Console.WriteLine("If [end time] is zero, then the utility converts up to the end of the input file.");
      Console.WriteLine("If either vo or so is specified as sixth parameter, then the utility extracts video only (vo) or audio only (so).");
      Console.WriteLine("Additionally, if cts is specified as sixth or seventh parameter, then the utility adds the CTTS box to MP4 output.");
      Console.WriteLine("");
      Console.WriteLine("If there is no parameter other than the input file, information about the input file is output to the console:");
      Console.WriteLine("RecodingFilter [input file]");

      // ListMediaHandlers does not work anymore because we have put all handlers in the same assembly (the main assembly).
      //ListMediaHandlers();

    }

    static void Main(string[] args)
    {
      // this class can read either a finite length file, such as mp4 or a fragmented/live file, such as ISMV or input
      if ((args.Length != 7) && (args.Length != 5) && (args.Length != 6) && (args.Length != 1))
      {
        PrintUsage();
        return;
      }

      ulong startTime = 0UL;
      ulong endTime = 0UL;
      string outVideo = "";
      ushort trackID = 1;
      TracksIncluded audioOrVideoOnly = TracksIncluded.Both;
      bool cttsOut = false; // default to NO CTTS box in MP4 output

      if ((args.Length >= 5) && (args.Length <= 7))
      {
        // command line validation
        char[] badc = Path.GetInvalidPathChars();
        if (args[0].ToCharArray().Any(c => IsOneOf(c, badc)))
        {
          Console.WriteLine("First parameter must be a path name to the input file to recode.");
          return;
        }

        if (!ulong.TryParse(args[1], out startTime))
        {
          Console.WriteLine("Second parameter must be an unsigned long integer which is the start time.");
          return;
        }

        if (!ulong.TryParse(args[2], out endTime))
        {
          Console.WriteLine("Third parameter must be an unsigned long integer which is the end time.");
          return;
        }

        if ((args[3].ToCharArray().Any(c => IsOneOf(c, badc))) || (!Path.HasExtension(args[3])))
        {
          Console.WriteLine("Fourth parameter must be a path name to the output file, with extension.");
          return;
        }
        outVideo = args[3];

        if (!ushort.TryParse(args[4], out trackID))
        {
          Console.WriteLine("Fifth parameter must be an unsigned short integer which is the track ID.");
          return;
        }

        if (args.Length >= 6)
        {
          if (args[5].Equals("vo"))
          {
            audioOrVideoOnly = TracksIncluded.Video;
          }
          else if (args[5].Equals("so"))
          {
            audioOrVideoOnly = TracksIncluded.Audio;
          }
          else if (args[5].Equals("cts"))
          {
            cttsOut = true;
          }
          else
          {
            Console.WriteLine("Sixth parameter should be \"vo\" (video only) or \"so\" (audio only).");
            System.Environment.Exit(0);
          }

          if (args.Length == 7)
          {
            if (args[6].Equals("cts"))
              cttsOut = true;
            else
            {
              Console.WriteLine("Seventh parameter should be \"cts\", or there should be no seventh param.");
              System.Environment.Exit(0);
            }
          }
        }
      }

      string filePath = args[0];
      //      string outVideo = Path.GetFileNameWithoutExtension(filePath) + ".mp4"; // create output file in same folder as input

      // check extension to input file path; if it does not have it, just exit
      if (Path.HasExtension(filePath))
      {
        string ext = Path.GetExtension(filePath).ToLower();
        Console.WriteLine("Input file {0} is a {1} file.", Path.GetFileName(filePath), ext);
      }
      else
      {
        Console.WriteLine("Must specify file name extension for both input and output files.");
        System.Environment.Exit(0);
      }

      IMediaStream mediaStream;
      if (args.Length == 1)
      {
        mediaStream = GenericMediaStreamFactory.Create(filePath, FileMode.Open, false); // non-caching
        mediaStream.Read();
        Console.WriteLine("<RecoderOutput>");
        Console.WriteLine("  <QboxInfo");
        Console.WriteLine("    Path=\"{0}\" ", filePath);
        Console.WriteLine("    Length100NanoSecs=\"{0}\" ", mediaStream.DurationIn100NanoSecs);
        Console.WriteLine("    TrackCount=\"{0}\" ", mediaStream.MediaTracks.Count);
        Console.WriteLine("  />");

        Console.WriteLine("  <TrackList>");
        foreach (GenericMediaTrack track in mediaStream.MediaTracks)
        {
          Console.WriteLine("  <Track ID=\"{0}\" Codec=\"{1}\" />", track.TrackID, track.Codec.CodecType);
        }
        Console.WriteLine("  </TrackList>");

        Console.WriteLine("</RecoderOutput>");
        return;
      }

      if (File.Exists(outVideo))
      {
        //        Console.WriteLine("Output file already exists. Delete? Y/N");
        //        string s = Console.ReadLine();
        //        if ((s[0] == 'y') || (s[0] == 'Y'))
        File.Delete(outVideo);
        //        else System.Environment.Exit(0);
      }

      try
      {
        // mediaStream can be any media stream: mp4, ismv, etc. it all depends on the file extension
        BaseRecode recodingInstance;
        bool cacheEnabled = true;
        mediaStream = GenericMediaStreamFactory.Create(filePath, FileMode.Open, cacheEnabled);
        IMediaStream outStream = GenericMediaStreamFactory.Create(outVideo, FileMode.CreateNew, cacheEnabled);
        if (cacheEnabled)
        {
          recodingInstance = new GenericRecodeWRC(mediaStream, outStream, trackID, audioOrVideoOnly, cttsOut); // caching
        }
        else
        {
          recodingInstance = null; // new GenericRecodeNOC(mediaStream, outStream, audioOrVideoOnly, cttsOut); // non-caching
        }

        string outputExtension = Path.GetExtension(outVideo);
        if (cttsOut && !(outputExtension.Contains("mp4") || outputExtension.Contains("MP4"))) // all lower-case or all-uppercse only
        {
          throw new Exception("CTTS output is supported only for MP4 output at this time.");
        }
        recodingInstance.MaxIterateDuration = 30000000; // 3 second video blocks

        recodingInstance.Recode(startTime, endTime, trackID);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
      }
    }

    static void ListMediaHandlers()
    {
      Console.WriteLine();
      Console.WriteLine("List of available Media Handlers (check DLLs included in executable path):");
      try
      {
        Assembly asm1 = Assembly.ReflectionOnlyLoad("GenericMediaHandlerWin, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        Assembly asm2 = Assembly.ReflectionOnlyLoad("ISMVHandlerWin, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        Console.WriteLine("Expression Encoder ISMV file format");
        Assembly asm3 = Assembly.ReflectionOnlyLoad("MP4HandlerWin, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        Console.WriteLine("MP4 file format");
        Assembly asm4 = Assembly.ReflectionOnlyLoad("QBOXHandlerWin, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        Console.WriteLine("QBOX file format (currently broken)");
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
      }
    }
  }
}

