using System.Text.RegularExpressions;

namespace TestStation.Protocol;

public partial class ConnectionFactory : IConnectionFactory
{
    [GeneratedRegex(@"^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3}):(\d{1,5})$")]
    private static partial Regex IpPortRegex();

    public IConnection Create(string endpoint) =>
        IpPortRegex().IsMatch(endpoint)
            ? new TcpConnection()
            : new PipesConnection();
}