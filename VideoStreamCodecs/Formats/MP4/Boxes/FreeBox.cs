using System.Text;

namespace Media.Formats.MP4 {
  public class FreeBox : Box {
    public byte[] data;

    public FreeBox() : base(BoxTypes.Free) {}

    public FreeBox(byte[] indata)
      : this()
    {
      data = indata;
      this.Size += (ulong)indata.Length;
    }

    public override void Read(BoxReader reader) {
      long pos = reader.BaseStream.Position;
      base.Read(reader);
      data = new byte[base.Size - 8];
      reader.Read(data, 0, data.Length); // FIXME: useless all-zero data - should we even read?
    }

    public override void Write(BoxWriter writer)
    {
      using (new SizeCalculator(this, writer))
      {
        base.Write(writer);
        if (data != null)
          writer.Write(data, 0, data.Length);
      }
    }

    public override string ToString() {
        StringBuilder xml = new StringBuilder();
        xml.Append(base.ToString());
        xml.Append("<free/>");
        xml.Append("</box>");
        return xml.ToString();
    }
  }
}
