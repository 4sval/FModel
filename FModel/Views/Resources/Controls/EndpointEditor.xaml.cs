using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using ICSharpCode.AvalonEdit.Document;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace FModel.Views.Resources.Controls;

public partial class EndpointEditor
{
    private readonly EEndpointType _type;
    private bool _isTested;

    public EndpointEditor(FEndpoint endpoint, string title, EEndpointType type)
    {
        DataContext = endpoint;
        _type = type;
        _isTested = endpoint.IsValid;

        InitializeComponent();

        Title = title;
        TargetResponse.SyntaxHighlighting =
            EndpointResponse.SyntaxHighlighting = AvalonExtensions.HighlighterSelector("json");

        InstructionBox.Text = type switch
        {
            EEndpointType.Aes =>
@"In order to make this work, you first need to understand JSON and its query language. If you don't, please close this window. If your game never changes its AES keys or is not even encrypted, please close this window. If you do understand what you are doing, you have to know that the AES path supports up to 2 tokens.

    The first token is mandatory and will be assigned to the main AES key. It has to be looking like a key, else your configuration will not be valid (the key validity against your files will not be checked). Said key must be hexadecimal and can start without ""0x"".

    If your game uses several AES keys, you can specify a second token that will be your list of dynamic keys. The format needed is a list of objects with, at least, the next 2 variables:
{
    ""guid"": ""the archive guid"",
    ""key"": ""the archive aes key""
}",
            EEndpointType.Mapping =>
@"In order to make this work, you first need to understand JSON and its query language. If you don't, please close this window. If your game does not have unversioned package properties, please close this window. If you do understand what you are doing, you have to know that the Mapping path supports up to 2 tokens.

    The first token is mandatory and will be assigned to the mapping download URL, which can be all kinds of mapping but not Brotli compressed.

    The second token is optional and will be assigned to the mapping file name. If unspecified, said file name will be grabbed from the URL.",
            _ => ""
        };
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = _isTested && DataContext is FEndpoint { IsValid: true };
        Close();
    }

    private async void OnSend(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FEndpoint endpoint) return;

        var response = await new RestClient().ExecuteAsync(new FRestRequest(endpoint.Url)
        {
            OnBeforeDeserialization = resp => { resp.ContentType = "application/json; charset=utf-8"; }
        }).ConfigureAwait(false);
        var body = string.IsNullOrEmpty(response.Content) ? JToken.Parse("{}") : JToken.Parse(response.Content);

        Application.Current.Dispatcher.Invoke(delegate
        {
            EndpointResponse.Document ??= new TextDocument();
            EndpointResponse.Document.Text = body.ToString(Formatting.Indented);
        });
    }

    private void OnTest(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FEndpoint endpoint) return;

        endpoint.TryValidate(ApplicationService.ApiEndpointView.DynamicApi, _type, out var response);
        _isTested = true;

        TargetResponse.Document ??= new TextDocument();
        TargetResponse.Document.Text = JsonConvert.SerializeObject(response, Formatting.Indented);
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox { IsLoaded: true } ||
            DataContext is not FEndpoint endpoint) return;
        endpoint.IsValid = false;
    }

    private void OnEvaluator(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = "https://jsonpath.herokuapp.com/", UseShellExecute = true });
    }
}

