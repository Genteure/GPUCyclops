using System;
using System.Text;
using System.IO;

namespace Media.Formats.JPGFJCore
{
    /// <summary>
    /// A Big-endian binary writer.
    /// </summary>
    internal class FJBinaryWriter
    {
        private Stream _stream;

        internal FJBinaryWriter(Stream stream)
        {
            _stream = stream;
        }

        internal void Write(byte[] val)
        {
            _stream.Write(val, 0, val.Length);
        }

        internal void Write(byte[] val, int offset, int count)
        {
            _stream.Write(val, offset, count);
        }


        internal void Write(short val)
        {
            _stream.WriteByte((byte)(( val >> 8 ) & 0xFF));
            _stream.WriteByte((byte)(val & 0xFF));
        }

        internal void Write(byte val)
        {
            _stream.WriteByte(val);
        }

    }
}
