
namespace FModel.Models;

public enum DiffLineType { Unchanged, Added, Deleted }

public class DiffLine
{
    public string Text { get; set; }
    public DiffLineType Type { get; set; }
}
