using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using FModel.Settings;
using Ionic.Zlib;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FModel.ViewModels.ApiEndpoints;

public class ValorantApiEndpoint : AbstractApiProvider
{
    private const string _URL = "https://fmodel.fortnite-api.com/valorant/v2/manifest";

    public ValorantApiEndpoint(IRestClient client) : base(client)
    {
    }

    public async Task<VManifest> GetManifestAsync(CancellationToken token)
    {
        var request = new RestRequest(_URL, Method.GET);
        var response = await _client.ExecuteAsync(request, token).ConfigureAwait(false);
        return new VManifest(response.RawBytes);
    }

    public VManifest GetManifest(CancellationToken token) => GetManifestAsync(token).GetAwaiter().GetResult();
}

public class VManifest
{
    private readonly HttpClient _client;
    public readonly VHeader Header;
    public readonly VChunk[] Chunks;
    public readonly VPak[] Paks;

    public VManifest(byte[] data) : this(new FByteArchive("CompressedValorantManifest", data))
    {
    }

    private VManifest(FArchive Ar)
    {
        using (Ar)
        {
            Header = new VHeader(Ar);
            var compressedBuffer = Ar.ReadBytes((int) Header.CompressedSize);
            var uncompressedBuffer = ZlibStream.UncompressBuffer(compressedBuffer);
            if (uncompressedBuffer.Length != Header.UncompressedSize)
                throw new ParserException(Ar, $"Decompression failed, {uncompressedBuffer.Length} != {Header.UncompressedSize}");

            using var manifest = new FByteArchive("UncompressedValorantManifest", uncompressedBuffer);
            Chunks = manifest.ReadArray<VChunk>((int) Header.ChunkCount);
            Paks = manifest.ReadArray((int) Header.PakCount, () => new VPak(manifest));

            if (manifest.Position != manifest.Length)
                throw new ParserException(manifest, $"Parsing failed, {manifest.Position} != {manifest.Length}");
        }

        _client = new HttpClient(new HttpClientHandler
        {
            UseProxy = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.All,
            CheckCertificateRevocationList = false,
            PreAuthenticate = false,
            MaxConnectionsPerServer = 1337,
            UseDefaultCredentials = false,
            AllowAutoRedirect = false
        });
    }

    public async ValueTask PrefetchChunk(VChunk chunk, CancellationToken cancellationToken)
    {
        var chunkPath = Path.Combine(UserSettings.Default.OutputDirectory, ".data", $"{chunk.Id}.chunk");
        if (File.Exists(chunkPath)) return;
        using var response = await _client.GetAsync(chunk.GetUrl(), cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            await using var fileStream = new FileStream(chunkPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            await response.Content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<byte[]> GetChunkBytes(VChunk chunk, CancellationToken cancellationToken)
    {
        var chunkPath = Path.Combine(UserSettings.Default.OutputDirectory, ".data", $"{chunk.Id}.chunk");
        byte[] chunkBytes;

        if (File.Exists(chunkPath))
        {
            chunkBytes = new byte[chunk.Size];
            await using var fs = new FileStream(chunkPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await fs.ReadAsync(chunkBytes, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            using var response = await _client.GetAsync(chunk.GetUrl(), cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                chunkBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                await using var fs = new FileStream(chunkPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                await response.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                chunkBytes = null; // Maybe add logging?
            }
        }

        return chunkBytes;
    }

    public Stream GetPakStream(int index) => new VPakStream(this, index);
}

public readonly struct VHeader
{
    private const uint _MAGIC = 0xC3D088F7u;

    public readonly uint Magic;
    public readonly uint HeaderSize;
    public readonly ulong ManifestId;
    public readonly uint UncompressedSize;
    public readonly uint CompressedSize;
    public readonly uint ChunkCount;
    public readonly uint PakCount;
    public readonly string GameVersion;

    public VHeader(FArchive Ar)
    {
        Magic = Ar.Read<uint>();

        if (Magic != _MAGIC)
            throw new ParserException(Ar, "Invalid manifest magic");

        HeaderSize = Ar.Read<uint>();
        ManifestId = Ar.Read<ulong>();
        UncompressedSize = Ar.Read<uint>();
        CompressedSize = Ar.Read<uint>();
        ChunkCount = Ar.Read<uint>();
        PakCount = Ar.Read<uint>();
        var gameVersionLength = Ar.ReadByte();
        GameVersion = gameVersionLength == 0 ? null : Encoding.ASCII.GetString(Ar.ReadBytes(gameVersionLength));
        Ar.Position = HeaderSize;
    }
}

public readonly struct VPak
{
    public readonly ulong Id;
    public readonly uint Size;
    public readonly uint[] ChunkIndices;
    public readonly string Name;

    public VPak(FArchive Ar)
    {
        Id = Ar.Read<ulong>();
        Size = Ar.Read<uint>();
        ChunkIndices = Ar.ReadArray<uint>(Ar.Read<int>());
        Name = Encoding.ASCII.GetString(Ar.ReadBytes(Ar.ReadByte()));
    }

    public string GetFullName() => $"ValorantLive/ShooterGame/Content/Paks/{Name}";
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct VChunk
{
    public readonly ulong Id;
    public readonly uint Size;

    public string GetUrl() => $"https://fmodel.fortnite-api.com/valorant/v2/chunks/{Id}";
}

public class VPakStream : Stream, ICloneable
{
    private readonly VManifest _manifest;
    private readonly int _pakIndex;
    private readonly VChunk[] _chunks;

    public VPakStream(VManifest manifest, int pakIndex, long position = 0L)
    {
        _manifest = manifest;
        _pakIndex = pakIndex;
        _position = position;

        var pak = manifest.Paks[pakIndex];
        _chunks = new VChunk[pak.ChunkIndices.Length];
        for (var i = 0; i < _chunks.Length; i++)
        {
            _chunks[i] = manifest.Chunks[pak.ChunkIndices[i]];
        }

        Length = pak.Size;
    }

    public object Clone() => new VPakStream(_manifest, _pakIndex, _position);

    public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var (i, startPos) = GetChunkIndex(_position);
        if (i == -1) return 0;

        await PrefetchAsync(i, startPos, count, cancellationToken).ConfigureAwait(false);
        var bytesRead = 0;

        while (true)
        {
            var chunk = _chunks[i];
            var chunkData = await _manifest.GetChunkBytes(chunk, cancellationToken).ConfigureAwait(false);
            var chunkBytes = chunk.Size - startPos;
            var bytesLeft = count - bytesRead;

            if (bytesLeft <= chunkBytes)
            {
                Unsafe.CopyBlockUnaligned(ref buffer[bytesRead + offset], ref chunkData[startPos], (uint) bytesLeft);
                bytesRead += bytesLeft;
                break;
            }

            Unsafe.CopyBlockUnaligned(ref buffer[bytesRead + offset], ref chunkData[startPos], chunkBytes);
            bytesRead += (int) chunkBytes;
            startPos = 0u;

            if (++i == _chunks.Length) break;
        }

        _position += bytesRead;
        return bytesRead;
    }

    private async Task PrefetchAsync(int i, uint startPos, long count, CancellationToken cancellationToken, int concurrentDownloads = 4)
    {
        var tasks = new List<Task>();
        var s = new SemaphoreSlim(concurrentDownloads);
        while (count > 0)
        {
            await s.WaitAsync(cancellationToken).ConfigureAwait(false);

            var chunk = _chunks[i++];
            tasks.Add(PrefetchChunkAsync(chunk));

            if (i == _chunks.Length) break;
            count -= chunk.Size - startPos;
            startPos = 0u;
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        s.Dispose();

        async Task PrefetchChunkAsync(VChunk chunk)
        {
            await _manifest.PrefetchChunk(chunk, cancellationToken).ConfigureAwait(false);
            s.Release(); // This is intended
        }
    }

    private (int Index, uint ChunkPos) GetChunkIndex(long position)
    {
        for (var i = 0; i < _chunks.Length; i++)
        {
            var size = _chunks[i].Size;
            if (position < size) return (i, (uint) position);
            position -= size;
        }

        return (-1, 0u);
    }

    private long _position;
    public override long Position
    {
        get => _position;
        set
        {
            if (value >= Length || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _position = value;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => offset + _position,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(offset))
        };
        return _position;
    }

    public override long Length { get; }
    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override void Flush() => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}