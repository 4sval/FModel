using System;
using System.Windows;
using FModel.Extensions;
using FModel.Framework;
using FModel.ViewModels.ApiEndpoints.Models;
using ICSharpCode.AvalonEdit.Document;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace FModel.Views.Resources.Controls;

public partial class EndpointEditor
{
    private readonly AesResponse _defaultAesResponse = new ();
    private readonly MappingsResponse[] _defaultMappingResponse = {new ()};
    private JObject _body;

    public FEndpoint Endpoint { get; private set; }

    public EndpointEditor(FEndpoint endpoint, string title, EEndpointType type)
    {
        DataContext = endpoint;

        InitializeComponent();

        Title = title;
        EndpointUrl.Text = endpoint.Url;
        TargetResponse.SyntaxHighlighting = EndpointResponse.SyntaxHighlighting = AvalonExtensions.HighlighterSelector("json");
        TargetResponse.Document = new TextDocument
        {
            Text = JsonConvert.SerializeObject(type switch
            {
                EEndpointType.Aes => _defaultAesResponse,
                EEndpointType.Mapping => _defaultMappingResponse,
                _ => throw new NotImplementedException()
            }, Formatting.Indented)
        };
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        Endpoint = new FEndpoint(EndpointUrl.Text);
        DialogResult = true;
        Close();
    }

    private async void OnSend(object sender, RoutedEventArgs e)
    {
        try
        {
            var response = await new RestClient().ExecuteAsync(new FRestRequest(EndpointUrl.Text)
            {
                OnBeforeDeserialization = resp => { resp.ContentType = "application/json; charset=utf-8"; }
            }).ConfigureAwait(false);
            _body = JObject.Parse(response.Content!);

            Application.Current.Dispatcher.Invoke(delegate
            {
                EndpointResponse.Document = new TextDocument
                {
                    Text = _body.ToString(Formatting.Indented)
                };
            });
        }
        catch
        {
            //
        }
    }
}

