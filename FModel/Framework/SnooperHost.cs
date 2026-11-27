using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets.Exports.Texture;
using Editor;
using FModel.Views.Resources.Controls;
using OpenTK.Windowing.Desktop;
using Serilog;
using Serilog.Context;
using Snooper.Rendering;

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
public sealed class SnooperHost(Func<EditorWindow> build, Action<Exception> onStartupFailure) : IDisposable
{
    private readonly Lock _lock = new();
    private readonly AutoResetEvent _wanted = new(false);
    private readonly ManualResetEventSlim _ready = new();
    private readonly ConcurrentQueue<Action> _renderRequests = new();
    private readonly MeshPreviewRenderer _meshPreviews = new();

    private volatile EditorWindow? _window;
    private volatile bool _openRequested;
    private Exception? _failure;
    private Thread? _thread;

    public EditorWindow? GetWindow()
    {
        Exception? failure;
        lock (_lock)
        {
            if (_window is { } current) return current;

            _thread ??= StartThread();
            _ready.Reset();
            _openRequested = true;
            _wanted.Set();
            _ready.Wait();

            if (_window is { } window) return window;

            failure = _failure;
        }

        onStartupFailure(new InvalidOperationException("Snooper failed to start", failure));
        return null;
    }

    public Task<byte[]> RenderMeshPreviewAsync(IFileProvider provider, GameFile file, ETexturePlatform platform)
    {
        if (_meshPreviews.IsReady)
            return _meshPreviews.RenderAsync(provider, file, platform);

        var completion = new TaskCompletionSource<Task<byte[]>>(TaskCreationOptions.RunContinuationsAsynchronously);
        QueueGraphicsWork(() =>
        {
            try
            {
                _meshPreviews.Initialize(loadBindings: _window == null);
                completion.SetResult(_meshPreviews.RenderAsync(provider, file, platform));
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });

        return completion.Task.Unwrap();
    }

    public void CloseMeshPreviews()
    {
        if (_thread is not null)
            QueueGraphicsWork(_meshPreviews.Dispose);
    }

    private void QueueGraphicsWork(Action action)
    {
        lock (_lock)
        {
            _thread ??= StartThread();
            _renderRequests.Enqueue(action);
            if (_window is { } window)
                window.Invoke(RunGraphicsWork);
            else
                _wanted.Set();
        }
    }

    private void RunGraphicsWork()
    {
        if (_renderRequests.TryDequeue(out var action))
            action();
        if (!_renderRequests.IsEmpty && _window is { } window)
            window.Invoke(RunGraphicsWork);
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
            if (!_openRequested && _renderRequests.IsEmpty)
                _wanted.WaitOne();

            if (!_openRequested)
            {
                RunGraphicsWork();
                continue;
            }
            _openRequested = false;

            try
            {
                _failure = null;
                _window = build();
                if (!_renderRequests.IsEmpty)
                    _window.Invoke(RunGraphicsWork);
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
