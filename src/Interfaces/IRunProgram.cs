namespace Shared.Classes
{
    public interface IRunProgram
    {
        /// <summary>
        /// Runs an executable
        /// </summary>
        /// <param name="programName"></param>
        /// <param name="parameters"></param>
        /// <param name="useShellExecute"></param>
        /// <param name="waitForFinish"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        int Run(string programName, string parameters, bool useShellExecute, bool waitForFinish, int timeoutMilliseconds);

        /// <summary>
        /// Runs an executable (command line) and returns the output
        /// </summary>
        /// <param name="programName">Name of program to run</param>
        /// <param name="parameters">Parameters</param>
        /// <returns></returns>
        string Run(string programName, string parameters);
    }
}
