namespace Media.Formats.MP4
{
    using System;

    public class SizeChecker : IDisposable
    {
        private Box m_currentBox;
        private BoxReader m_reader;
        private long m_startPosition;

        public SizeChecker(Box currentBox, BoxReader reader)
        {
            this.m_currentBox = currentBox;
            this.m_startPosition = reader.BaseStream.Position;
            this.m_reader = reader;
        }

			  public long DataLeft() {
					long num = (long)this.m_currentBox.Size - (long)(this.m_reader.BaseStream.Position - this.m_startPosition);
			  	return (num);
			  }

        public void Dispose()
        {
            long num = this.m_reader.BaseStream.Position - this.m_startPosition;
            if (num != (long)this.m_currentBox.Size)
            {
              // there are certain Expression files that have extra bytes in the Avc1 box
              if ((m_currentBox.Type == BoxTypes.Avc1) ||
                  (m_currentBox.Type == BoxTypes.AvcC) ||
                  (m_currentBox.Type == BoxTypes.SampleDescription))
                m_currentBox.Size = (ulong)num; // fix size of avc1 box and do not skip, the same as how VLC treats this
              else
                throw new UnexpectedBoxSizeException(this.m_currentBox.Type, this.m_currentBox.Size, (uint)num);
            }
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
        }
    }
}
