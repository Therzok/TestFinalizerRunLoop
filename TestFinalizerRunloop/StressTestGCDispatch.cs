using System;
using System.Threading;
using System.Threading.Tasks;
using CoreFoundation;
using Foundation;

namespace TestFinalizerRunloop
{
	public class StressTestGCDispatch
	{

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

            ~FinalizerAppKit()
            {
                if (Interlocked.Increment(ref finalizedCount) % 1000 == 0)
                    Console.WriteLine("AppKit: {0} {1}", FinalizerAppKit.finalizedCount, FinalizerAppKit.disposedCount);

                target.BeginInvokeOnMainThread(() => _Dispose());
            }

            void _Dispose()
            {
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

            ~FinalizerDispatch()
            {
                if (Interlocked.Increment(ref finalizedCount) % 1000 == 0)
                    Console.WriteLine("Dispatch: {0} {1}", FinalizerDispatch.finalizedCount, FinalizerDispatch.disposedCount);

                queue.DispatchAsync(() => _Dispose());
            }

            void _Dispose()
            {
                if (Interlocked.Increment(ref disposedCount) % 1000 == 0)
                    Console.WriteLine("Dispatch: {0} {1}", FinalizerDispatch.finalizedCount, FinalizerDispatch.disposedCount);
            }
        }
    }
}

