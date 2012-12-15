using System.Text;

namespace Media.Formats.MP4
{
    public class UserUniqueIDBox : FullBox
    {
        public UserUniqueIDBox() : base(BoxTypes.UUID)
        {
        }

				public UserUniqueIDBox(byte[] inUserType, byte[] inUserData) : this() {
					this.UserType = inUserType;
					this.UserData = inUserData;
          this.Size += (ulong)(4 + inUserData.Length);
        }

        public override void Read(BoxReader reader)
        {
            using (SizeChecker checker = new SizeChecker(this, reader))
            {
              base.Read(reader);
            	UserType = reader.ReadBytes(4);
							UserData = reader.ReadBytes((int)checker.DataLeft());
            }
        }

        public override void Write(BoxWriter writer) {
          using (new SizeCalculator(this, writer)) {
            base.Write(writer);
						writer.Write(UserType);
						writer.Write(UserData);
					}
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();
          xml.Append(base.ToString());
					string utype = Encoding.UTF8.GetString(UserType, 0, UserType.Length);
					xml.Append("<usertype>").Append(utype).Append("</usertype>");
					xml.Append("<userdata>...").Append("</userdata>");
					xml.Append("</box>");
          return (xml.ToString());
        }


        public byte[] UserType; // 4 bytes
    		public byte[] UserData;
    }
}
