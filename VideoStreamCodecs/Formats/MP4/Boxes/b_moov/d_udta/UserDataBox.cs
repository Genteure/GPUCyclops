using System;
using System.Text;

namespace Media.Formats.MP4
{
  public class UserDataBox : Box
  {
    byte[] _data;

    public UserDataBox()
      : base(BoxTypes.UserData)
    {
    }

    public UserDataBox(string dataStr)
      : base(BoxTypes.UserData)
    {
      Data = dataStr;
    }

    public override void Read(BoxReader reader)
    {
      using (new SizeChecker(this, reader))
      {
        base.Read(reader);
        _data = new byte[base.Size - 8];
        reader.Read(_data, 0, _data.Length);
      }
    }

    public override void Write(BoxWriter writer)
    {
      using (new SizeCalculator(this, writer))
      {
        base.Write(writer);
        writer.Write(_data, 0, _data.Length);
      }
    }

    public string Data
    {
      get
      {
        StringBuilder sb = new StringBuilder(_data.Length * 2);
        for (int i = 0; i < _data.Length; i++)
        {
          sb.Append(string.Format("{0:X2}", _data[i]));
        }
        return sb.ToString();
      }
      private set
      {
        string strData = (string)value;
        _data = H264Utilities.HexStringToBytes(strData);
        this.Size += (ulong)_data.Length;
      }
    }
  }
}
