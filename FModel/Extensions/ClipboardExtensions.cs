using System;
using System.IO;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FModel.Views;
using Serilog;

namespace FModel.Extensions;

public static class ClipboardExtensions
{
    /// <summary>
    /// Copies text to the system clipboard. Fire-and-forget; runs on the UI thread.
    /// </summary>
    public static void SetText(string text)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                var clipboard = MainWindow.YesWeCats?.Clipboard;
                if (clipboard == null)
                    return;

                await clipboard.SetTextAsync(text);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to copy text to clipboard");
            }
        });
    }

    /// <summary>
    /// Copies PNG image bytes to the system clipboard. Fire-and-forget; runs on the UI thread.
    /// </summary>
    public static void SetImage(byte[] pngBytes)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                var clipboard = MainWindow.YesWeCats?.Clipboard;
                if (clipboard == null)
                    return;

                var dataObject = new DataObject();
                // Keep both MIME and generic bitmap formats for better cross-app paste compatibility.
                dataObject.Set("image/png", pngBytes);
                dataObject.Set("PNG", pngBytes);
                // The MemoryStream can be disposed after decoding — Bitmap copies
                // the pixel data during construction. The Bitmap itself must stay
                // alive because on X11/Wayland the clipboard uses deferred rendering
                // and the DataObject may be read when another app pastes.
                Bitmap bitmap;
                using (var ms = new MemoryStream(pngBytes))
                    bitmap = new Bitmap(ms);
                dataObject.Set(DataFormats.Bitmap, bitmap);
                await clipboard.SetDataObjectAsync(dataObject);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to copy image to clipboard");
            }
        });
    }
}
