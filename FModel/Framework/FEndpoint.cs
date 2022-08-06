namespace FModel.Framework;

public class FEndpoint : ViewModel
{
    private string _url;
    public string Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    private bool _overwrite;
    public bool Overwrite
    {
        get => _overwrite;
        set => SetProperty(ref _overwrite, value);
    }

    private string _path;
    public string Path
    {
        get => _path;
        set => SetProperty(ref _path, value);
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_url); // change this later

    public FEndpoint() {}
    public FEndpoint(string url)
    {
        Url = url;
    }
}
