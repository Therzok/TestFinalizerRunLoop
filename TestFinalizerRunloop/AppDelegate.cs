using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using ObjCRuntime;

namespace TestFinalizerRunloop
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        List<NSObject> _observers = new List<NSObject>();
        WindowController[] _controllers;
        NSWindow[] _windows;

        public AppDelegate()
        {
        }

        [DllImport(ObjCRuntime.Constants.ObjectiveCLibrary)]
        public static extern void objc_msgSend(IntPtr handle, IntPtr sel, IntPtr value);

        [DllImport(ObjCRuntime.Constants.ObjectiveCLibrary)]
        public static extern IntPtr objc_msgSend(IntPtr handle, IntPtr sel);

        public override void DidFinishLaunching(NSNotification notification)
        {
            //NSWindowController[] _controllers;
            //NSWindow[] _windows;

            _windows = new[]
            {
                new MyWindow(new CGRect(400, 400, 400, 400))
                {
                    Title = "NOT ReleaseWhenClosed",
                    BackgroundColor = NSColor.Red,
                },
                new MyWindow(new CGRect(800, 400, 400, 400))
                {
                    Title = "Normal",
                    BackgroundColor = NSColor.Blue,
                },
            };

            _controllers = Array.ConvertAll(_windows, x => new WindowController(x));

            foreach (var controller in _controllers)
            {
                var window = controller.Window;
                window.ReleasedWhenClosed = true;

                // this causes ref cycle if used from managed
                objc_msgSend(window.Handle, Selector.GetHandle("setDelegate:"), controller.Handle);
                objc_msgSend(window.Handle, Selector.GetHandle("setWindowController:"), controller.Handle);
                //controller.Window.WeakDelegate = controller;

                var viewController = new ViewController(window.ContentView);
                controller.ContentViewController = viewController;
            }

            var observer = NSWindow.Notifications.ObserveWillClose(static (sender, args) =>
            {
                var wnd = (NSWindow)args.Notification.Object;
                Console.WriteLine("Controller {0}", wnd.WindowController);
                Console.WriteLine("Closing {0} - ReleaseWhenClosed {1}", wnd, wnd.ReleasedWhenClosed);

                //objc_msgSend(wnd.Handle, ObjCRuntime.Selector.GetHandle("setContentView:"), IntPtr.Zero);
                //wnd.ContentView = null; // xammac throws...
            });
            _observers.Add(observer);

            observer = NSApplication.Notifications.ObserveMainWindowChanged(static (sender, args) =>
            {
                Console.WriteLine("Window changed: {0}", args.Notification.Object);
            });
            _observers.Add(observer);

            BeginInvokeOnMainThread(() =>
            {
                foreach (var controller in _controllers)
                {
                    controller.ShowWindow(this);
                }
            });
        }

        public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
        {
            _controllers = Array.Empty<WindowController>();
            _windows = Array.Empty<MyWindow>();

            Console.WriteLine("ShouldTerminate: {0}", sender);

            _ = Task.Run(() => WaitForGC());

            return false;

            static async Task WaitForGC()
            {
                // If this is run on the UI thread and no GCHandle in WindowController, hello crash.
                while (true)
                {
                    await Task.Delay(1000);

                    Console.WriteLine("[GC] Running GC...");

                    for (int i = 0; i < 10; ++i)
                        _ = new int[10_000_000];

                    //System.GC.Collect();
                    //System.GC.WaitForPendingFinalizers();
                }
            }
        }

    }
}

