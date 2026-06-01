using Discord;
using Discord.Audio;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using TiktokExplode.Bot.Handlers;
using TiktokExplode.Bot.Models;
using TiktokExplode.Domain.Abstractions;

namespace TiktokExplode.Bot.Services;

public sealed class MusicPlayer : IAsyncDisposable
{
    private readonly ILogger<MusicPlayer> _logger;
    private readonly Channel<QueueItem> _queue;
    private readonly List<QueueItem> _queueSnapshot; // para mostrar la queue
    private readonly SemaphoreSlim _queueLock = new(1, 1);

    private IAudioClient? _audioClient;
    private FFmpegAudioStream? _currentStream;
    private CancellationTokenSource _skipCts = new();
    private CancellationTokenSource _stopCts = new();
    private readonly IVoiceChannel _voiceChannel;
    private readonly ulong _guildId;
    private Task? _loopTask;

    private readonly Timer _inactivityTimer;
    private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(5);

    public event Func<QueueItem, Task>? OnTrackStarted;
    public event Func<Task>? OnQueueEmpty;
    public event Func<Task>? OnDisposed;

    public MusicPlayer(
    IVoiceChannel voiceChannel,
    ulong guildId,
    ILogger<MusicPlayer> logger)
    {
        _voiceChannel = voiceChannel;
        _guildId = guildId;
        _logger = logger;
        _queue = Channel.CreateUnbounded<QueueItem>();
        _queueSnapshot = [];
        _inactivityTimer = new Timer(_ => _ = DisposeAsync().AsTask(),
            null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public async Task StartAsync()
    {
        _audioClient = await _voiceChannel.ConnectAsync();
        _audioClient.Disconnected += eventArgs =>
        {
            _audioClient = null;
            return Task.CompletedTask;
        };

        _stopCts = new CancellationTokenSource();
        _loopTask = RunLoopAsync(_stopCts.Token);
        _inactivityTimer.Change(InactivityTimeout, Timeout.InfiniteTimeSpan);
    }

    public async Task EnqueueAsync(QueueItem item)
    {
        await _queueLock.WaitAsync();
        _queueSnapshot.Add(item);
        _queueLock.Release();
        await _queue.Writer.WriteAsync(item);
        // Cancelar timer de inactividad mientras hay canciones pendientes
        _inactivityTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public Task SkipAsync()
    {
        _skipCts.Cancel();
        return Task.CompletedTask;
    }

    // Para la canción actual y vacía la cola, pero el player sigue vivo
    // para aceptar nuevas canciones. El shutdown total ocurre en DisposeAsync.
    public async Task StopAsync()
    {
        // Vaciar el snapshot
        await _queueLock.WaitAsync();
        _queueSnapshot.Clear();
        _queueLock.Release();

        // Drenar el channel sin sellarlo
        while (_queue.Reader.TryRead(out _)) { }

        // Cancelar la canción actual
        await _skipCts.CancelAsync();
    }

    public void Pause() => _currentStream?.Pause();
    public void Resume() => _currentStream?.Resume();

    public async Task<IReadOnlyList<QueueItem>> GetQueueAsync()
    {
        await _queueLock.WaitAsync();
        try { return _queueSnapshot.AsReadOnly(); }
        finally { _queueLock.Release(); }
    }

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_audioClient is not null) return;

        _logger.LogInformation("Conectando al canal de voz...");
        _audioClient = await _voiceChannel.ConnectAsync();
        _audioClient.Disconnected += _ =>
        {
            _audioClient = null;
            return Task.CompletedTask;
        };
        // Dar tiempo a libDave para el handshake E2EE
        await Task.Delay(1000, ct);
    }

    private async Task RunLoopAsync(CancellationToken stopToken)
    {
        // Mantener el PCM stream vivo entre canciones del mismo audio client
        // evita el ciclo speaking=false/true que produce glitches en la transición.
        AudioOutStream? discordStream = null;
        IAudioClient? pcmOwner = null;

        async ValueTask ClosePcmStreamAsync()
        {
            if (discordStream is null) return;
            try { await discordStream.FlushAsync(CancellationToken.None); } catch { }
            discordStream.Dispose();
            discordStream = null;
            pcmOwner = null;
        }

        try
        {
            await foreach (var item in _queue.Reader.ReadAllAsync(stopToken))
            {
                _inactivityTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                _skipCts = new CancellationTokenSource();
                using var linkedCts = CancellationTokenSource
                    .CreateLinkedTokenSource(stopToken, _skipCts.Token);

                try
                {
                    await _queueLock.WaitAsync(stopToken);
                    _queueSnapshot.Remove(item);
                    _queueLock.Release();

                    if (OnTrackStarted is not null)
                        await OnTrackStarted(item);

                    _currentStream = new FFmpegAudioStream(item.Url, _logger);
                    await _currentStream.StartAsync(linkedCts.Token);

                    // Si el audio client cambió (reconexión o primera vez), cerrar PCM stream anterior
                    if (!ReferenceEquals(_audioClient, pcmOwner))
                        await ClosePcmStreamAsync();

                    await EnsureConnectedAsync(linkedCts.Token);

                    if (discordStream is null)
                    {
                        discordStream = _audioClient!.CreatePCMStream(AudioApplication.Mixed);
                        pcmOwner = _audioClient;

                        // Solo al crear el stream: primar estado "hablando" en Discord
                        var silence = new byte[3840];
                        for (int i = 0; i < 25; i++)
                            await discordStream.WriteAsync(silence, linkedCts.Token);
                    }

                    await _currentStream.PipeToAsync(discordStream, linkedCts.Token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reproduciendo {Url}", item.Url);
                    await ClosePcmStreamAsync(); // forzar recreación del stream en error
                }
                finally
                {
                    if (_currentStream is not null)
                    {
                        await _currentStream.DisposeAsync();
                        _currentStream = null;
                    }
                }

                if (_queue.Reader.Count == 0)
                {
                    await ClosePcmStreamAsync();
                    if (OnQueueEmpty is not null)
                        await OnQueueEmpty();
                    _inactivityTimer.Change(InactivityTimeout, Timeout.InfiniteTimeSpan);
                }
            }
        }
        finally
        {
            await ClosePcmStreamAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Parar canción actual y vaciar cola
        await StopAsync();

        // Cerrar el loop de reproducción
        _queue.Writer.TryComplete();
        await _stopCts.CancelAsync();
        if (_loopTask is not null)
            try { await _loopTask; } catch { }

        _inactivityTimer.Dispose();
        _queueLock.Dispose();
        _skipCts.Dispose();
        _stopCts.Dispose();

        if (_audioClient is not null)
        {
            await _audioClient.StopAsync();
            _audioClient.Dispose();
            _audioClient = null;
        }

        if (OnDisposed is not null)
            await OnDisposed();
    }
}