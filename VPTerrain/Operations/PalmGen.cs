using Nexus;
using System;
using System.Collections.Generic;
using System.IO;
using VP;

namespace VPTerrain
{
    public class PalmGen : IOperation
    {
        const string tag      = "PalmGen";
        const string template = "create scale {0} {1} {0}, rotate -.1 0 0 loop time={2} wait=.3,specular 0";

        Random rand = new Random();

        static string[] models = new[]
        {
            "palmtree5", "palmtree2", "palmtree4"
        };

        int  originTileX;
        int  originTileZ;
        int  widthX;
        int  widthZ;
        int  totalCells;
        int  totalNodes;
        int  scannedNodes;
        int  totalTrees;
        int  builtTrees;
        bool scanning;
        bool building;

        StreamWriter   log;
        Instance       bot;
        List<VPObject> trees;

        public void Run(Instance bot)
        {
            originTileX = ConsoleEx.AskInt("What origin tile X to query from?");
            originTileZ = ConsoleEx.AskInt("What origin tile Z to query from?");
            widthX      = ConsoleEx.AskInt("What width in tiles to query from origin X?");
            widthZ      = ConsoleEx.AskInt("What height in tiles to query from origin Z?");

            totalNodes = (widthX * Consts.NodePerTile) * (widthZ * Consts.NodePerTile);
            totalCells = (32 * widthX) * (32 * widthZ);

            Console.WriteLine("### Will plant trees in a total of {0} cells", totalCells);

            if ( !ConsoleEx.AskBool("Is this correct?") )
                Run(bot);
            else
            {
                this.bot = bot;

                if ( !Directory.Exists(bot.World) )
                    Directory.CreateDirectory(bot.World);

                SetupLog();
                CommenceQuery();
                CommencePlanting();
            }
        }

        public void SetupLog()
        {
            var file = "{0}x{1} to {2}x{3} {4}.genundo".LFormat(
                originTileX, originTileZ,
                originTileX + widthX - 1,
                originTileZ + widthZ - 1,
                TDateTime.UnixTimestamp);
            var path = "{0}/{1}".LFormat(bot.World, file);

            log = new StreamWriter(path);
            Log.Info(tag, "Saving generation undo file to {0}", path);
        }

        public void CommenceQuery()
        {
            Log.Info(tag, "Commencing query of {0} nodes...", totalNodes);
            scanning     = true;
            scannedNodes = 0;
            trees        = new List<VPObject>();

            VPTerrain.Busy = true;

            bot.Terrain.GetNode               += onNodeGet;
            bot.Property.CallbackObjectCreate += onCreate;

            for (var tileX = originTileX; tileX < originTileX + widthX; tileX++)
            for (var tileZ = originTileZ; tileZ < originTileZ + widthZ; tileZ++)
            {
                var revisions = new int[4,4];

                for (var x = 0; x < 4; x++)
                for (var z = 0; z < 4; z++)
                    revisions[x,z] = -1;

                bot.Terrain.QueryTile(tileX, tileZ, revisions);
            }

            while (scanning)
                bot.Pump();

            VPTerrain.Busy = false;
        }

        public void CommencePlanting()
        {
            Log.Info(tag, "Commencing build of {0} trees...", trees.Count);
            totalTrees = trees.Count;
            building   = true;
            builtTrees = 0;

            foreach (var tree in trees)
                bot.Property.AddObject(tree);

            while (building)
                bot.Pump();
        }

        /// <summary>
        /// TODO: check this code for negative tiles
        /// </summary>
        void onNodeGet(Instance sender, TerrainNode node, int tileX, int tileZ)
        {
            Console.Title = "Planting trees for node {0}x{1} on tile {2}x{3}".LFormat(node.X, node.Z, tileX, tileZ);
            var nodeOffsetX = node.X * Consts.CellPerNode;
            var nodeOffsetZ = node.Z * Consts.CellPerNode;
            var tileOffsetX = tileX * Consts.CellPerTile;
            var tileOffsetZ = tileZ * Consts.CellPerTile;

            for (var x = 0; x < 8; x++)
            for (var z = 0; z < 8; z++)
            {
                var cell  = node[x, z];
                var cellX = x + nodeOffsetX + tileOffsetX;
                var cellZ = z + nodeOffsetZ + tileOffsetZ;

                // Random skip
                if ( rand.Next(0, 1000) < 75 )
                    continue;

                if (cell.Hole)
                    continue;

                if (cellX % 2 == 0 && cellZ % 2 == 1)
                    continue;

                if (cellZ % 2 == 0 && cellX % 2 == 1)
                    continue;

                //if (cell.Height < 5.5)
                if (cell.Height < 0.1 || cell.Height > 5.5)
                    continue;

                genCellTrees(cellX, cellZ, cell.Height);
            }

            scannedNodes++;
            if (scannedNodes >= totalNodes)
            {
                Log.Info(tag, "Query of region heights complete");
                scanning = false;
            }
        }

        void genCellTrees(int x, int z, float height)
        {
            //var n = rand.Next(1, 2);
            var n = 1;

            // Randomly skip cell
            if ( rand.Next(0, 100) >= 80 )
                return;

            for (var i = 0; i < n; i++)
            {
                var treeHeight = (float) ( height - .1 - (rand.NextDouble() / 6) );
                var treeX      = (float) ( x + rand.NextDouble() );
                var treeZ      = (float) ( z + rand.NextDouble() );
                var treePos    = new Vector3D(treeX, treeHeight, treeZ);
                var treeRot    = Quaternion.CreateFromYawPitchRoll((float) (rand.NextDouble() * Math.PI), 0f, 0f);
                var scale      = Math.Round( 0.8 + ( rand.NextDouble() / 2 ), 2);
                var time       = Math.Round( rand.NextDouble() * 5, 2);

                var tree = new VPObject(models[ rand.Next(0, models.Length - 1) ], treePos, treeRot)
                {
                    Action = template.LFormat(scale * 1.5, scale * 1.5, time),
                };

                if ( rand.Next(0, 100) >= 50 )
                    tree.Action += ", visible no radius=200";

                trees.Add(tree);
            }
        }

        void onCreate(Instance sender, ReasonCode result, VPObject obj)
        {
            builtTrees++;
            log.WriteLine(obj.Id);
            Console.Title = "Built tree ID {0}; {1} out of {2}".LFormat(obj.Id, builtTrees, totalTrees);

            if (builtTrees >= totalTrees)
            {
                Log.Info(tag, "All {0} trees built", totalTrees);
                building = false;
            }
        }

        public void Dispose()
        {
            trees.Clear();
            trees = null;

            bot.Terrain.GetNode               -= onNodeGet;
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
