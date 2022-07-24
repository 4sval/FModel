using System.Text;
using System.Windows.Input;

namespace FModel.Framework;

public class Hotkey : ViewModel
{
    private Key _key;
    public Key Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    private ModifierKeys _modifiers;
    public ModifierKeys Modifiers
    {
        get => _modifiers;
        set => SetProperty(ref _modifiers, value);
    }

    public Hotkey(Key key, ModifierKeys modifiers = ModifierKeys.None)
    {
        Key = key;
        Modifiers = modifiers;
    }

    public bool IsTriggered(Key e)
    {
        return e == Key && Keyboard.Modifiers.HasFlag(Modifiers);
    }

    public override string ToString()
    {
        var str = new StringBuilder();

        if (Modifiers.HasFlag(ModifierKeys.Control))
            str.Append("Ctrl + ");
        if (Modifiers.HasFlag(ModifierKeys.Shift))
            str.Append("Shift + ");
        if (Modifiers.HasFlag(ModifierKeys.Alt))
            str.Append("Alt + ");
        if (Modifiers.HasFlag(ModifierKeys.Windows))
            str.Append("Win + ");

        str.Append(Key);
        return str.ToString();
    }
}