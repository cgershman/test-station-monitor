using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TestStation.Protocol;

namespace TestStationMonitor;

public class MainViewModel : INotifyPropertyChanged
{
    private IConnectionFactory _connectionFactory = new ConnectionFactory();

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    private string _newStationEndpoint = string.Empty;
    public string NewStationEndpoint
    {
        get => _newStationEndpoint;
        set 
        { 
            if (_newStationEndpoint != value)
            {
                _newStationEndpoint = value;
                OnPropertyChanged();
            } 
        }
    }

    public ObservableCollection<TestStationViewModel> TestStations { get; } = [];

    public ICommand AddTestStationCommand { get; }
    public ICommand RunAllCommand { get; }

    public MainViewModel()
    {
        AddTestStationCommand = new RelayCommand(AddTestStation);
        RunAllCommand = new RelayCommand(RunAll, () => 
            TestStations.Count > 0 
            && !TestStations.All(ts => ts.Status == TestStationStatus.Running || ts.Status == TestStationStatus.Disconnected));
    }

    private async void AddTestStation()
    {
        var endpoint = NewStationEndpoint;
        if (String.IsNullOrEmpty(endpoint))
        {
            StatusMessage = "Enter an endpoint to add a station.";
            return;
        }

        var duplicates = TestStations.Where(ts => ts.Endpoint == endpoint);
        if (duplicates.Count() > 0)
        {
            StatusMessage = "That station has already been added.";
            return;
        }

        IConnection connection = _connectionFactory.Create(endpoint);
        bool connected = await connection.ConnectAsync(endpoint);
        if (!connected)
        {
           StatusMessage = "Could not connect to endpoint.";
           return; 
        }
        
        var dispatcher = new WpfDispatcher();
        var testStation = new TestStationViewModel(endpoint, connection, dispatcher);
        TestStations.Add(testStation);
        await testStation.InitializeAsync();
        CommandManager.InvalidateRequerySuggested();
    }

    private async void RunAll()
    {
        try {
            foreach(TestStationViewModel ts in TestStations)
                ts.RunTest();
        }
        finally
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}