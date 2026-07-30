namespace TestStationMonitor;

public class WpfDispatcher : IUiDispatcher
{
    public void Invoke(Action action) =>
        System.Windows.Application.Current.Dispatcher.Invoke(action);
}