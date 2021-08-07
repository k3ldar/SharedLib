using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Shared.Classes;

using SharedLibTests.CpuUsage;

namespace SharedLibTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class ThreadManagerTests
    {
        [TestMethod]
        public void Initialization_DefaultOptions_StartsThreadSuccessfully()
        {
            ThreadManager.Initialise();
            try
            {
                TestThread testThread = new TestThread();
                ThreadManager.ThreadStart(testThread, nameof(testThread), ThreadPriority.Normal);

                Thread.Sleep(1000);
                testThread.CancelThread();
                ThreadManager.CancelAll();
            }
            finally
            {
                ThreadManager.Finalise();
            }

        }
    }
}
