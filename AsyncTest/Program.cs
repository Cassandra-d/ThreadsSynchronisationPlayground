using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncTest
{
    public class Container
    {
        private LinkedList<int> _container;
        private object _sync = new object();
        private SpinLock _slimSync = new SpinLock();
        private ReaderWriterLock _rwl = new ReaderWriterLock();
        private ReaderWriterLockSlim _rwls = new ReaderWriterLockSlim();

        public Container()
        {
            _container = new LinkedList<int>();
        }

        public void Place(int num)
        {
            bool lockTaken = false;
            _slimSync.Enter(ref lockTaken);
            if (!lockTaken)
                Console.WriteLine("Lock isn't taken");
            //_rwl.AcquireWriterLock(TimeSpan.FromSeconds(2));
            //_rwls.EnterWriteLock();
            //lock(_sync)
            _container.AddFirst(num);
            //_rwls.ExitWriteLock();
            //_rwl.ReleaseWriterLock();
            _slimSync.Exit(useMemoryBarrier: true);
        }

        public void Pop()
        {
            bool lockTaken = false;
            _slimSync.Enter(ref lockTaken);
            if (!lockTaken)
                Console.WriteLine("Lock isn't taken");
            //_rwl.AcquireWriterLock(TimeSpan.FromSeconds(2));
            //_rwls.EnterWriteLock();
            //lock(_sync)
            _container.RemoveLast();
            //_rwls.ExitWriteLock();
            //_rwl.ReleaseWriterLock();
            _slimSync.Exit(useMemoryBarrier: true);
        }

        public long Count()
        {
            return _container.Count;
        }
    }

    class Program
    {
        internal static int _a;
        internal static int _b;

        static void Main(string[] args)
        {
            //TestConcurrentAccessToCollection();
            //TestConcurrentSimultaneousSeveralVariablesAccess();

            Console.WriteLine("Done");
            Console.ReadKey();
        }

        private static void TestConcurrentSimultaneousSeveralVariablesAccess()
        {
            var lc = new SpinLock();

            Task.Run(() =>
            {
                while (true)
                {
                    bool taken = false;
                    lc.Enter(ref taken);
                    Volatile.Write(ref _b, 6);
                    Volatile.Write(ref _a, 5);
                    Volatile.Write(ref _a, Volatile.Read(ref _b));
                    Volatile.Write(ref _b, 5);
                    lc.Exit();
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    bool taken = false;
                    lc.Enter(ref taken);
                    if (Volatile.Read(ref _a) == 5 && Volatile.Read(ref _b) == 5)
                        Console.WriteLine("Got it!");
                    lc.Exit();
                }
            });
        }

        private static void TestConcurrentAccessToCollection()
        {
            var c = new Container();

            Stopwatch sw = new Stopwatch();

            sw.Start();
            Task.WaitAll(CreateWorkload(7, c));
            sw.Stop();


            Console.WriteLine(c.Count());
            Console.WriteLine($"Done in {sw.Elapsed.TotalSeconds} sec");
        }

        private static Task[] CreateWorkload(int threadsCount, Container c)
        {
            var retVal = new List<Task>(threadsCount);

            for (int i = 0; i < threadsCount; i++)
            {
                var t = Task.Factory.StartNew(() => SomeIntensiveAction(c), TaskCreationOptions.LongRunning);
                retVal.Add(t);
            }

            return retVal.ToArray();
        }

        private static void SomeIntensiveAction(Container c)
        {
            const int ops = 10000000;

            Console.WriteLine("Thread started");

            for (int i = 0; i < ops; i++)
            {
                c.Place(i);
            }
            for (int i = 0; i < ops; i++)
            {
                c.Pop();
            }
            for (int i = 0; i < ops; i++)
            {
                c.Place(i);
            }
            for (int i = 0; i < ops; i++)
            {
                c.Pop();
            }

            Console.WriteLine("Thread done");
        }
    }
}
