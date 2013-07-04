using System;
using System.Threading;
using System.Linq;
using VP;
using System.Drawing;

namespace VPTerrain
{

    class VPTerrain
    {
        const  string    tag = "VPTerrain";
        static VPTerrain instance;

        Instance bot;
        string   file;
        int      tileX;
        int      tileZ;
        int      scanned = 0;
        bool     scanning = false;
        bool     loading  = false;

        Bitmap heightmap;

        public static void Main(string[] args)
        {
            Log.QuickSetup();
            Log.Info(tag, "Starting up...");

            instance = new VPTerrain();
            instance.Run();

            Log.Info(tag, "Shutting down...");
        }

        void Run()
        {
            ConnectBot();
            AskParameters();
            CommenceScan();

            Finish();
        }

        public void ConnectBot()
        {
            if (bot != null)
                bot.Dispose();

            var username = ConsoleEx.Ask("What is your VP username?");
            var password = ConsoleEx.Ask("What is your VP password?");
            var world    = ConsoleEx.Ask("What is the target world to scan?");

            try
            {
                Log.Info("Bot", "Creating and logging in new bot instance");

                bot = new Instance("VPTerrain");
                bot.Login(username, password);
                bot.Enter(world);
                bot.GoTo();
                bot.Wait(0);
            }
            catch (Exception e)
            {
                Log.Severe("Bot", e.Message);
                Console.WriteLine("Could not log in bot; please check login detals");
                ConnectBot();
            }
        }

        public void AskParameters()
        {
            file  = ConsoleEx.Ask("What file do you wish to load? (leave empty for scan only)");
            tileX = ConsoleEx.AskInt("What tile X to query/load to?");
            tileZ = ConsoleEx.AskInt("What tile Z to query/load to?");

            if ( !string.IsNullOrWhiteSpace(file) )
                loading = true;            
        }

        public void CommenceScan()
        {
            Log.Info(tag, "Commencing scan and fetch...");

            var revisions = new int[4,4];

            for (var x = 0; x < 4; x++)
            for (var z = 0; z < 4; z++)
                revisions[x,z] = -1;

            if (loading)
                heightmap = new Bitmap(file);
            else
                heightmap = new Bitmap(32, 32);

            scanning             = true;
            bot.Terrain.GetNode += onNodeGet;
            bot.Terrain.QueryTile(tileX, tileZ, revisions);

            while (scanning)
                bot.Wait(100);
        }

        public void Finish()
        {
            // Do any final bits
            for (var i = 0; i < 10; i++)
                bot.Wait(100);

            if (!loading)
                heightmap.Save( "{0}x{1}.bmp".LFormat(tileX, tileZ) );

            Log.Info(tag, "Finished");
            bot.Dispose();
            heightmap.Dispose();
            Console.ReadLine();
        }

        void onNodeGet(Instance sender, TerrainNode node, int tileX, int tileZ)
        {
            var offsetX = node.X * 8;
            var offsetZ = node.Z * 8;

            if (loading)
            {
                for (var x = 0; x < 8; x++)
                for (var z = 0; z < 8; z++)
                {
                    var pixel  = heightmap.GetPixel(x + offsetX, z + offsetZ);
                    var height = (float) pixel.R; 
                    var cell   = new TerrainCell
                    {
                        Height = (height - 127) / 100,
                        Hole   = pixel.G == 0xFF
                    };

                    // TODO: Investigate why swapping z and x is nessecary...
                    node[z,x] = cell;
                }

                Console.WriteLine("Wrote node {0}x{1} for tile {2}x{3} ::", node.X, node.Z, tileX, tileZ);
                bot.Terrain.SetNode(node, tileX, tileZ);
            }
            else
            {
                for (var x = 0; x < 8; x++)
                for (var z = 0; z < 8; z++)
                {
                    var cell    = node[x, z];
                    int height  = (int) (127 + cell.Height * 100);
                    int hole    = cell.Hole ? 0xFF : 0;
                    var color   = height.Clamp(0, 255);

                    if (cell.Height != 0)
                        Log.Fine("Cell", "Height of {0}x{1}: {2}", x, z, cell.Height);

                    heightmap.SetPixel(x + offsetX, z + offsetZ, System.Drawing.Color.FromArgb(color, hole, 0) );
                }
            }

            scanned++;
            if (scanned >= 16)
                scanning = false;
        }
    }
}
