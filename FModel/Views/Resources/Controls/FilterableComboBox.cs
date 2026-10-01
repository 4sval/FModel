using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace FModel.Views.Resources.Controls;

/// <summary>
/// https://stackoverflow.com/a/58066259/13389331
/// </summary>
public class FilterableComboBox : ComboBox
{
    /// <summary>
    /// If true, on lost focus or enter key pressed, checks the text in the combobox. If the text is not present
    /// in the list, it leaves it blank.
    /// </summary>
    public bool OnlyValuesInList
    {
        get => (bool) GetValue(OnlyValuesInListProperty);
        set => SetValue(OnlyValuesInListProperty, value);
    }
    public static readonly DependencyProperty OnlyValuesInListProperty =
        DependencyProperty.Register(nameof(OnlyValuesInList), typeof(bool), typeof(FilterableComboBox));

    /// <summary>
    /// Selected item, changes only on lost focus or enter key pressed
    /// </summary>
    public object EffectivelySelectedItem
    {
        get => (bool) GetValue(EffectivelySelectedItemProperty);
        set => SetValue(EffectivelySelectedItemProperty, value);
    }
    public static readonly DependencyProperty EffectivelySelectedItemProperty =
        DependencyProperty.Register(nameof(EffectivelySelectedItem), typeof(object), typeof(FilterableComboBox));

    private string CurrentFilter = string.Empty;
    private bool TextBoxFreezed;
    protected TextBox EditableTextBox => GetTemplateChild("PART_EditableTextBox") as TextBox;
    private UserChange<bool> IsDropDownOpenUC;
    private readonly DispatcherTimer FilterTimer;

    /// <summary>
    /// Triggers on lost focus or enter key pressed, if the selected item changed since the last time focus was lost or enter was pressed.
    /// </summary>
    public event Action<FilterableComboBox, object> SelectionEffectivelyChanged;

    public FilterableComboBox()
    {
        IsDropDownOpenUC = new UserChange<bool>(v => IsDropDownOpen = v);
        FilterTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(125)
        };
        FilterTimer.Tick += (_, _) =>
        {
            FilterTimer.Stop();
            RefreshFilter();
        };
        DropDownOpened += FilteredComboBox_DropDownOpened;

        Focusable = true;
        IsEditable = true;
        IsTextSearchEnabled = true;
        StaysOpenOnEdit = true;
        IsReadOnly = false;

        Loaded += (s, e) =>
        {
            if (EditableTextBox != null)
                new TextBoxBaseUserChangeTracker(EditableTextBox).UserTextChanged += FilteredComboBox_UserTextChange;
        };

        SelectionChanged += (_, __) => shouldTriggerSelectedItemChanged = true;

        SelectionEffectivelyChanged += (_, o) => EffectivelySelectedItem = o;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (IsDropDownOpen && Keyboard.Modifiers == ModifierKeys.None && e.Key is Key.Up or Key.Down)
        {
            MoveSelection(e.Key);
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
        if (e.Key == Key.Down && !IsDropDownOpen)
        {
            ApplyPendingFilter();
            IsDropDownOpen = true;
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ClearFilter();
            Text = "";
            IsDropDownOpen = true;
        }
        else if (e.Key == Key.Enter || e.Key == Key.Tab)
        {
            ApplyPendingFilter();
            CheckSelectedItem();
            TriggerSelectedItemChanged();
        }
    }

    private void MoveSelection(Key key)
    {
        ApplyPendingFilter();
        if (Items.Count == 0)
            return;

        var index = SelectedItem == null ? -1 : Items.IndexOf(SelectedItem);
        index = index < 0
            ? key == Key.Up ? Items.Count - 1 : 0
            : Math.Clamp(index + (key == Key.Up ? -1 : 1), 0, Items.Count - 1);

        SelectedItem = Items[index];
        (ItemContainerGenerator.ContainerFromIndex(index) as ComboBoxItem)?.BringIntoView();
    }

    protected override void OnPreviewLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnPreviewLostKeyboardFocus(e);
        ApplyPendingFilter();
        CheckSelectedItem();
        if ((e.OldFocus == this || e.OldFocus == EditableTextBox) && e.NewFocus != this && e.NewFocus != EditableTextBox)
            TriggerSelectedItemChanged();
    }

    private void CheckSelectedItem()
    {
        if (OnlyValuesInList)
            Text = SelectedItem?.ToString() ?? "";
    }

    private bool shouldTriggerSelectedItemChanged = false;
    private void TriggerSelectedItemChanged()
    {
        if (shouldTriggerSelectedItemChanged)
        {
            SelectionEffectivelyChanged?.Invoke(this, SelectedItem);
            shouldTriggerSelectedItemChanged = false;
        }
    }

    public void ClearFilter()
    {
        FilterTimer.Stop();
        if (string.IsNullOrEmpty(CurrentFilter))
            return;
        CurrentFilter = "";
        if (ItemsSource != null)
            CollectionViewSource.GetDefaultView(ItemsSource).Refresh();
    }

    private void FilteredComboBox_DropDownOpened(object sender, EventArgs e)
    {
        if (IsDropDownOpenUC.IsUserChange)
            ClearFilter();
    }

    private void FilteredComboBox_UserTextChange(object sender, EventArgs e)
    {
        if (TextBoxFreezed)
            return;
        var tb = EditableTextBox;
        if (tb.SelectionStart + tb.SelectionLength == tb.Text.Length)
            CurrentFilter = tb.Text.Substring(0, tb.SelectionStart);
        else
            CurrentFilter = tb.Text;

        FilterTimer.Stop();
        FilterTimer.Start();
    }

    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        FilterTimer.Stop();

        if (newValue != null)
        {
            var view = CollectionViewSource.GetDefaultView(newValue);
            view.Filter += FilterItem;
        }

        if (oldValue != null)
        {
            var view = CollectionViewSource.GetDefaultView(oldValue);
            if (view != null)
                view.Filter -= FilterItem;
        }

        base.OnItemsSourceChanged(oldValue, newValue);
    }

    private void ApplyPendingFilter()
    {
        if (!FilterTimer.IsEnabled)
            return;
        FilterTimer.Stop();
        RefreshFilter();
    }

    private void RefreshFilter()
    {
        if (ItemsSource == null)
            return;

        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        FreezTextBoxState(() =>
        {
            view.Refresh();

            if (!IsDropDownOpen && !string.IsNullOrEmpty(CurrentFilter))
                IsDropDownOpenUC.Set(true);

            if (SelectedItem == null)
            {
                foreach (var itm in ItemsSource)
                    if (itm.ToString() == Text)
                    {
                        SelectedItem = itm;
                        break;
                    }
            }
        });
    }

    private void FreezTextBoxState(Action action)
    {
        TextBoxFreezed = true;
        var tb = EditableTextBox;
        var text = Text;
        var selStart = tb.SelectionStart;
        var selLen = tb.SelectionLength;
        action();
        Text = text;
        tb.SelectionStart = selStart;
        tb.SelectionLength = selLen;
        TextBoxFreezed = false;
    }

    private bool FilterItem(object value)
    {
        if (value == null)
            return false;
        if (CurrentFilter.Length == 0)
            return true;

        var text = value.ToString();
        if (text?.StartsWith("GAME_", StringComparison.OrdinalIgnoreCase) == true)
            text = text[5..];

        return text?.Contains(CurrentFilter, StringComparison.OrdinalIgnoreCase) == true;
    }

    private class TextBoxBaseUserChangeTracker
    {
        private bool IsTextInput { get; set; }

        public TextBox TextBoxBase { get; set; }
        private List<Key> PressedKeys = new List<Key>();
        public event EventHandler UserTextChanged;
        private string LastText;

        public TextBoxBaseUserChangeTracker(TextBox textBoxBase)
        {
            TextBoxBase = textBoxBase;
            LastText = TextBoxBase.ToString();

            textBoxBase.PreviewTextInput += (s, e) =>
            {
                IsTextInput = true;
            };

            textBoxBase.TextChanged += (s, e) =>
            {
                var isUserChange = PressedKeys.Count > 0 || IsTextInput || LastText == TextBoxBase.ToString();
                IsTextInput = false;
                LastText = TextBoxBase.ToString();
                if (isUserChange)
                    UserTextChanged?.Invoke(this, e);
            };

            textBoxBase.PreviewKeyDown += (s, e) =>
            {
                switch (e.Key)
                {
                    case Key.Back:
                    case Key.Space:
                        if (!PressedKeys.Contains(e.Key))
                            PressedKeys.Add(e.Key);
                        break;
                }
                if (e.Key == Key.Back)
                {
                    var textBox = textBoxBase as TextBox;
                    if (textBox.SelectionStart > 0 && textBox.SelectionLength > 0 && (textBox.SelectionStart + textBox.SelectionLength) == textBox.Text.Length)
                    {
                        textBox.SelectionStart--;
                        textBox.SelectionLength++;
                        e.Handled = true;
                        UserTextChanged?.Invoke(this, e);
                    }
                }
            };

            textBoxBase.PreviewKeyUp += (s, e) =>
            {
                if (PressedKeys.Contains(e.Key))
                    PressedKeys.Remove(e.Key);
            };

            textBoxBase.LostFocus += (s, e) =>
            {
                PressedKeys.Clear();
                IsTextInput = false;
            };
        }
    }

    private class UserChange<T>
    {
        private Action<T> action;

        public bool IsUserChange { get; private set; } = true;

        public UserChange(Action<T> action)
        {
            this.action = action;
        }

        public void Set(T val)
        {
            try
            {
                IsUserChange = false;
                action(val);
            }
            finally
            {
                IsUserChange = true;
            }
        }
    }
}
