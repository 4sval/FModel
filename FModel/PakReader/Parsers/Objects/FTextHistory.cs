namespace FModel.PakReader.Parsers.Objects
{
    public abstract partial class FTextHistory
    {
        // quick conversion so extra space isn't wasted casting this if you know what the type is
        public T As<T>() where T : FTextHistory => (T)this;
    }
}
