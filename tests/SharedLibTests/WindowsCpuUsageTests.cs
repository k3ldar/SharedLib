using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Shared.Classes;

using SharedLib.Win;

namespace SharedLibTests.CpuUsage
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class WindowsCpuUsageTests
    {
        private WindowsCpuUsage _winCpuUsage;

        [TestInitialize]
        public void TestStart()
        {
            _winCpuUsage = new WindowsCpuUsage();
            ThreadManager.Initialise(_winCpuUsage);
            ThreadManager.MaximumRunningThreads = 200;
            ThreadManager.ThreadCpuChangeNotification = 1;
        }

        [TestCleanup]
        public void TestEnd()
        {
            _winCpuUsage = null;
            ThreadManager.Finalise();
        }

        [TestMethod]
        public void Construct_ValidInstance_Success()
        {
            WindowsCpuUsage sut = new WindowsCpuUsage();
        }

        [TestMethod]
        public void ThreadUsageCount_Returns_CorrectCount_Success()
        {
            TestThread testThread = new TestThread();

            List<ThreadManager> threads = new List<ThreadManager>();

            for (int i = 0; i < 100; i++)
            {
                ThreadManager thread = new TestThread();
                threads.Add(thread);
                ThreadManager.ThreadStart(thread, $"Thread {i}", ThreadPriority.BelowNormal, true);
            }

            _winCpuUsage.ThreadAdd(testThread);

            ThreadManager.ThreadStart(testThread, "just a test", System.Threading.ThreadPriority.Normal);

            Assert.IsTrue(_winCpuUsage.ThreadUsageCount() > 1);

            DateTime endTest = DateTime.Now.AddSeconds(3);

            while (DateTime.Now < endTest)
            {
                _winCpuUsage.GetProcessUsage();
                Thread.Sleep(20);
            }

            Thread.Sleep(200);

            testThread.CancelThread();
            Assert.IsTrue(testThread.Cancelled);
            _winCpuUsage.ThreadRemove(testThread);

            for (int i = 0; i < _winCpuUsage.ThreadUsageCount(); i++)
            {
                ThreadManager thread = _winCpuUsage.ThreadUsageGet(i);
                Debug.Print(thread.ToString());
            }

            ThreadManager.CancelAll();

            Debug.Print(testThread.ToString());
        }
    }

    [ExcludeFromCodeCoverage]
    public class TestThread : ThreadManager
    {
        public TestThread()
            : base(null, new TimeSpan(0, 0, 0, 0, 50))
        {

        }

        protected override bool Run(object parameters)
        {
            string file = Path.GetTempFileName();

            int counter = 0;

            for (int i = 0; i < 10000000; i++)
            {
                using (StreamWriter writer = System.IO.File.AppendText(file))
                {
                    writer.Write("hello");
                    writer.Flush();
                }

                counter++;

                if (Cancelled)
                    break;
            }

            Debug.Print($"Counter: {counter}");
            return !Cancelled;
        }
    }
}
