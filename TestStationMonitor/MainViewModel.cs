using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TestStationMonitor;

public class MainViewModel
{
    private static int _tsCount = 0;
    private bool _isRunningAll;

    public ObservableCollection<TestStationViewModel> TestStations { get; } = [];

    public ICommand AddTestStationCommand { get; }
    public ICommand RunAllCommand { get; }

    public MainViewModel()
    {
        AddTestStationCommand = new RelayCommand(AddTestStation);
        RunAllCommand = new RelayCommand(RunAll, () => TestStations.Count > 0 && !_isRunningAll);
    }

    private void AddTestStation()
    {
        _tsCount++;
        TestStation testStation = new(_tsCount.ToString());
        TestStations.Add(new TestStationViewModel(testStation));
        CommandManager.InvalidateRequerySuggested();
    }

    private async void RunAll()
    {
        _isRunningAll = true;
        CommandManager.InvalidateRequerySuggested();

        try {
            foreach(TestStationViewModel ts in TestStations)
                ts.RunTest();
            
            var allRunning = TestStations
                .Select(s => s.CurrentRun)
                .Where(t => t != null)
                .Cast<Task>();
        
            await Task.WhenAll(allRunning);
        }
        finally
        {
            _isRunningAll = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}