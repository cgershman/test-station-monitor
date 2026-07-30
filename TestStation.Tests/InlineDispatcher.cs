namespace TestStationMonitor;

public class InlineDispatcher : IUiDispatcher
{
    public void Invoke(Action action) => action();   // run synchronously, no UI thread in test
}