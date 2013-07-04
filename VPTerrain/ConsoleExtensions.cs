using System;

namespace VPTerrain
{
    public static class ConsoleEx
    {
        public static string Ask(string query)
        {
            Console.WriteLine("*** {0}", query);
            Console.Write("> ");
            return Console.ReadLine().Trim();
        }

        public static int AskInt(string query)
        {
            Console.WriteLine("*** {0}", query);
            Console.Write("> ");
            var value = 0;

            if ( !int.TryParse(Console.ReadLine().Trim(), out value) )
            {
                Console.WriteLine("!!! That is not a valid integer");
                return AskInt(query);
            }
            else
                return value;
        }
    }
}
