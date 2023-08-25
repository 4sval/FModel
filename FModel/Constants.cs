using System.Numerics;
using System.Reflection;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace FModel;

public static class Constants
{
    public static readonly string APP_VERSION = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    public const string ZERO_64_CHAR = "0000000000000000000000000000000000000000000000000000000000000000";
    public static readonly FGuid ZERO_GUID = new(0U);

    public const float SCALE_DOWN_RATIO = 0.01F;
    public const int SAMPLES_COUNT = 4;

    public const string WHITE = "#DAE5F2";
    public const string GRAY = "#BBBBBB";
    public const string RED = "#E06C75";
    public const string GREEN = "#98C379";
    public const string YELLOW = "#E5C07B";
    public const string BLUE = "#528BCC";

    public const string ISSUE_LINK = "https://github.com/4sval/FModel/discussions/categories/q-a";
    public const string DONATE_LINK = "https://fmodel.app/donate";
    public const string DISCORD_LINK = "https://fmodel.app/discord";

    public const string _FN_LIVE_TRIGGER = "fortnite-live.manifest";
    public const string _VAL_LIVE_TRIGGER = "valorant-live.manifest";

    public const string _NO_PRESET_TRIGGER = "Hand Made";

    public static int PALETTE_LENGTH => COLOR_PALETTE.Length;
    public static readonly Vector3[] COLOR_PALETTE =
    {
        new (0.231f, 0.231f, 0.231f), // Dark gray
        new (0.376f, 0.490f, 0.545f), // Teal
        new (0.957f, 0.263f, 0.212f), // Red
        new (0.196f, 0.804f, 0.196f), // Green
        new (0.957f, 0.647f, 0.212f), // Orange
        new (0.612f, 0.153f, 0.690f), // Purple
        new (0.129f, 0.588f, 0.953f), // Blue
        new (1.000f, 0.920f, 0.424f), // Yellow
        new (0.824f, 0.412f, 0.118f), // Brown
        new (0.612f, 0.800f, 0.922f)  // Light blue
    };
}
