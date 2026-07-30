/////////// !! THIS FILE WRITTEN BY CLAUDE !! /////////////

using TestStation.Protocol;

namespace TestStation.Tests;

// A fake IConnection: records what was sent, lets the test simulate
// messages arriving from the "station."
public class FakeConnection : IConnection
{
    public List<string> SentMessages { get; } = new();
    public bool ConnectCalled { get; private set; }
    public bool Disposed { get; private set; }

    public event Action<string>? MessageReceived;
    public event Action? Disconnected;

    public Task<bool> ConnectAsync(string endpoint, CancellationToken ct = default)
    {
        ConnectCalled = true;
        return Task.FromResult(true);   // pretend we connected
    }

    public Task<bool> SendAsync(string message)
    {
        SentMessages.Add(message);      // record it so the test can assert
        return Task.FromResult(true);
    }

    // Test helpers — let the test drive the viewmodel from the "station" side
    public void SimulateMessage(string message) => MessageReceived?.Invoke(message);
    public void SimulateDisconnect() => Disconnected?.Invoke();

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}