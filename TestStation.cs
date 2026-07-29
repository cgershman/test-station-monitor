namespace TestStationMonitor;

public enum TestStationStatus
{
    Idle,
    Running,
    Passed,
    Failed
}

public class TestStation
{
    public string Name { get; }
    public TestStationStatus Status { get; set; } = TestStationStatus.Idle;

    public TestStation(string name)
    {
        Name = name;
    }
}
