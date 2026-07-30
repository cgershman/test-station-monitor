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

        // Assert — the viewmodel should have sent exactly the RUN command
        CollectionAssert.Contains(fake.SentMessages, Commands.Run);
    }

    [TestMethod]
    public void MessageReceived_Running_UpdatesStatus()
    {
        var fake = new FakeConnection();
        var dispatcher = new InlineDispatcher();
        var vm = new TestStationViewModel("Station1", fake, dispatcher);

        // Act — simulate the station reporting it started
        fake.SimulateMessage(TestStationStatus.Running.ToString());

        // Assert
        Assert.AreEqual(TestStationStatus.Running, vm.Status);
    }

    [TestMethod]
    public void MessageReceived_Passed_UpdatesStatus()
    {
        var fake = new FakeConnection();
        var dispatcher = new InlineDispatcher();
        var vm = new TestStationViewModel("Station1", fake, dispatcher);

        fake.SimulateMessage(TestStationStatus.Passed.ToString());

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

        // Enum.TryParse fails on garbage → status should stay Disconnected
        Assert.AreEqual(TestStationStatus.Disconnected, vm.Status);
    }
}