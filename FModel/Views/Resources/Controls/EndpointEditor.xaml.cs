using System.ComponentModel;
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
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        Close();
        DialogResult = DataContext is FEndpoint { IsValid: true };
    }

    private async void OnSend(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FEndpoint endpoint) return;

        var response = await new RestClient().ExecuteAsync(new FRestRequest(endpoint.Url)
        {
            OnBeforeDeserialization = resp => { resp.ContentType = "application/json; charset=utf-8"; }
        }).ConfigureAwait(false);
        var body = JToken.Parse(response.Content!);

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

    private void OnClosing(object sender, CancelEventArgs e)
    {
        if (!_isTested) OnTest(null, null);
    }
}

