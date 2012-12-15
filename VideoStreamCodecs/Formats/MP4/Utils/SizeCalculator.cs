namespace Media.Formats.MP4
{
    using System;

    public class SizeCalculator : IDisposable
    {
        private Box m_currentBox;
        private BoxWriter m_writer;
        private long m_startPosition;

        public SizeCalculator(Box currentBox, BoxWriter writer)
        {
            this.m_currentBox = currentBox;
            this.m_startPosition = writer.BaseStream.Position;
            this.m_writer = writer;
        }

        public void Dispose() {
          // calculate size
          long pos = this.m_writer.BaseStream.Position;
          ulong size = (ulong)(pos - this.m_startPosition);
          // check against this box's Size property
          if (this.m_currentBox.Size != size)
            throw new Exception("mismatched box size");
          // write out the size to the dest file
          //m_writer.BaseStream.Seek(m_startPosition, System.IO.SeekOrigin.Begin);
          //m_writer.WriteUInt32(size);
          // put the file position back to the end of this box
          //this.m_writer.BaseStream.Position = pos;

          this.Dispose(true);
          GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
        }
    }
}
