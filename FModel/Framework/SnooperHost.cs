using System;
using System.Threading;
using Editor;
using FModel.Views.Resources.Controls;
using OpenTK.Windowing.Desktop;
using Serilog;
using Serilog.Context;

namespace FModel.Framework;

/// <summary>
/// this basically opens Snooper, survives it crashing, and opens it again without taking FModel down with it.
/// clauded but fixes real problems I had with simpler implementations
///
/// The simple version (a lazy window on a throwaway thread) does not work. A crash left FModel unable to open its
/// own menu items, and Snooper could not be started again without restarting FModel. Both come from the thread
/// dying: GLFW and the GL driver keep per-thread state there. So the thread Snooper is created on never dies until
/// FModel itself closes, it just loops back to waiting for the next window to build.
/// </summary>
public sealed class SnooperHost(Func<EditorWindow> build) : IDisposable
{
    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _wanted = new(0);
    private readonly ManualResetEventSlim _ready = new();

    private volatile EditorWindow? _window;
    private Exception? _failure;
    private Thread? _thread;

    public EditorWindow Window
    {
        get
        {
            lock (_lock)
            {
                if (_window is { } current) return current;

                _thread ??= StartThread();
                _ready.Reset();
                _wanted.Release();
                _ready.Wait();

                return _window ?? throw new InvalidOperationException("Snooper failed to start", _failure);
            }
        }
    }

    private Thread StartThread()
    {
        // this thread must never die until fmodel closes
        // if it dies first, opening any menu hangs the ui thread inside UiaReturnRawElementProvider, which is
        // wpf building the popup, and exiting hangs too (dead thread leaves com state behind???)
        var thread = new Thread(WindowLoop) { IsBackground = true, Name = "Snooper" };
        thread.Start();

        return thread;
    }

    private void WindowLoop()
    {
        using var _ = LogContext.PushProperty("SourceContext", "Snooper");
        GLFWProvider.CheckForMainThread = false;

        while (true)
        {
            _wanted.Wait();

            try
            {
                _failure = null;
                _window = build();
            }
            catch (Exception e)
            {
                _failure = e;
                continue;
            }
            finally
            {
                _ready.Set();
            }

            try
            {
                _window?.Run();
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Crashed");
                FLogger.Append(e);
            }
            finally
            {
                _window?.Dispose();
                _window = null; // the next caller asks for a new one, on this same thread
            }
        }
    }

    public void Dispose()
    {
        if (_window is not { } window) return;

        window.Shutdown();
        SpinWait.SpinUntil(() => _window == null, TimeSpan.FromSeconds(10));
    }
}
