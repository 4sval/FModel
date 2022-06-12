using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace FModel.Framework;

public class AsyncQueue<T> : IAsyncEnumerable<T>
{
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly BufferBlock<T> _buffer = new();

    public int Count => _buffer.Count;

    public void Enqueue(T item) => _buffer.Post(item);

    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            while (Count > 0)
            {
                token.ThrowIfCancellationRequested();
                yield return await _buffer.ReceiveAsync(token);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}