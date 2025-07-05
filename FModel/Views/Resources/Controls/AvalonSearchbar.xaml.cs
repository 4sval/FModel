using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using FModel.ViewModels;

namespace FModel.Views.Resources.Controls;

public partial class AvalonSearchbar
{
    public AvalonSearchbar()
    {
        InitializeComponent();
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

    private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!(bool) e.NewValue) return;

        SearchTextBox.Focus();
        SearchTextBox.SelectAll();
    }
}
