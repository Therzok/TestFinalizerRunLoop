using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using CoreFoundation;
using Foundation;
using ObjCRuntime;

namespace TestFinalizerRunloop
{
    class MyView : NSView
    {
        public override void ViewDidMoveToWindow()
        {
            Console.WriteLine("ViewDidMoveToWindow {0}", Window?.ToString() ?? "null");

            base.ViewDidMoveToWindow();
        }

        ~MyView()
        {
            Console.WriteLine("Byeeee");
        }
    }
    public partial class ViewController : NSViewController
    {
        NSButton button;
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            button = new NSButton { Title = "Create Views" };
            View.AddSubview(button);
            button.Frame = new CoreGraphics.CGRect(0, 0, 200, 50);
            button.Activated += Button_Activated;

            //var raise = new Selector("raise");
            //new NSException("a", "a", null).PerformSelector(raise);
        }

        public override void ViewDidAppear()
        {
            Console.WriteLine("ViewDidAppear");

            base.ViewDidAppear();
        }
        public override void ViewDidDisappear()
        {
            button.Activated -= Button_Activated;
            button.Target = null;

            Console.WriteLine("ViewDidDisappear");

            base.ViewDidDisappear();
        }

        NSView MakePrintingSubviews()
        {
            var sv = new MyView();
            sv.AddSubview(new MyView());

            return sv;
        }

        private void Button_Activated(object sender, EventArgs e)
        {
            using var pool = new NSAutoreleasePool();

            var sv = MakePrintingSubviews();

            Console.WriteLine("Expecting 2 non-null");
            button.AddSubview(sv);

            Console.WriteLine("Expecting 2 null");
            button.RemoveFromSuperview();

            Console.WriteLine("Expecting nothing");
            sv.RemoveFromSuperview();

            Console.WriteLine("Expecting nothing");
            button.AddSubview(sv);

            Console.WriteLine("Expecting 2 non-null");
            View.AddSubview(button);

            Console.WriteLine("Expecting 2 null");
            sv.RemoveFromSuperview();

            Console.WriteLine("Expecting 2 non-null");
            button.AddSubview(sv);

            Console.WriteLine("Expecting 2 null");
            var window = View.Window;
            window.ReleasedWhenClosed = true;
            //window.Close();

            // This crashes.
            var controller = window.WindowController;
            controller.BeginInvokeOnMainThread(new Selector("close"), this);
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
