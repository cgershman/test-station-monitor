using System.IO.Pipes;
using TestStation.Protocol;

const int RunDurationMinMs = 1000;
const int RunDurationMaxMs = 10000;

string pipeName = args.Length > 0 ? args[0] : "Station1";

Console.WriteLine($"[{pipeName}] Starting. Waiting for a connection...");

using var server = new NamedPipeServerStream(
    pipeName,
    PipeDirection.InOut,
    maxNumberOfServerInstances: 1,
    PipeTransmissionMode.Byte,
    PipeOptions.Asynchronous);

await server.WaitForConnectionAsync();
Console.WriteLine($"[{pipeName}] Client connected.");

using var reader = new StreamReader(server);
using var writer = new StreamWriter(server) { AutoFlush = true };

var status = TestStationStatus.Idle;

try
{
    string? line;
    while ((line = await reader.ReadLineAsync()) != null)
    {
        string command = line.Trim().ToUpperInvariant();
        Console.WriteLine($"[{pipeName}] Received: {command}");

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
    Console.WriteLine($"[{pipeName}] Connection lost: {ex.Message}");
}
finally
{
    try { writer.Dispose(); } catch (IOException) { }
    reader.Dispose();
    Console.WriteLine($"[{pipeName}] Client disconnected.");
}