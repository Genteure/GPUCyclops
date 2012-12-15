namespace Media.Formats.MP4 {
  public class RawFrameData {
        private byte[] m_drmData;
        private uint m_frameSize;
        private uint m_startOffset;
        private long m_timestamp;

        public RawFrameData(long timestamp, uint startOffset, uint frameSize, uint duration, byte[] data)
        {
            this.m_timestamp = timestamp;
            this.m_startOffset = startOffset;
            this.m_frameSize = frameSize;
            this.m_drmData = data;
            this.Duration = duration;
        }

        public byte[] DrmData
        {
            get
            {
                return this.m_drmData;
            }
        }

        public uint Duration { get; set; }

        public uint FrameSize
        {
            get
            {
                return this.m_frameSize;
            }
        }

        public uint StartOffset
        {
            get
            {
                return this.m_startOffset;
            }
        }

        public long Timestamp
        {
            get
            {
                return this.m_timestamp;
            }
        }

  }
}
