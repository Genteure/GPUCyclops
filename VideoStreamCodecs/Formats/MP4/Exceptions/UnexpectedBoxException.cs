namespace Media.Formats.MP4
{

    public class UnexpectedBoxException : Fmp4ParserException
    {
        private BoxType m_actualBoxType;
        private BoxType m_expectedBoxType;

        public UnexpectedBoxException(BoxType expectedBoxType, BoxType actualBoxType)
        {
            this.m_expectedBoxType = expectedBoxType;
            this.m_actualBoxType = actualBoxType;
        }
    }
}
