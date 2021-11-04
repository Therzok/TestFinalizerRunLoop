using System;
using System.Runtime.CompilerServices;
using System.Threading;
using AppKit;
using ObjCRuntime;

namespace TestFinalizerRunloop
{
	public class FinalizableView : NSView
	{
        static readonly ConditionalWeakTable<FinalizableView, Writer> _defer = new();
        static int i = 0;
        int id;

        public FinalizableView()
        {
            id = Interlocked.Increment(ref i);
            _defer.Add(this, new Writer { Id = id, });
        }

        public override void ViewWillMoveToWindow(NSWindow newWindow)
        {
            Console.WriteLine("[{0}] ViewWillMoveToWindow {1} -> {2}", id, Window?.Title ?? "null", (newWindow?.Title ?? "null"));
            base.ViewWillMoveToWindow(newWindow);
        }
    }

    class Writer
    {
        public int Id;

        ~Writer()
        {
            Console.WriteLine("[{0}] Byeeee", Id);
        }
    }
}

