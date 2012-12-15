using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Media.Formats.Generic;

namespace Media.Formats.MP4
{
  public enum WriteState { Start, MoovReady, MoovDone, AllSamplesDone, Closed }

  public class MP4StreamWriter : MP4Stream, IDisposable
  {
    // We use the same m_writer for writing to boxes and the mdat box itself.
    // This is OK because, even if this were a long-running movie from a live camera, the movie will be broken up into relatively independent fragments,
    // and each MP4StreamWriter instance concerns itself with a single file only.
    protected BoxWriter m_writer = null;

    protected WriteState state;

    protected List<GenericMediaTrack> inputTracks;

    public void Close()
    {
      if (this.m_writer != null) { this.m_writer.Close(); this.m_writer = null; }
		if (this.tempMdat != null) { this.tempMdat.Close(); }
		if (string.IsNullOrEmpty(tempMdatFilePath) == false && File.Exists(tempMdatFilePath)) { File.Delete(tempMdatFilePath); }
      state = WriteState.Closed;
    }

    public void Dispose()
    {
      if (this.m_writer != null) this.m_writer.Close();
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
      if (disposing)
      {
        // dispose more stuff here
      }
    }

    public long GetWriterPosition()
    {
      return (m_writer.BaseStream.Position);
    }

    public override void Create(Stream outStream)
    {
      base.Create(outStream); // base.Stream in this case is an output file
      this.m_writer = new BoxWriter(outStream);
      state = WriteState.Start;
    }

    public override void Create(string fileName)
    {
      FileStream output = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
      this.Create(output);
    }

    //public override void Write()
    //{
    //  if (state == WriteState.MoovReady)
    //  {
    //    // write out moov only
    //    this.ftb.Write(m_writer);
    //    this.mmb.Write(m_writer);
    //    state = WriteState.MoovDone;
    //  }
    //  else if (state == WriteState.MoovDone)
    //  {
    //    // write out samples
    //  }
    //  else throw new Exception(string.Format("Cannot write to destination file (bad state = {0})", state));
    //}

    /// <summary>
    /// CreateTracksForWriting
    /// Generic method for creating tracks given a list of RawBaseTrackInfo.
    /// </summary>
    /// <typeparam name="T">must be MP4TrackFormat or any type derived from it</typeparam>
    /// <param name="tracksInfo">list of RawBaseTrackInfo</param>
    protected void CreateTracksForWriting<T>(List<IsochronousTrackInfo> tracksInfo) where T : MP4TrackFormat, new()
    {
      // We can't just use the input tracksInfo and assign it to our _MediaTracks (that would not work because the types won't match).
      // The input or source media tracks will normally have different types than our _MediaTracks (although they are derived from the same GenericMediaTrack).
      foreach (IsochronousTrackInfo rawTrack in tracksInfo)
      {
        T trackFormat = new T();
        trackFormat.TrackBox = new TrackBox(this.mmb, rawTrack);
        this.mmb.AddTrackBox(trackFormat.TrackBox);
        switch (rawTrack.HandlerType)
        {
          case "Audio":
            GenericAudioTrack audioTrack = new GenericAudioTrack(trackFormat, this);
            base.AddTrack(audioTrack);
            break;
          case "Video":
            GenericVideoTrack videoTrack = new GenericVideoTrack(trackFormat, this);
            base.AddTrack(videoTrack);
            break;
          default:
            throw new Exception("Unknown source handler type");
        }
        //if (trackFormat.TrackID != (mmb.MovieHeaderBox.NextTrackID - 1))
        //  throw new Exception("MP4StreamWriter: TrackID broken");
      }
    }

    /// <summary>
    /// InitializeForWriting
    /// Setup moov boxes, except ctts, stco, stsc, stss, stsz, and stts boxes
    /// </summary>
    /// <param name="tracksInfo">input media tracks</param>
    public override void InitializeForWriting(List<IsochronousTrackInfo> mediaTracksInfo)
    {
      string[] brands;
      if (mediaTracksInfo[0].Brands != null)
      {
        brands = mediaTracksInfo[0].Brands.Split(',');
      }
      else
      {
        brands = new string[3];
        brands[0] = "mp42";
        brands[1] = "isom";
        brands[2] = "mp42";
      }
      this.ftb = new FileTypeBox(brands);

      uint[] matrix = new uint[9];
      matrix[0] = 0x10000; // 1.0
      matrix[4] = 0x10000; // 1.0
      matrix[8] = 0x40000000; // 1.0 (see description of RenderMatrix class)

      RawVideoTrackInfo rvti = (RawVideoTrackInfo)mediaTracksInfo.FirstOrDefault(ti => ti is RawVideoTrackInfo);
      //RawVideoTrackInfo rvti = (RawVideoTrackInfo)tracksInfo.First(info => info.HandlerType == "Video");
      this.mmb = new MovieMetadataBox(mediaTracksInfo, 1.0f, 1.0f, matrix);

      if (rvti != null)
      {
        if (rvti.ObjectDescriptor != null)
          this.mmb.ObjectDescriptorBox = new ObjectDescriptorBox(rvti.ObjectDescriptor);
        if (rvti.UserData != null)
          this.mmb.UserDataBox = new UserDataBox(rvti.UserData);
      }

      if (!IsMediaStreamFragmented)
        CreateTracksForWriting<MP4TrackFormat>(mediaTracksInfo);

      state = WriteState.MoovReady;

      base.InitializeForWriting(mediaTracksInfo);
    }

	 private string tempMdatFilePath = null;
    private Stream tempMdat = null;

    public void WriteSamples(IMediaTrack sourceTrack)
    {
      WriteSamples(sourceTrack, sourceTrack.Codec.CodecType);
    }

    /// <summary>
    /// WriteSamples
    /// Writing out a slice of both the audio and video tracks means that the tracks are going to be interleaved in the final mdat.
    /// NOTE: For fragments, the derived class ISMVStreamWriter takes care of fragments having a separate mdat box for each fragment.
    /// </summary>
    /// <param name="sourceAudio"></param>
    /// <param name="sourceVideo"></param>
    public override void WriteSamples(IMediaTrack sourceAudio, IMediaTrack sourceVideo)
    {
      // NOTE: the sequence order of tracks is important!
      this.WriteSamples(sourceAudio);
      this.WriteSamples(sourceVideo);

      // use current offset into mdat to verify file position AFTER writing all samples in this batch
      if (tempMdat.Position != (long)base.CurrMDatOffset - 8)
        throw new Exception("MPrStreamWriter: current file position does not match stbl data");
    }

    /// <summary>
    /// WriteSamples
    /// Overloaded method for writing slices directly.
    /// </summary>
    /// <param name="slices">A List of Sample(s)</param>
    /// <param name="codecType">Member of CodecTypes enum</param>
    public override void WriteSamples(IEnumerable<Slice> slices, CodecTypes codecType)
    {
		 if (tempMdat == null)
      {
			tempMdatFilePath = Path.GetTempFileName();
			tempMdat = File.Create(tempMdatFilePath);
      }

      foreach (Slice sample in slices)
      {
			  //Common.Logger.Instance.Info("[MP4StreamWriter::WriteSamples] writes " + sample  + ", " + (sample != null ? sample.GetType().Name : string.Empty));
			  tempMdat.Write(sample.SliceBytes, 0, sample.SliceBytes.Length);
      }

      // use current offset into mdat to verify file position AFTER writing all samples in this batch
      // do this check here ONLY if caching is enabled
      if (CachingEnabled)
        base.CheckMDatOffset(tempMdat.Position);
    }

    /// <summary>
    /// FinalizeStream
    /// 1. Write out ftyp
    /// 2. Finalize stbl box in each trak box (call mmb.FinalizeBox)
    /// 3. Calculate size of ftyp + moov + header of mdat boxes
    /// 4. Fix fileoffsets in stbl chunk boxes (NOTE: interleaving means we should have more than one chunk)
    /// 5. Write out all of moov box
    /// 6. Create and write out mdat header only, with correct byte count
    /// 7. Read from mdat sample file and write to destination mdat
    /// </summary>
    public override void FinalizeStream()
    {
      // step 0
      // flush all remaining slices
      if (this.CachingEnabled)
      foreach (GenericMediaTrack track in this.MediaTracks)
      {
        if ((track.SampleStreamLocations != null) && (track.SampleStreamLocations.Count > 0))
        {
          ulong _currMDatOffset = CurrMDatOffset;
          track.TrackFormat.PrepareSampleWriting(track.SampleStreamLocations, ref _currMDatOffset);
          CurrMDatOffset = _currMDatOffset;
          this.WriteSamples(track.SampleStreamLocations.Cast<Slice>(), track.Codec.CodecType);
        }
      }
      // set duration of movie (must be set to duration of longest track)
      TrackBox trkBox = this.mmb.TrackBoxes.FirstOrDefault(box => box.MediaBox.HandlerReferenceBox.HandlerType.Equals("vide"));
      decimal vDuration;
      if (trkBox == null)
        vDuration = 0;
      else
        vDuration = (decimal)trkBox.MediaBox.MediaHeaderBox.Duration / trkBox.MediaBox.MediaHeaderBox.TimeScale;

      trkBox = this.mmb.TrackBoxes.FirstOrDefault(box => box.MediaBox.HandlerReferenceBox.HandlerType.Equals("soun"));
      decimal aDuration;
      if (trkBox == null)
        aDuration = 0;
      else
        aDuration = (decimal)trkBox.MediaBox.MediaHeaderBox.Duration / trkBox.MediaBox.MediaHeaderBox.TimeScale;

      if (vDuration < aDuration)
        this.mmb.MovieHeaderBox.Duration = (ulong)(aDuration * this.mmb.MovieHeaderBox.TimeScale);
      else
        this.mmb.MovieHeaderBox.Duration = (ulong)(vDuration * this.mmb.MovieHeaderBox.TimeScale);

      // step 1
      this.ftb.Write(m_writer);

      // step 2
      this.mmb.FinalizeBox();

      // step 3
      int headerSize = (int)(this.ftb.Size + this.mmb.Size);

      // step 4
      foreach (TrackBox tbox in this.mmb.TrackBoxes)
      {
        tbox.MediaBox.MediaInformationBox.SampleTableBox.ChunkOffSetBox.Fixup(headerSize);
      }

      // step 5
      this.mmb.Write(m_writer);

      // step 6
      this.MediaDataBoxList = new List<MediaDataBox>(); // there is only one mdat for non-fragmented streams.
      MediaDataBox mdat = new MediaDataBox();
      // because the mdat is the last box in the file, we can set its size to 0
      // if mdat is too large, set its size filed to zero:
      // this gets WMP to work, but VLC and QT doesn't like it.
      mdat.Size += (ulong)tempMdat.Length;
      if (mdat.Size > uint.MaxValue)
        mdat.Size = 0UL; // += (ulong)tempMdat.Length;
      this.MediaDataBoxList.Add(mdat);

      // step 7
      tempMdat.Position = 0L;
      mdat.Write(m_writer, tempMdat);

      // close file
      Close();
    }
  }
}
