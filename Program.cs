using System;

namespace ConsoleApplication
{
    public class Program
    {
        public static int Main(string[] args)
        {

            if ( args.Length > 0 )
            {
                if ( args[0] == "--selftest") 
                {
                    return SelfTest.Run();
                }
                else 
                {
                    Console.WriteLine($"Unknown argument: {args[0]}");
                    return -1;
                }
            }

            Console.WriteLine("Hello World!");

            return 0;
        }
    }
}
