using System.IO.Pipes;

namespace TestStation.Protocol;

public class PipesConnection : IConnection
{
    private const int TimeoutMs = 5000;
    private NamedPipeClientStream? _client; 
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private string? _pipeName; 
    private CancellationTokenSource _cts = new();
    private Task? _listenTask;
    private bool _disposed = false;

    public event Action<string>? MessageReceived;
    public event Action? Disconnected;

    public async Task<bool> ConnectAsync(string endpoint, CancellationToken ct = default)
    {
        if (_client != null || _disposed)
            return false;

        try {
            _client = new NamedPipeClientStream(
                ".",
                endpoint,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            await _client.ConnectAsync(TimeoutMs, ct);
            _reader = new StreamReader(_client);
            _writer = new StreamWriter(_client) { AutoFlush = true };

            _pipeName = endpoint;
            _listenTask = ListenAsync(_cts.Token);

            return true;
        }
        catch(IOException) { /* broken pipe */ }
        catch(TimeoutException) { /* failed to connect in time */ }
        catch(OperationCanceledException) { /* canceled by caller */}

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)  
            return;

        _disposed = true;

        _cts.Cancel();

        // wait for listen task to finish
        if (_listenTask != null)
        {
            try { await _listenTask; } // wait for listen loop to finish
            catch { /* any exceptions should already be handled inside ListenAsync */ }
        }

        if (_writer != null)
        {
            try { await _writer.DisposeAsync(); } // flush writer while the pipe is still open
            catch (IOException) { /* broken pipe */ }
        }

        _reader?.Dispose();

        if (_client != null)
            await _client.DisposeAsync();

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
        catch(IOException) 
        { 
            /* broken pipe */ 
            Disconnected?.Invoke();
        } 
        catch(ObjectDisposedException) 
        { 
            /* disoposed during send */
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
        catch (OperationCanceledException) { /* operation cancelled */ }
        catch (IOException) { /* broken pipe */ }
        finally
        {
            if (!ct.IsCancellationRequested)
                Disconnected?.Invoke();
        }
    }
}