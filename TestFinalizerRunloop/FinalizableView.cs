using System;
using System.Threading;
using AppKit;

namespace TestFinalizerRunloop
{
	public class FinalizableView : NSView
	{
        static int i = 0;

        int id = Interlocked.Increment(ref i);

        public override void ViewWillMoveToWindow(NSWindow newWindow)
        {
            Console.WriteLine("[{0}] ViewWillMoveToWindow {1} -> {2}", id, Window?.Title ?? "null", newWindow?.Title ?? "null");
            base.ViewWillMoveToWindow(newWindow);
        }

        ~FinalizableView()
        {
            Console.WriteLine("[{0}] Byeeee", id);
        }
    }
}

