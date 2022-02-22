using System;

namespace Protacon.RxMq.LegacyConsoleExample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            new LegacyMessageService();

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();
        }
    }
}
