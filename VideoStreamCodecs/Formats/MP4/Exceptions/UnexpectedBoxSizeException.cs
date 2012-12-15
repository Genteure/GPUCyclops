namespace Media.Formats.MP4
{
    public class UnexpectedBoxSizeException : Fmp4ParserException
    {
        public UnexpectedBoxSizeException(BoxType type, ulong expectedSize, uint readSize)
        {
            this.BoxType = type;
            this.ExpectedSize = expectedSize;
            this.ReadSize = readSize;
        }

        public BoxType BoxType { get; private set; }

        public ulong ExpectedSize { get; private set; }

        public uint ReadSize { get; private set; }
    }
}
