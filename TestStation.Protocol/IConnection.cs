namespace TestStation.Protocol;

public interface IConnection : IAsyncDisposable
{
    Task<bool> ConnectAsync(string endpoint, CancellationToken ct = default);
    Task<bool> SendAsync(string message);
    event Action<string>? MessageReceived;
    event Action? Disconnected;
}