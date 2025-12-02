using System;
using System.Threading;
using System.Windows.Forms;

namespace SuperGrate
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] parameters)
        {
            Win32Interop.SetThreadExecutionState(
                Win32Interop.EXECUTION_STATE.ES_CONTINUOUS |
                Win32Interop.EXECUTION_STATE.ES_SYSTEM_REQUIRED |
                Win32Interop.EXECUTION_STATE.ES_DISPLAY_REQUIRED
            );
            Config.LoadConfig(parameters);
            Config.LoadLanguage();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += Application_ThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.Run(new Main());
        }
        /// <summary>
        /// Thread Exception event for uncaught UI errors.
        /// </summary>
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Logger.Error(e.Exception.Message + "\r", true);
            Logger.Error(e.Exception.StackTrace + "\r\r", true);
            if (e.Exception.InnerException != null)
            {
                Logger.Error(e.Exception.InnerException.Message + "\r", true);
                Logger.Error(e.Exception.InnerException.StackTrace + "\r\r", true);
            }
        }
    }
}
