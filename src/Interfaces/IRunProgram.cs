namespace Shared
{
    public interface IRunProgram
    {
        int Run(string programName);

        int Run(string programName, string parameters);

        int Run(string programName, string parameters, bool useShellExecute);
    }
}
