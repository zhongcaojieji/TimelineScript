using System;
using System.Threading;

namespace TimeLineScript
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Interpreter interpreter = new Interpreter();
            interpreter.Init();

            PlayScheduler scheduler = new PlayScheduler(interpreter);
            scheduler.ActivePlay("the_first_play");
            int last_tick = Environment.TickCount;
            while (true)
            {
                scheduler.Update((Environment.TickCount - last_tick) * 0.001f);
                last_tick = Environment.TickCount;
                Thread.Sleep(33);
            }
        }
    }
}
