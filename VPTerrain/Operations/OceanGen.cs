using Nexus;
using System;
using System.IO;
using VP;

namespace VPTerrain
{
    public class OceanGen : IOperation
    {
        const string tag    = "OceanGen";
        const string action = @"create texture water1_top, color tint teal, opacity .5,
  normalmap nmap-water4,
  specular 10, move 60 0 60 loop sync reset time=60, ambient 1,
  solid no";

        int  originX;
        int  originZ;
        int  widthX;
        int  widthZ;
        int  scannedNodes;
        int  totalOcean;
        int  builtOcean;
        bool building;

        StreamWriter   log;
        Instance       bot;

        public void Run(Instance bot)
        {
            originX = ConsoleEx.AskInt("What origin coordinate X do you wish to cover from?");
            originZ = ConsoleEx.AskInt("What origin coordinate Z do you wish to cover from?");
            widthX  = ConsoleEx.AskInt("What width in coordinates to cover from origin X?");
            widthZ  = ConsoleEx.AskInt("What height in coordinates to cover from origin Z?");

            this.bot = bot;

            if ( !Directory.Exists(bot.World) )
                Directory.CreateDirectory(bot.World);

            SetupLog();
            CommenceBuilding();
        }

        public void SetupLog()
        {
            var file = "Ocean {0}.genundo".LFormat(TDateTime.UnixTimestamp);
            var path = "{0}/{1}".LFormat(bot.World, file);

            log = new StreamWriter(path);
            Log.Info(tag, "Logging ocean object IDs to {0}", path);
        }

        public void CommenceBuilding()
        {
            Log.Info(tag, "Commencing build of ocean...");
            building   = true;
            builtOcean = 0;
            totalOcean = 0;

            VPTerrain.Busy = true;

            bot.Property.CallbackObjectCreate += onCreate;

            for (var oceanX = originX; oceanX <= originX + widthX; oceanX += 6)
            for (var oceanZ = originZ; oceanZ <= originZ + widthZ; oceanZ += 6)
            {
                var ocean = new VPObject( "f6000,0,.1,.1.rwx", new Vector3D(oceanX, -0.05f, oceanZ) )
                {
                    Action   = action
                };

                bot.Property.AddObject(ocean);
                totalOcean++;
            }

            while (building)
                bot.Pump();

            VPTerrain.Busy = false;
        }

        void onCreate(Instance sender, ReasonCode result, VPObject obj)
        {
            builtOcean++;
            log.WriteLine(obj.Id);
            Console.Title = "Built ocean ID {0}; {1} out of {2}".LFormat(obj.Id, builtOcean, totalOcean);

            if (builtOcean >= totalOcean)
            {
                Log.Info(tag, "All {0} ocean built", totalOcean);
                building = false;
            }
        }

        public void Dispose()
        {
            bot.Property.CallbackObjectCreate -= onCreate;

            if (log != null)
            {
                log.Flush();
                log.Close();
                log.Dispose();
            }
        }
    }
}
