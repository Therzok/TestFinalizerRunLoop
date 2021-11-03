using System;
using System.Runtime.InteropServices;
using AppKit;
using Foundation;

namespace TestFinalizerRunloop
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        public AppDelegate()
        {
        }

        [DllImport(ObjCRuntime.Constants.ObjectiveCLibrary)]
        static extern void objc_msgSend(IntPtr handle, IntPtr sel, IntPtr value);

        public override void DidFinishLaunching(NSNotification notification)
        {
            NSNotificationCenter.DefaultCenter.AddObserver(NSWindow.WillCloseNotification, static notif =>
            {
                var wnd = (NSWindow)notif.Object;
                Console.WriteLine("Closing {0} - ReleaseWhenClosed {1}", wnd, wnd.ReleasedWhenClosed);

                //objc_msgSend(wnd.Handle, ObjCRuntime.Selector.GetHandle("setContentView:"), IntPtr.Zero);
                //wnd.ContentView = null; // xammac throws...
            });

            NSNotificationCenter.DefaultCenter.AddObserver(NSApplication.MainWindowChangedNotification, static notif =>
            {
                Console.WriteLine("Window changed: {0}", notif.Object);
            });

            // Insert code here to initialize your application
        }

        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
        }

        public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
        {
            Console.WriteLine("ShouldTerminate: {0}", sender);

            return false;
        }
    }
}

