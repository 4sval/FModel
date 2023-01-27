using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.Serialization;
using FModel.Extensions;
using ICSharpCode.AvalonEdit.Document;
using Newtonsoft.Json;

namespace FModel.Views.Resources.Controls;

public partial class DictionaryEditor
{
    private readonly bool _enableElements;
    private readonly List<FCustomVersion> _defaultCustomVersions;
    private readonly Dictionary<string, bool> _defaultOptions;
    private readonly Dictionary<string, KeyValuePair<string, string>> _defaultMapStructTypes;

    public List<FCustomVersion> CustomVersions { get; private set; }
    public Dictionary<string, bool> Options { get; private set; }
    public Dictionary<string, KeyValuePair<string, string>> MapStructTypes { get; private set; }

    public DictionaryEditor(string title, bool enableElements)
    {
        _enableElements = enableElements;
        _defaultCustomVersions = new List<FCustomVersion> { new() { Key = new FGuid(), Version = 0 } };
        _defaultOptions = new Dictionary<string, bool> { { "key1", true }, { "key2", false } };
        _defaultMapStructTypes = new Dictionary<string, KeyValuePair<string, string>> { { "MapName", new KeyValuePair<string, string>("KeyType", "ValueType") } };

        InitializeComponent();

        Title = title;
        MyAvalonEditor.IsReadOnly = !_enableElements;
        MyAvalonEditor.SyntaxHighlighting = AvalonExtensions.HighlighterSelector("");
    }

    public DictionaryEditor(List<FCustomVersion> customVersions, string title, bool enableElements) : this(title, enableElements)
    {
        MyAvalonEditor.Document = new TextDocument
        {
            Text = JsonConvert.SerializeObject(customVersions ?? _defaultCustomVersions, Formatting.Indented)
        };
    }

    public DictionaryEditor(Dictionary<string, bool> options, string title, bool enableElements) : this(title, enableElements)
    {
        MyAvalonEditor.Document = new TextDocument
        {
            Text = JsonConvert.SerializeObject(options ?? _defaultOptions, Formatting.Indented)
        };
    }

    public DictionaryEditor(Dictionary<string, KeyValuePair<string, string>> options, string title, bool enableElements) : this(title, enableElements)
    {
        MyAvalonEditor.Document = new TextDocument
        {
            Text = JsonConvert.SerializeObject(options ?? _defaultMapStructTypes, Formatting.Indented)
        };
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        if (!_enableElements)
        {
            DialogResult = false;
            Close();
            return;
        }

        try
        {
            switch (Title)
            {
                case "Versioning Configuration (Custom Versions)":
                    CustomVersions = JsonConvert.DeserializeObject<List<FCustomVersion>>(MyAvalonEditor.Document.Text);
                    // DialogResult = !CustomVersions.SequenceEqual(_defaultCustomVersions);
                    DialogResult = true;
                    Close();
                    break;
                case "Versioning Configuration (Options)":
                    Options = JsonConvert.DeserializeObject<Dictionary<string, bool>>(MyAvalonEditor.Document.Text);
                    // DialogResult = !Options.SequenceEqual(_defaultOptions);
                    DialogResult = true;
                    Close();
                    break;
                case "MapStructTypes":
                    MapStructTypes = JsonConvert.DeserializeObject<Dictionary<string, KeyValuePair<string, string>>>(MyAvalonEditor.Document.Text);
                    // DialogResult = !Options.SequenceEqual(_defaultOptions);
                    DialogResult = true;
                    Close();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        catch
        {
            HeBrokeIt.Text = "GG YOU BROKE THE FORMAT, FIX THE JSON OR RESET THE CHANGES!";
            HeBrokeIt.Foreground = new SolidColorBrush((Color) ColorConverter.ConvertFromString(Constants.RED));
        }
    }

    private void OnReset(object sender, RoutedEventArgs e)
    {
        if (!_enableElements)
            return;

        MyAvalonEditor.Document = Title switch
        {
            "Versioning Configuration (Custom Versions)" => new TextDocument
            {
                Text = JsonConvert.SerializeObject(_defaultCustomVersions, Formatting.Indented)
            },
            "Versioning Configuration (Options)" => new TextDocument
            {
                Text = JsonConvert.SerializeObject(_defaultOptions, Formatting.Indented)
            },
            "MapStructTypes" => new TextDocument
            {
                Text = JsonConvert.SerializeObject(_defaultMapStructTypes, Formatting.Indented)
            },
            _ => throw new NotImplementedException()
        };
    }
}
