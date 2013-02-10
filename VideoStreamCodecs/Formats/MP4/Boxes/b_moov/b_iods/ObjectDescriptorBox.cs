using System;
using System.Text;
using Media.H264;

namespace Media.Formats.MP4
{
  /// <summary>
  /// ObjectDesriptorBox
  /// We don't know what this box is for, and what's in it.
  /// It exists in MP4 files that comply with the isom brand.
  /// </summary>
  public class ObjectDescriptorBox : Box // maybe a full box?
  {
    byte[] _contents;

    public ObjectDescriptorBox()
      : base(BoxTypes.ObjectDescriptor)
    {
    }

    public ObjectDescriptorBox(string descriptor)
      : base(BoxTypes.ObjectDescriptor)
    {
      Contents = descriptor;
    }

    public override void Read(BoxReader reader)
    {
      using (new SizeChecker(this, reader))
      {
        base.Read(reader);
        _contents = new byte[base.Size - 8UL];
        reader.Read(_contents, 0, _contents.Length);
      }
    }

    public override void Write(BoxWriter writer)
    {
      using (new SizeCalculator(this, writer))
      {
        base.Write(writer);
        writer.Write(_contents, 0, _contents.Length);
      }
    }

    public string Contents
    {
      get
      {
        StringBuilder sb = new StringBuilder(_contents.Length * 2);
        for (int i = 0; i < _contents.Length; i++)
        {
          sb.Append(string.Format("{0:X2}", _contents[i]));
        }
        return sb.ToString();
      }
      private set
      {
        string strData = (string)value;
        _contents = H264Utilities.HexStringToBytes(strData);
        this.Size += (ulong)_contents.Length;
      }
    }
  }
}
