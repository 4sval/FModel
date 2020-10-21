namespace PakReader.Pak.IO
{
    public enum EIoContainerFlags : byte
    {
        None,
        Compressed	= (1 << 0),
        Encrypted	= (1 << 1),
        Signed		= (1 << 2),
        Indexed		= (1 << 3),
    };
}