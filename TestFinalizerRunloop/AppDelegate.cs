using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AppKit;
using Foundation;

namespace TestFinalizerRunloop
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        WeakReference<NSWindow> _wnd = new WeakReference<NSWindow>(null);
        public AppDelegate()
        {
        }

        [DllImport(ObjCRuntime.Constants.ObjectiveCLibrary)]
        static extern void objc_msgSend(IntPtr handle, IntPtr sel, IntPtr value);

        public override void DidFinishLaunching(NSNotification notification)
        {
            NSNotificationCenter.DefaultCenter.AddObserver(NSWindow.WillCloseNotification, notif =>
            {
                var wnd = (NSWindow)notif.Object;
                _wnd.SetTarget(wnd);
                Console.WriteLine("Closing {0} - ReleaseWhenClosed {1}", wnd, wnd.ReleasedWhenClosed);

                //objc_msgSend(wnd.Handle, ObjCRuntime.Selector.GetHandle("setContentView:"), IntPtr.Zero);
                //wnd.ContentView = null; // xammac throws...
            });

            NSNotificationCenter.DefaultCenter.AddObserver(NSApplication.MainWindowChangedNotification, static notif =>
            {
                Console.WriteLine("Window changed: {0}", notif.Object);
            });

            _ = WaitForGC();

            static async Task WaitForGC()
            {
                while (true)
                {
                    await Task.Delay(1000);

                    Console.WriteLine("[GC] Running GC...");

                    using var pool = new NSAutoreleasePool();

                    _ = new int[100000];

                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                }
            }
        }

        public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
        {
            Console.WriteLine("ShouldTerminate: {0}", sender);

            return false;
        }
    }
}

