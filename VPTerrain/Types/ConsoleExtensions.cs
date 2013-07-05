using System;

namespace VPTerrain
{
    public static class ConsoleEx
    {
        public static string Ask(string query)
        {
            TConsole.WriteLineColored(ConsoleColor.Cyan, "\n*** {0}", query);
            Console.Write("> ");
            return Console.ReadLine().Trim();
        }

        public static int AskInt(string query)
        {
            TConsole.WriteLineColored(ConsoleColor.Cyan, "\n*** {0}", query);
            Console.Write("(integers only) > ");
            var value = 0;

            if ( !int.TryParse(Console.ReadLine().Trim(), out value) )
            {
                TConsole.WriteLineColored(ConsoleColor.Red, "!!! That is not a valid integer");
                return AskInt(query);
            }
            else
                return value;
        }

        public static float AskFloat(string query)
        {
            TConsole.WriteLineColored(ConsoleColor.Cyan, "\n*** {0}", query);
            Console.Write("(floats only) > ");
            var value = 0f;

            if ( !float.TryParse(Console.ReadLine().Trim(), out value) )
            {
                TConsole.WriteLineColored(ConsoleColor.Red, "!!! That is not a valid float");
                return AskFloat(query);
            }
            else
                return value;
        }

        public static bool AskBool(string query)
        {
            TConsole.WriteLineColored(ConsoleColor.Cyan, "\n*** {0}", query);
            Console.Write("(true/false) > ");
            bool value;

            if ( !bool.TryParse(Console.ReadLine().Trim(), out value) )
            {
                TConsole.WriteLineColored(ConsoleColor.Red, "!!! That is not a valid boolean");
                return AskBool(query);
            }
            else
                return value;
        }

        public static T AskEnum<T>(string query)
            where T : struct
        {
            var type = typeof(T);

            if ( !type.IsEnum )
                throw new ArgumentException("Given type is not an enum");
            
            T   value;
            var options = Enum.GetNames(type);
            TConsole.WriteLineColored(ConsoleColor.Cyan, "\n*** {0}", query);
            Console.WriteLine("*** Valid options are: {0}", string.Join(", ", options) );

            if ( !Enum.TryParse<T>(Console.ReadLine().Trim(), true, out value) )
            {
                TConsole.WriteLineColored(ConsoleColor.Red, "!!! That is not a valid option");
                return AskEnum<T>(query);
            }
            else
                return value;
        }
    }
}
