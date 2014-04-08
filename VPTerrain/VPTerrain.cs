using System;
using System.Threading.Tasks;
using System.Threading;
using VP;

namespace VPTerrain
{
    class VPTerrain
    {
        const  string tag = "VPTerrain";

        public static bool Busy = false;

        static Instance bot;

        static Task botLoop;
        static bool finishing = false;

        public static void Main(string[] args)
        {
            Log.QuickSetup();
            Log.Info(tag, "Starting up...");
            Console.Title = "VP terrain tool";

            ConnectBot();
            botLoop = Task.Factory.StartNew(BotLoop);
            
            while (!finishing)
            {
                var task = ConsoleEx.AskEnum<Operations>("What would you like to do?");

                // TODO: change this to reflection based system
                switch (task)
                {
                    case Operations.Exit:
                        Finish();
                        break;

                    case Operations.PalmGen:
                        DoOperation( new PalmGen() );
                        break;

                    case Operations.SaveHeightmap:
                        DoOperation( new SaveHeightmap() );
                        break;

                    case Operations.LoadHeightmap:
                        DoOperation( new LoadHeightmap() );
                        break;

                    case Operations.TreeGen:
                        DoOperation( new TreeGen() );
                        break;

                    case Operations.OceanGen:
                        DoOperation( new OceanGen() );
                        break;

                    case Operations.GenUndo:
                        DoOperation( new GenUndo() );
                        break;
                }

                Console.Title = "Operation complete - VPTerrain";
            }

            Log.Info(tag, "Shutting down...");
        }

        public static void DoOperation(IOperation current)
        {
            try
            {
                Log.Info("Operation", "Running operation {0}...", current);
                current.Run(bot);
            }
            catch (Exception e)
            {
                Log.Severe("Operation", "Exception: {0}", e);
            }
            finally
            {
                current.Dispose();
                current = null;
                Log.Info("Operation", "{0} finished executing", current);
            }
        }

        public static void ConnectBot()
        {
            if (bot != null)
                bot.Dispose();

            var username = ConsoleEx.Ask("What is your VP username?");
            var password = ConsoleEx.Ask("What is your VP password?");
            var world    = ConsoleEx.Ask("What is the target world?");

            try
            {
                Log.Info("Bot", "Creating and logging in new bot instance");

                bot = new Instance()
                    .Login(username, password, "VPTerrain")
                    .Enter(world)
                    .GoTo()
                    .Pump();
            }
            catch (Exception e)
            {
                Log.Severe("Bot", e.Message);
                Console.WriteLine("Could not log in bot; please check login detals");
                ConnectBot();
            }
        }

        public static void BotLoop()
        {
            while (!finishing)
                if (!Busy)
                    bot.Pump();
        }

        public static void Finish()
        {
            Log.Debug(tag, "Waiting for keep-alive thread to finish");
            finishing = true;
            botLoop.Wait();

            Log.Info(tag, "Finished");
            bot.Dispose();
        }
    }
}
