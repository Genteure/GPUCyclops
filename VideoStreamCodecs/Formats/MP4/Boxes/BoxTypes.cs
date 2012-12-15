namespace Media.Formats.MP4
{
    using System;

    public static class BoxTypes
    {
        public static readonly BoxType Error = new BoxType("xxxx");
        public static readonly BoxType UUID = new BoxType("uuid"); // facility for extending box types (see uuid in MP4 standards)

        public static readonly BoxType Any = new BoxType("????");
        public static readonly BoxType AnyDescription = new BoxType("anyd");
        public static readonly BoxType Avcc = new BoxType("avcC");
        public static readonly BoxType BitRate = new BoxType("btrt");
        public static readonly BoxType ChunkOffset = new BoxType("stco");
        public static readonly BoxType CleanApertureBox = new BoxType("clap");
        public static readonly BoxType CompositionOffset = new BoxType("ctts");
        public static readonly BoxType Dvc1 = new BoxType("dvc1");
        public static readonly BoxType DataEntryUrl = new BoxType("url ");
        public static readonly BoxType DataEntryUrn = new BoxType("urn ");
        public static readonly BoxType DataInformation = new BoxType("dinf");
        public static readonly BoxType DataReference = new BoxType("dref");
        public static readonly BoxType Damr = new BoxType("damr"); // use when SampleDescription = samr
        public static readonly BoxType Edts = new BoxType("edts");
        public static readonly BoxType Elst = new BoxType("elst");
        public static readonly BoxType Esds = new BoxType("esds"); // use when SampleDescription = mp4a/aac
        public static readonly BoxType FileType = new BoxType("ftyp");
        public static readonly BoxType Free = new BoxType("free");
        public static readonly BoxType HandlerReference = new BoxType("hdlr");
        public static readonly BoxType HintMediaHeader = new BoxType("hmhd");
        public static readonly BoxType IndependentAndDisposableSamplesBox = new BoxType("sdtp");
        public static readonly BoxType Media = new BoxType("mdia");
        public static readonly BoxType MediaData = new BoxType("mdat");
        public static readonly BoxType MediaHeader = new BoxType("mdhd");
        public static readonly BoxType MediaInformation = new BoxType("minf");
        public static readonly BoxType Movie = new BoxType("moov");
        public static readonly BoxType MovieExtends = new BoxType("mvex");
        public static readonly BoxType MovieExtendsHeader = new BoxType("mehd");
        public static readonly BoxType MovieFragment = new BoxType("moof");
        public static readonly BoxType MovieFragmentHeader = new BoxType("mfhd");
        public static readonly BoxType MovieFragmentRandomAccess = new BoxType("mfra");
        public static readonly BoxType MovieFragmentRandomAccessOffset = new BoxType("mfro");
        public static readonly BoxType MovieHeader = new BoxType("mvhd");
        public static readonly BoxType NullMediaHeader = new BoxType("nmhd");
        public static readonly BoxType ObjectDescriptor = new BoxType("iods");
        public static readonly BoxType OpaqueVC1Sample = new BoxType("ovc1");
        public static readonly BoxType OpaqueWMASample = new BoxType("owma");
        public static readonly BoxType PixelAspectRatio = new BoxType("pasp"); 
        public static readonly BoxType SampleTable = new BoxType("stbl");
        public static readonly BoxType SampleDescription = new BoxType("stsd");
        public static readonly BoxType SampleToChunk = new BoxType("stsc");
        public static readonly BoxType SampleSize = new BoxType("stsz");
        public static readonly BoxType Samr = new BoxType("samr");
        public static readonly BoxType SoundMediaHeader = new BoxType("smhd");
        public static readonly BoxType SyncSampleMap = new BoxType("stss");
        public static readonly BoxType TimeToSample = new BoxType("stts");
        public static readonly BoxType Track = new BoxType("trak");
        public static readonly BoxType TrackExtends = new BoxType("trex");
        public static readonly BoxType TrackFragment = new BoxType("traf");
        public static readonly BoxType TrackFragmentHeader = new BoxType("tfhd");
        public static readonly BoxType TrackFragmentRandomAccess = new BoxType("tfra");
        public static readonly BoxType TrackFragmentRun = new BoxType("trun");
        public static readonly BoxType TrackHeader = new BoxType("tkhd");
        public static readonly BoxType TrackReference = new BoxType("tref");
        public static readonly BoxType UserData = new BoxType("udta");
        public static readonly BoxType Vc1 = new BoxType("vc-1");
        public static readonly BoxType VideoMediaHeader = new BoxType("vmhd");
        public static readonly BoxType Wfex = new BoxType("wfex");  // in MS ismv file
        public static readonly BoxType Wma = new BoxType("wma ");   // used in MS ismv file


        public static readonly BoxType AudioSampleEntry = new BoxType("soun"); // can be mp4a
        public static readonly BoxType VisualSampleEntry = new BoxType("vide"); // can be mp4v
        public static readonly BoxType HintSampleEntry = new BoxType("hint");
        public static readonly BoxType XMLMetaDataSampleEntry = new BoxType("metx");
        public static readonly BoxType TextMetaDataSampleEntry = new BoxType("mett");
        public static readonly BoxType ODSMEntry = new BoxType("odsm");
        public static readonly BoxType ALISEntry = new BoxType("alis"); // Apple's alis
        public static readonly BoxType Avc1 = new BoxType("avc1");  // codec private and other data
        public static readonly BoxType AvcC = new BoxType("avcC");  // codec private and other data
        public static readonly BoxType Mp4v = new BoxType("mp4v");  // codec private and other data
        public static readonly BoxType Mp4a = new BoxType("mp4a");
        public static readonly BoxType UnknownSampleEntry = new BoxType("????");
        
    }
}
