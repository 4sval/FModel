using System.Linq;
using FModel.ViewModels.ApiEndpoints;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FModel.Framework;

public class FEndpoint : ViewModel
{
    private string _url;
    public string Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    private string _path;
    public string Path
    {
        get => _path;
        set => SetProperty(ref _path, value);
    }

    private bool _overwrite;
    public bool Overwrite
    {
        get => _overwrite;
        set => SetProperty(ref _overwrite, value);
    }

    private string _filePath;
    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    private bool _isValid;
    public bool IsValid
    {
        get => _isValid;
        set
        {
            SetProperty(ref _isValid, value);
            RaisePropertyChanged(nameof(Label));
        }
    }

    [JsonIgnore]
    public string Label => IsValid ?
        "Your endpoint configuration is valid! Please, avoid any unnecessary modifications!" :
        "Your endpoint configuration DOES NOT seem to be valid yet! Please, test it out!";

    public FEndpoint() {}
    public FEndpoint(string url, string path)
    {
        Url = url;
        Path = path;
        IsValid = !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(path); // be careful with this
    }

    public void TryValidate(DynamicApiEndpoint endpoint, EEndpointType type, out JToken response)
    {
        response = null;
        if (string.IsNullOrEmpty(Url) || string.IsNullOrEmpty(Path))
        {
            IsValid = false;
        }
        else switch (type)
        {
            case EEndpointType.Aes:
            {
                var r = endpoint.GetAesKeys(default, Url, Path);
                response = JToken.FromObject(r);
                IsValid = r.IsValid;
                break;
            }
            case EEndpointType.Mapping:
            {
                var r = endpoint.GetMappings(default, Url, Path);
                response = JToken.FromObject(r);
                IsValid = r.Any(x => x.IsValid);
                break;
            }
            default:
            {
                IsValid = false;
                break;
            }
        }
    }
}
