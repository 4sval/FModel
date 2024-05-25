namespace FModel.Framework;

public class FStatus : ViewModel
{
    private bool _isReady;
    public bool IsReady
    {
        get => _isReady;
        private set => SetProperty(ref _isReady, value);
    }

    private EStatusKind _kind;
    public EStatusKind Kind
    {
        get => _kind;
        private set
        {
            SetProperty(ref _kind, value);
            IsReady = Kind != EStatusKind.Loading && Kind != EStatusKind.Stopping;
        }
    }

    private string _label;
    public string Label
    {
        get => _label;
        private set => SetProperty(ref _label, value);
    }

    public FStatus()
    {
        SetStatus(EStatusKind.Loading);
    }

    public void SetStatus(EStatusKind kind, string label = "")
    {
        Kind = kind;
        UpdateStatusLabel(label);
    }

    public void UpdateStatusLabel(string label, string prefix = null)
    {
        Label = Kind == EStatusKind.Loading ? $"{prefix ?? Kind.ToString()} {label}".Trim() : Kind.ToString();
    }
}
