namespace FModel.PakReader.Pak
{
    public interface IPakFilter
    {
        bool CheckFilter(string path, bool caseSensitive);
    }
}
