using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FModel.ViewModels;
using ICSharpCode.AvalonEdit;

namespace FModel.Views.Resources.Controls;

public partial class AvalonSearchbar
{
    public AvalonSearchbar()
    {
        InitializeComponent();

        DataContextChanged += AvalonSearchbar_DataContextChanged;
    }

    private void AvalonSearchbar_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is TabItem oldTab)
            oldTab.PropertyChanged -= TabItem_PropertyChanged;

        if (e.NewValue is TabItem newTab)
            newTab.PropertyChanged += TabItem_PropertyChanged;
    }

    public TextEditor TargetEditor
    {
        get => (TextEditor) GetValue(TargetEditorProperty);
        set => SetValue(TargetEditorProperty, value);
    }

    public static readonly DependencyProperty TargetEditorProperty =
        DependencyProperty.Register(nameof(TargetEditor), typeof(TextEditor), typeof(AvalonSearchbar), new PropertyMetadata(null));

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                FindNext();
                e.Handled = true;
                break;
            case Key.Escape:
                CloseSearch();
                e.Handled = true;
                break;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        CloseSearch();
    }

    private void OnDeleteSearchClick(object sender, RoutedEventArgs e)
    {
        ((TabItem) DataContext).TextToFind = string.Empty;
    }

    private void CloseSearch()
    {
        if (DataContext is TabItem vm)
            vm.HasSearchOpen = false;
    }

    private void FindNext(bool invertLeftRight = false)
    {
        if (TargetEditor == null)
            return;
        if (DataContext is not TabItem vm)
            return;
        if (TargetEditor.Document == null)
            return;
        if (string.IsNullOrEmpty(vm.TextToFind))
            return;

        Regex regex;
        if (invertLeftRight)
        {
            vm.SearchUp = !vm.SearchUp;
            regex = GetRegEx(vm);
            vm.SearchUp = !vm.SearchUp;
        }
        else
        {
            regex = GetRegEx(vm);
        }

        var rightToLeft = regex.Options.HasFlag(RegexOptions.RightToLeft);
        int startIndex = rightToLeft ? TargetEditor.SelectionStart : TargetEditor.SelectionStart + TargetEditor.SelectionLength;

        var match = regex.Match(TargetEditor.Text, startIndex);
        if (match.Success)
        {
            TargetEditor.Select(match.Index, match.Length);
            TargetEditor.TextArea.Caret.BringCaretToView();
        }
        else
        {
            // Reached end/start of document: wrap search
            match = rightToLeft ? regex.Match(TargetEditor.Text, TargetEditor.Text.Length - 1) : regex.Match(TargetEditor.Text, 0);

            if (!match.Success) return;

            TargetEditor.Select(match.Index, match.Length);
            TargetEditor.TextArea.Caret.BringCaretToView();
        }
    }

    private static Regex GetRegEx(TabItem vm, bool forceLeftToRight = false)
    {
        RegexOptions options = RegexOptions.None;

        if (vm.SearchUp && !forceLeftToRight)
            options |= RegexOptions.RightToLeft;
        if (!vm.CaseSensitive)
            options |= RegexOptions.IgnoreCase;

        if (vm.UseRegEx)
        {
            return new Regex(vm.TextToFind, options);
        }

        var escaped = Regex.Escape(vm.TextToFind);
        if (vm.WholeWord)
            escaped = "\\b" + escaped + "\\b";

        return new Regex(escaped, options);
    }

    private void TabItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TabItem.TriggerFocusSearch))
            return;

        if (DataContext is not TabItem)
            return;

        SearchTextBox.Focus();
        SearchTextBox.SelectAll();
    }
}
