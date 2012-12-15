using System;
using System.Text;
using System.Linq;

namespace Media.Formats.MP4 {
  public class FileTypeBox : Box {
    public FileTypeBox() : base(BoxTypes.FileType) {
    }

    public FileTypeBox(string[] brands)
      : this()
    {
      major_brand = StringToUInt(brands[0]); // 4 bytes
      if (brands.Any(b => b.Equals("mp42")))
        minor_version = 1;  // mp42 = 1; 
      else
        minor_version = 0; // isml                    // 4 bytes
      compatible_brands = new uint[brands.Length];
      for (int i = 0; i < brands.Length; i++)
      {
        compatible_brands[i] = StringToUInt(brands[i]); // 4 bytes each
      }
      this.Size += 8UL + 4 * (ulong)brands.Length;
    }

    private uint StringToUInt(string brand)
    {
      byte[] fourBytes = new byte[4];
      fourBytes[0] = (byte)brand[3];
      fourBytes[1] = (byte)brand[2];
      fourBytes[2] = (byte)brand[1];
      fourBytes[3] = (byte)brand[0];
      return BitConverter.ToUInt32(fourBytes, 0);
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        long curpos = reader.BaseStream.Position;

        base.Read(reader); 
        major_brand = reader.ReadUInt32();
        minor_version = reader.ReadUInt32();
        int brand_count = (int)(this.Size - (ulong)(reader.BaseStream.Position - curpos)) / 4;
        compatible_brands = new uint[brand_count];
        for (int i=0; i<brand_count; i++) {
          compatible_brands[i] = reader.ReadUInt32();
        }
      }
    }

    public override void Write(BoxWriter writer) {
      using (new SizeCalculator(this, writer)) {
        base.Write(writer);

        writer.Write(major_brand);
        writer.WriteUInt32(minor_version);
        foreach (uint brand in compatible_brands) {
          writer.WriteUInt32(brand);
        }
      }
    }

    public static string ReverseString(string s) {
      char[] arr = s.ToCharArray();
      Array.Reverse(arr);
      return new string(arr);
    }

    private uint major_brand { get; set; }
    private uint minor_version { get; set; }
    private uint[] compatible_brands { get; set; }

    public uint MinorVersion { get { return(minor_version); } set { minor_version = value; } }

    public string MajorBrand { 
      get {
        byte[] bytes = BitConverter.GetBytes(major_brand);
        System.Text.UTF8Encoding encoding = new UTF8Encoding();
        string sval = ReverseString(encoding.GetString(bytes, 0, bytes.Length));
        return (sval);
      }

      set {
        ASCIIEncoding  encoding = new ASCIIEncoding();
        byte[] bytes = encoding.GetBytes(ReverseString(value));
        major_brand = BitConverter.ToUInt32(bytes, 0);
      }
    }

    public string[] CompatibleBrands {
      get {
        string[] ans = new string[compatible_brands.Length];
        int x = 0;
        foreach (uint brand in compatible_brands) {
          byte[] bytes = BitConverter.GetBytes(brand);

          System.Text.UTF8Encoding encoding = new UTF8Encoding();
          string sval = ReverseString(encoding.GetString(bytes, 0, bytes.Length));
          ans[x] = sval;
          x++;
        }
        return (ans);
      }

      set {
        int x = 0;
        compatible_brands = new uint[value.Length];
        foreach (string brand in value) {
          ASCIIEncoding encoding = new ASCIIEncoding();
          byte[] bytes = encoding.GetBytes(ReverseString(brand));
          compatible_brands[x] = BitConverter.ToUInt32(bytes, 0);
          x++;
        }
      }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<majorbrand>").Append(MajorBrand).Append("</majorbrand>");
      xml.Append("<minorversion>").Append(MinorVersion).Append("</minorversion>");
      xml.Append("<compatiblebrands>");
      for (int i=0; i<CompatibleBrands.Length; i++)
          if (CompatibleBrands[i][0] != (char)0)
            xml.Append("<compatiblebrand>").Append(CompatibleBrands[i]).Append("</compatiblebrand>");
      xml.Append("</compatiblebrands>");
      xml.Append("</box>");
      return (xml.ToString());
    }
  }
}
