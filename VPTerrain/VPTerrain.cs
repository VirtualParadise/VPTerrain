using System;
using System.Threading.Tasks;
using System.Threading;
using VP;

namespace VPTerrain
{
    class VPTerrain
    {
        const  string    tag = "VPTerrain";
        static VPTerrain instance;

        Instance bot;
        Task     keepAliveTask;
        bool     keepAlive = true;
        bool     finishing = false;

        public static void Main(string[] args)
        {
            Log.QuickSetup();
            Log.Info(tag, "Starting up...");
            Console.Title = "VP terrain tool";

            instance = new VPTerrain();
            instance.Run();

            Log.Info(tag, "Shutting down...");
        }

        public void Run()
        {
            ConnectBot();
            keepAliveTask = Task.Factory.StartNew(KeepAlive);
            
            while (true)
            {
                var task = ConsoleEx.AskEnum<Operations>("What would you like to do?");
                keepAlive = false;

                // TODO: change this to reflection based system
                switch (task)
                {
                    case Operations.Exit:
                        Finish();
                        return;

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

                keepAlive = true;
                Console.Title = "Operation complete - VPTerrain";
            }
        }

        public void DoOperation(IOperation current)
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

        public void ConnectBot()
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

        public void KeepAlive()
        {
            while (!finishing)
                if (keepAlive && bot != null)
                    bot.Pump(1000);
                else
                    Thread.Sleep(1000);
        }

        public void Finish()
        {
            Log.Debug(tag, "Waiting for keep-alive thread to finish");
            finishing = true;
            keepAliveTask.Wait();

            Log.Info(tag, "Finished");
            bot.Dispose();
        }
    }
}
