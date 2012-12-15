/*
8.8.3.1 Definition
  Box Type: ‘trex’
  Container: Movie Extends Box (‘mvex’)
  Mandatory: Yes
  Quantity: Exactly one for each rawTrack in the Movie Box
  
  This sets up default values used by the movie fragments. By setting defaults in this way, space and
  complexity can be saved in each Track Fragment Box.
  The sample flags field in sample fragments (default_sample_flags here and in a Track Fragment Header
  Box, and sample_flags and first_sample_flags in a Track Fragment Run Box) is coded as a 32-bit
  value. 
  
  It has the following structure:
    bit(6) reserved=0;
    unsigned int(2) sample_depends_on;
    unsigned int(2) sample_is_depended_on;
    unsigned int(2) sample_has_redundancy;
    bit(3) sample_padding_value;
    bit(1) sample_is_difference_sample;
    // i.e. when 1 signals a non-key or non-sync sample
    unsigned int(16) sample_degradation_priority;
  
  The sample_depends_on, sample_is_depended_on and sample_has_redundancy values are defined
  as documented in the Independent and Disposable Samples Box.
  The sample_padding_value is defined as for the padding bits table. The
  sample_degradation_priority is defined as for the degradation priority table.
8.8.3.2 Syntax
  aligned(8) class TrackExtendsBox extends FullBox(‘trex’, 0, 0){
    unsigned int(32) track_ID;
    unsigned int(32) default_sample_description_index;
    unsigned int(32) default_sample_duration;
    unsigned int(32) default_sample_size;
    unsigned int(32) default_sample_flags
  }
8.8.3.3 Semantics
  track_id identifies the rawTrack; this shall be the rawTrack ID of a rawTrack in the Movie Box
  default_ these fields set up defaults used in the rawTrack fragments.
*/

using System.Text;

namespace Media.Formats.MP4
{

  class TrackExtendsBox : FullBox {
    public TrackExtendsBox() : base(BoxTypes.TrackExtends) {
    }

    public TrackExtendsBox(uint trackID, uint dsdi, uint dsd, uint dss, uint dsf)
      : this()
    {
      this.TrackID = trackID;
      DefaultSampleDescriptionIndex = dsdi;
      DefaultSampleDuration = dsd;
      DefaultSampleFlags = dsf;
      DefaultSampleSize = dss;
      this.Size += 20UL; // 5 * 4 bytes
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);
        TrackID = reader.ReadUInt32();
        DefaultSampleDescriptionIndex = reader.ReadUInt32();
        DefaultSampleDuration = reader.ReadUInt32();
        DefaultSampleSize = reader.ReadUInt32();
        DefaultSampleFlags = reader.ReadUInt32();
      }
    }


    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.WriteUInt32(TrackID);
            writer.WriteUInt32(DefaultSampleDescriptionIndex);
            writer.WriteUInt32(DefaultSampleDuration);
            writer.WriteUInt32(DefaultSampleSize);
            writer.WriteUInt32(DefaultSampleFlags);
        }
    }


    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<trackid>").Append(TrackID).Append("</trackid>");
      xml.Append("<defaultsampledescriptionindex>").Append(DefaultSampleDescriptionIndex).Append("</defaultsampledescriptionindex>");
      xml.Append("<defaultsampleduration>").Append(DefaultSampleDuration).Append("</defaultsampleduration>");
      xml.Append("<defaultsamplesize>").Append(DefaultSampleSize).Append("</defaultsamplesize>");
      xml.Append("<defaultsampleflags>").Append(DefaultSampleFlags).Append("</defaultsampleflags>");
      xml.Append("</box>");
      return (xml.ToString());
    }


    public uint TrackID;
    public uint DefaultSampleDescriptionIndex;
    public uint DefaultSampleDuration;
    public uint DefaultSampleSize;
    public uint DefaultSampleFlags;
  }
}
