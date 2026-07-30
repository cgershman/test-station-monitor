using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using TestStation.Protocol;

const int RunDurationMinMs = 1000;
const int RunDurationMaxMs = 10000;

string endpoint = args.Length > 0 ? args[0] : "Station1";

// Decide transport the same way the client factory does: ip:port -> TCP, else -> pipe.
var ipPort = new Regex(@"^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3}):(\d{1,5})$");

Console.WriteLine($"[{endpoint}] Starting. Waiting for a connection...");

// Get a connected Stream from whichever transport, then run the shared loop.
Stream stream = ipPort.IsMatch(endpoint)
    ? await AcceptTcpAsync(endpoint)
    : await AcceptPipeAsync(endpoint);

Console.WriteLine($"[{endpoint}] Client connected.");

await RunStationLoopAsync(stream, endpoint);


// ---- transport-specific setup ----

static async Task<Stream> AcceptPipeAsync(string pipeName)
{
    var server = new NamedPipeServerStream(
        pipeName,
        PipeDirection.InOut,
        maxNumberOfServerInstances: 1,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous);

    await server.WaitForConnectionAsync();
    return server;  // NamedPipeServerStream IS a Stream
}

static async Task<Stream> AcceptTcpAsync(string endpoint)
{
    var parts = endpoint.Split(':');
    string host = parts[0];
    int port = int.Parse(parts[1]);

    // Listen on the given address/port and accept one client.
    var listener = new TcpListener(IPAddress.Parse(host), port);
    listener.Start();

    TcpClient client = await listener.AcceptTcpClientAsync();
    listener.Stop();               // we only serve one connection

    return client.GetStream();     // NetworkStream IS a Stream
}


// ---- transport-agnostic command loop (unchanged logic) ----

static async Task RunStationLoopAsync(Stream stream, string name)
{
    using var reader = new StreamReader(stream);
    using var writer = new StreamWriter(stream) { AutoFlush = true };

    var status = TestStationStatus.Idle;

    try
    {
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            string command = line.Trim().ToUpperInvariant();
            Console.WriteLine($"[{name}] Received: {command}");

            switch (command)
            {
                case Commands.Run:
                    status = TestStationStatus.Running;
                    await writer.WriteLineAsync(status.ToString());

                    await Task.Delay(Random.Shared.Next(RunDurationMinMs, RunDurationMaxMs));

                    status = Random.Shared.Next(2) == 0
                        ? TestStationStatus.Passed
                        : TestStationStatus.Failed;

                    await writer.WriteLineAsync(status.ToString());
                    break;

                case Commands.Status:
                    await writer.WriteLineAsync(status.ToString());
                    break;

                default:
                    await writer.WriteLineAsync($"UNKNOWN: {command}");
                    break;
            }
        }
    }
    catch (IOException ex)
    {
        Console.WriteLine($"[{name}] Connection lost: {ex.Message}");
    }
    finally
    {
        try { writer.Dispose(); } catch (IOException) { }
        reader.Dispose();
        stream.Dispose();
        Console.WriteLine($"[{name}] Client disconnected.");
    }
}