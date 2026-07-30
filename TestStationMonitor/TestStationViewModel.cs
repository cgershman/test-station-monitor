using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TestStation.Protocol;

namespace TestStationMonitor;

public class TestStationViewModel : INotifyPropertyChanged
{
    private TestStationStatus _status = TestStationStatus.Disconnected;
    private IConnection _connection;
    private IUiDispatcher _dispatcher;

    public string Endpoint { get; protected set; }

    public TestStationStatus Status
    {
        get => _status;
        set 
        { 
            _status = value; 
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public Task? CurrentRun { get; private set; }

    public ICommand RunTestCommand { get; }

    public TestStationViewModel(string endpoint, IConnection connection, IUiDispatcher dispatcher)
    {
        Endpoint = endpoint;
        _connection = connection;
        _dispatcher = dispatcher;

        _connection.MessageReceived += OnMessageReceived;
        _connection.Disconnected += OnDisconnected;

        RunTestCommand = new RelayCommand(RunTest, () => 
            Status != TestStationStatus.Running 
            && Status != TestStationStatus.Disconnected);
    }

    public async Task InitializeAsync()
    {
        await GetStatus();
    }

    public async void RunTest()
    {
        if (Status == TestStationStatus.Running)
            return;

        await _connection.SendAsync(Commands.Run);
    }

    private void OnMessageReceived(string message)
    {
        _dispatcher.Invoke(() =>
        {
            if (Enum.TryParse<TestStationStatus>(message, out var status))
                Status = status;

            CommandManager.InvalidateRequerySuggested();
        });
    }

    private void OnDisconnected()
    {
        _dispatcher.Invoke(() =>
        {
            Status = TestStationStatus.Disconnected;
        });
    }

    private async Task GetStatus()
    {
        await _connection.SendAsync(Commands.Status);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}