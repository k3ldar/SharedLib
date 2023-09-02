﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Shared.Classes;

namespace SharedLib.Win.Classes
{
    public sealed class WindowsRunProgram : IRunProgram
    {
        public int Run(string programName, string parameters, bool useShellExecute, bool waitForFinish, int timeoutMilliseconds)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(programName);

            if (!String.IsNullOrEmpty(parameters))
            {
                processStartInfo.Arguments = parameters;
            }


            if (waitForFinish)
            {
                processStartInfo.RedirectStandardError = true;
                processStartInfo.UseShellExecute = false;
                Process process = Process.Start(processStartInfo);
                _ = process.StandardError.ReadToEnd();
                process.WaitForExit(timeoutMilliseconds);
                return process.ExitCode;
            }

            processStartInfo.UseShellExecute = useShellExecute;
            Process.Start(processStartInfo);
            return Int32.MinValue;
        }
    }
}
