﻿using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace NetSpeed.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Constants and Fields

        /// <summary>The event mutex name.</summary>
        private const string UniqueEventName = "{EA63DE40-641A-495C-8A37-5D7885646077}";

        /// <summary>The unique mutex name.</summary>
        private const string UniqueMutexName = "{4FDF8CF5-322E-4AA0-91BC-D059AD4768FE}";

        /// <summary>The event wait handle.</summary>
        private EventWaitHandle eventWaitHandle;

        /// <summary>The mutex.</summary>
        private Mutex mutex;

        #endregion

        #region Methods

        /// <summary>The app on startup.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void AppOnStartup(object sender, StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .WriteTo.Console()
#endif
                .WriteTo.File(Path.Combine(AppConfig.Default.AppDataFolder, "nsv.log"), fileSizeLimitBytes: 1048576, shared: true)
                .CreateLogger();

            bool isOwned;
            mutex = new Mutex(true, UniqueMutexName, out isOwned);
            eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);

            // So, R# would not give a warning that this variable is not used.
            GC.KeepAlive(mutex);

            if (isOwned)
            {
                Log.Information("NetSpeed app is already running.");
                // Spawn a thread which will be waiting for our event
                var thread = new Thread(
                    () =>
                    {
                        while (eventWaitHandle.WaitOne())
                        {
                            Current.Dispatcher.BeginInvoke(
                                (Action)(() => ((MainWindow)Current.MainWindow).BringToForeground()));
                        }
                    });

                // It is important mark it as background otherwise it will prevent app from exiting.
                thread.IsBackground = true;

                thread.Start();
                return;
            }

            // Notify other instance so it could bring itself to foreground.
            eventWaitHandle.Set();

            // Terminate this instance.
            Shutdown();
        }

        #endregion
    }
}
