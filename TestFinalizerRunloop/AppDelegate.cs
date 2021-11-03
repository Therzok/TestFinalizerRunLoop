using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using CoreFoundation;
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
            NSWindowController controller;

            controller = NSStoryboard.MainStoryboard.InstantiateControllerWithIdentifier("ReleaseWhenClosed") as NSWindowController;
            controller.ShowWindow(this);

            controller = NSStoryboard.MainStoryboard.InstantiateControllerWithIdentifier("NotReleaseWhenClosed") as NSWindowController;
            controller.ShowWindow(this);

            NSNotificationCenter.DefaultCenter.AddObserver(NSWindow.WillCloseNotification, notif =>
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
        }

        public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
        {
            Console.WriteLine("ShouldTerminate: {0}", sender);

            _ = Task.Run(() => WaitForGC());

            return false;

            static async Task WaitForGC()
            {
                // If this is run on the UI thread, hello crash.
                while (true)
                {
                    await Task.Delay(1000);

                    Console.WriteLine("[GC] Running GC...");

                    _ = new int[10_000_000];

                    System.GC.Collect();

                    // WaitForPendingFinalizers crashes!
                    //System.GC.WaitForPendingFinalizers();
                }
            }
        }

        void StressTestGC()
        {
            Console.WriteLine("Creating views");
            Task.Run(() =>
            {

                for (int n = 0; n < 10000; n++)
                {
                    new FinalizerAppKit
                    {
                        Child = new FinalizerAppKit
                        {
                            Child = new FinalizerAppKit(),
                        },
                    };
                }
            });
            Task.Run(() =>
            {
                for (int n = 0; n < 10000; n++)
                {
                    new FinalizerDispatch
                    {
                        Child = new FinalizerDispatch
                        {
                            Child = new FinalizerDispatch(),
                        },
                    };
                }
            });
            Console.WriteLine("Done");
        }

        public class FinalizerAppKit : NSObject
        {
            static int finalizedCount = 0;
            static int disposedCount = 0;
            static readonly NSObject target = new NSObject();

            public FinalizerAppKit Child;

            bool disposed;
            ~FinalizerAppKit()
            {
                if (Interlocked.Increment(ref finalizedCount) % 1000 == 0)
                    Console.WriteLine("AppKit: {0} {1}", FinalizerAppKit.finalizedCount, FinalizerAppKit.disposedCount);

                target.BeginInvokeOnMainThread(() => Dispose());
            }

            void Dispose()
            {
                disposed = true;
                if (Interlocked.Increment(ref disposedCount) % 1000 == 0)
                    Console.WriteLine("AppKit: {0} {1}", FinalizerAppKit.finalizedCount, FinalizerAppKit.disposedCount);
            }
        }

        public class FinalizerDispatch : NSObject
        {
            static int finalizedCount = 0;
            static int disposedCount = 0;
            static readonly DispatchQueue queue = DispatchQueue.MainQueue;

            public FinalizerDispatch Child;

            bool disposed;

            ~FinalizerDispatch()
            {
                if (Interlocked.Increment(ref finalizedCount) % 1000 == 0)
                    Console.WriteLine("Dispatch: {0} {1}", FinalizerDispatch.finalizedCount, FinalizerDispatch.disposedCount);

                queue.DispatchAsync(() => Dispose());
            }

            void Dispose()
            {
                disposed = true;
                if (Interlocked.Increment(ref disposedCount) % 1000 == 0)
                    Console.WriteLine("Dispatch: {0} {1}", FinalizerDispatch.finalizedCount, FinalizerDispatch.disposedCount);
            }
        }
    }
}

