namespace Shared.Classes
{
    public interface IRunProgram
    {
        int Run(string programName, string parameters, bool useShellExecute, bool waitForFinish, int timeoutMilliseconds);
    }
}
