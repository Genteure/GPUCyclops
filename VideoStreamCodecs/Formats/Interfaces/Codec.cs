namespace Media {
  public enum CodecTypes { Audio, Video, Meta, Augment, Unknown }

  public class Codec {
    public CodecTypes CodecType { get; set; }
    public string PrivateCodecData { get; set; }


    public Codec(CodecTypes expectedType)
    {
        CodecType = expectedType;
    }

  }
}
