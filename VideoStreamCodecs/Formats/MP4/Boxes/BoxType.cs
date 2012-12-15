namespace Media.Formats.MP4
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    [StructLayout(LayoutKind.Sequential)]
    public struct BoxType
    {
        private byte[] typeBytes;
        public BoxType(byte[] bytes)
        {
            this.typeBytes = bytes;
        }

        public BoxType(string type)
        {
            if (type.Length != 4)
            {
                throw new ArgumentException("unexpected size");
            }
            this.typeBytes = new byte[] { Convert.ToByte(type[0]), Convert.ToByte(type[1]), Convert.ToByte(type[2]), Convert.ToByte(type[3]) };
        }

        public byte[] GetBytes() { return(typeBytes); }

        public static bool operator ==(BoxType box1, BoxType box2)
        {
            if (box1.typeBytes.Length != box2.typeBytes.Length)
            {
                return false;
            }
            for (int i = 0; i < box1.typeBytes.Length; i++)
            {
                if (box1.typeBytes[i] != box2.typeBytes[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static bool operator !=(BoxType box1, BoxType box2)
        {
            return !(box1 == box2);
        }

        public override int GetHashCode()
        {
            return this.typeBytes.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ((obj is BoxType) && (this == ((BoxType) obj)));
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Convert.ToChar(this.typeBytes[0]));
            builder.Append(Convert.ToChar(this.typeBytes[1]));
            builder.Append(Convert.ToChar(this.typeBytes[2]));
            builder.Append(Convert.ToChar(this.typeBytes[3]));
            return builder.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ExtendedBoxType
    {
        private byte[] typeBytes;
        public ExtendedBoxType(byte[] bytes)
        {
            this.typeBytes = bytes;
        }

        public ExtendedBoxType(string type)
        {
            if (type.Length != 16)
            {
                throw new ArgumentException("unexpected size of extended type");
            }
            this.typeBytes = new byte[] { 
                        Convert.ToByte(type[0]), Convert.ToByte(type[1]), Convert.ToByte(type[2]), Convert.ToByte(type[3]),
                        Convert.ToByte(type[4]), Convert.ToByte(type[5]), Convert.ToByte(type[6]), Convert.ToByte(type[7]),
                        Convert.ToByte(type[8]), Convert.ToByte(type[9]), Convert.ToByte(type[10]), Convert.ToByte(type[11]),
                        Convert.ToByte(type[12]), Convert.ToByte(type[13]), Convert.ToByte(type[14]), Convert.ToByte(type[15]),
            };
        }

        public byte[] GetBytes() { return(typeBytes); }

        public static bool operator ==(ExtendedBoxType box1, ExtendedBoxType box2)
        {
            if (box1.typeBytes.Length != box2.typeBytes.Length)
            {
                return false;
            }
            for (int i = 0; i < box1.typeBytes.Length; i++)
            {
                if (box1.typeBytes[i] != box2.typeBytes[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static bool operator !=(ExtendedBoxType box1, ExtendedBoxType box2)
        {
            return !(box1 == box2);
        }

        public override int GetHashCode()
        {
            return this.typeBytes.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ((obj is ExtendedBoxType) && (this == ((ExtendedBoxType) obj)));
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Convert.ToChar(this.typeBytes[0]));
            builder.Append(Convert.ToChar(this.typeBytes[1]));
            builder.Append(Convert.ToChar(this.typeBytes[2]));
            builder.Append(Convert.ToChar(this.typeBytes[3]));
            builder.Append(Convert.ToChar(this.typeBytes[4]));
            builder.Append(Convert.ToChar(this.typeBytes[5]));
            builder.Append(Convert.ToChar(this.typeBytes[6]));
            builder.Append(Convert.ToChar(this.typeBytes[7]));
            builder.Append(Convert.ToChar(this.typeBytes[8]));
            builder.Append(Convert.ToChar(this.typeBytes[9]));
            builder.Append(Convert.ToChar(this.typeBytes[10]));
            builder.Append(Convert.ToChar(this.typeBytes[11]));
            builder.Append(Convert.ToChar(this.typeBytes[12]));
            builder.Append(Convert.ToChar(this.typeBytes[13]));
            builder.Append(Convert.ToChar(this.typeBytes[14]));
            builder.Append(Convert.ToChar(this.typeBytes[15]));
            return builder.ToString();
        }
    }
}
