using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TestStationMonitor;

public class TestStationViewModel : INotifyPropertyChanged
{
    private readonly TestStation _testStation;

    public string Name
    {
        get => _testStation.Name;
    }

    public TestStationStatus Status
    {
        get => _testStation.Status;
        set 
        { 
            _testStation.Status = value; 
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public Task? CurrentRun { get; private set; }

    public ICommand RunTestCommand { get; }

    public TestStationViewModel(TestStation testStation)
    {
        _testStation = testStation;
        RunTestCommand = new RelayCommand(RunTest, () => Status != TestStationStatus.Running);
    }

    public void RunTest()
    {
        if (Status == TestStationStatus.Running)
            return;

        CurrentRun = RunTestAsync();
    }

    private async Task RunTestAsync()
    {
        Status = TestStationStatus.Running;
        await Task.Delay(Random.Shared.Next(9000) + 1000);
        bool success = Random.Shared.Next(2) == 0; 
        Status = success ? TestStationStatus.Passed : TestStationStatus.Failed;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}