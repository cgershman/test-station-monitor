namespace TestStation.Protocol;

public interface IConnectionFactory
{
    IConnection Create(string endpoint);
}