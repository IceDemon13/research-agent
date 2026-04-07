namespace Telemart.Client.SingleInstance
{
    public interface ISingleInstanceApp
    {
        bool SignalExternalCommandLineArgs();
    }
}