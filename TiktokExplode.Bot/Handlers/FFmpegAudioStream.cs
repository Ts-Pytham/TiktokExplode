using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace TiktokExplode.Bot.Handlers;

public sealed class FFmpegAudioStream(
    string url, 
    ILogger<FFmpegAudioStream> logger) : IAsyncDisposable
{

    private Process? _process;
    private Stream? _output;
    private Task? _stderrTask;
    private readonly ILogger _logger = logger;

    private bool _started;
    private bool _disposed;


    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_started)
            throw new InvalidOperationException("FFmpeg session already started.");

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments =
                $"-hide_banner " +
                $"-loglevel error " +
                $"-i \"{url}\" " +
                "-ac 2 " +
                "-f s16le " +
                "-ar 48000 " +
                "pipe:1",

            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start ffmpeg.");

        _output = _process.StandardOutput.BaseStream;

        _stderrTask = ConsumeErrorsAsync(_process, cancellationToken);

        _started = true;

        await Task.CompletedTask;
    }

    public async Task PipeToAsync(
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!_started)
            throw new InvalidOperationException("FFmpeg session not started.");

        if (_output is null)
            throw new InvalidOperationException("FFmpeg output stream not available.");

        await _output.CopyToAsync(destination, cancellationToken);

        await destination.FlushAsync(cancellationToken);
    }

    private async Task ConsumeErrorsAsync(
        Process process,
        CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await process.StandardError.ReadLineAsync();

                if (line is null)
                    break;

                _logger.LogInformation("[FFmpeg] {Line}", line);
            }
        }, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            if (_process is not null && !_process.HasExited)
            {
                _process.Kill(true);

                await _process.WaitForExitAsync();
            }
        }
        catch
        {
            _logger.LogWarning("Failed to kill ffmpeg process. It may have already exited.");
        }

        if (_output is not null)
            await _output.DisposeAsync();

        if (_stderrTask is not null)
        {
            try
            {
                await _stderrTask;
            }
            catch
            {
                _logger.LogWarning("Error while consuming ffmpeg stderr. It may have been killed.");
            }
        }

        _process?.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
