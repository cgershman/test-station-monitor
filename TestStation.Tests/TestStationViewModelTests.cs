using TestStation.Protocol;
using TestStationMonitor;

namespace TestStation.Tests;

[TestClass]
public class TestStationViewModelTests
{
    [TestMethod]
    public void RunTest_SendsRunCommand()
    {
        // Arrange
        var fake = new FakeConnection();
        var dispatcher = new InlineDispatcher();
        var vm = new TestStationViewModel("Station1", fake, dispatcher);

        // Act
        vm.RunTestCommand.Execute(null);

        // The viewmodel should have sent exactly the RUN command
        CollectionAssert.Contains(fake.SentMessages, Commands.Run);
    }

    [TestMethod]
    public void MessageReceived_Running_UpdatesStatus()
    {
        var fake = new FakeConnection();
        var dispatcher = new InlineDispatcher();
        var vm = new TestStationViewModel("Station1", fake, dispatcher);

        // Simulate the station reporting it started
        fake.SimulateMessage(TestStationStatus.Running.ToString());

        // The status should be Running
        Assert.AreEqual(TestStationStatus.Running, vm.Status);
    }

    [TestMethod]
    public void MessageReceived_Passed_UpdatesStatus()
    {
        var fake = new FakeConnection();
        var dispatcher = new InlineDispatcher();
        var vm = new TestStationViewModel("Station1", fake, dispatcher);

        // Send passed result
        fake.SimulateMessage(TestStationStatus.Passed.ToString());

        // Status should be passed
        Assert.AreEqual(TestStationStatus.Passed, vm.Status);
    }

    [TestMethod]
    public void MessageReceived_UnknownMessage_DoesNotChangeStatus()
    {
        var fake = new FakeConnection();
        var dispatcher = new InlineDispatcher();
        var vm = new TestStationViewModel("Station1", fake, dispatcher);
        // starts Disconnected

        fake.SimulateMessage("GARBAGE");

        // Enum.TryParse fails on garbage. Status should stay Disconnected
        Assert.AreEqual(TestStationStatus.Disconnected, vm.Status);
    }
}