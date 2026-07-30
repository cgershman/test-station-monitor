/////////// !! THIS FILE WRITTEN BY CLAUDE !! /////////////

using System.Net.Sockets;

namespace TestStation.Protocol;

public class TcpConnection : IConnection
{
    private const int TimeoutMs = 5000;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private string? _endpoint;
    private CancellationTokenSource _cts = new();
    private Task? _listenTask;
    private bool _disposed = false;

    public event Action<string>? MessageReceived;
    public event Action? Disconnected;

    public async Task<bool> ConnectAsync(string endpoint, CancellationToken ct = default)
    {
        if (_client != null || _disposed)
            return false;

        // endpoint format: "host:port", e.g. "127.0.0.1:5000"
        var parts = endpoint.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out int port))
            return false;
        string host = parts[0];

        try
        {
            _client = new TcpClient();

            // honor both the caller's token and a connect timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeoutMs);

            await _client.ConnectAsync(host, port, timeoutCts.Token);

            _stream = _client.GetStream();
            _reader = new StreamReader(_stream);
            _writer = new StreamWriter(_stream) { AutoFlush = true };

            _endpoint = endpoint;
            _listenTask = ListenAsync(_cts.Token);

            return true;
        }
        catch (SocketException) { /* connection refused / host unreachable */ }
        catch (OperationCanceledException) { /* timed out or caller canceled */ }

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _cts.Cancel();

        if (_listenTask != null)
        {
            try { await _listenTask; }
            catch { /* exceptions already handled inside ListenAsync */ }
        }

        if (_writer != null)
        {
            try { await _writer.DisposeAsync(); }
            catch (IOException) { /* broken connection */ }
        }

        _reader?.Dispose();
        _stream?.Dispose();
        _client?.Dispose();

        _cts.Dispose();
    }

    public async Task<bool> SendAsync(string message)
    {
        var writer = _writer;
        if (writer == null)
            return false;

        try
        {
            await writer.WriteLineAsync(message);
            return true;
        }
        catch (IOException)
        {
            Disconnected?.Invoke();
        }
        catch (ObjectDisposedException)
        {
            Disconnected?.Invoke();
        }

        return false;
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        try
        {
            string? line;
            while (!ct.IsCancellationRequested &&
                   (line = await _reader!.ReadLineAsync(ct)) != null)
            {
                MessageReceived?.Invoke(line);
            }
        }
        catch (OperationCanceledException) { /* cancelled */ }
        catch (IOException) { /* connection lost */ }
        finally
        {
            if (!ct.IsCancellationRequested)
                Disconnected?.Invoke();
        }
    }
}