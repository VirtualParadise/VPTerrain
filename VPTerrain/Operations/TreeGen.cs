using Nexus;
using System;
using System.Collections.Generic;
using System.IO;
using VP;

namespace VPTerrain
{
    public class TreeGen : IOperation
    {
        const string tag      = "TreeGen";
        const string template = "create solid no, scale {0} {1} {0}, color tint {2}";
        //const string template = "create solid no, scale {0} .2 {0}, specular 0";

        Random rand = new Random();

        static string[] colorsMid = new[]
        {
            "aaffaa", "bbffbb", "aaffcc", "ffffaa",
            "aaffee", "bbeebb", "bbffcc", "aaddaa",
            "aaffdd", "bbddbb", "ccffcc", "eeffaa",
        };

        static string[] colorsLarge = new[]
        {
            "88dd88", "55ee99", "99dd88", "88dd77",
        };

        static string[] models = new[]
        {
            "tbtree003", "tbtree004", "tbtree005",
        };

        string model = "pinetree1b";

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

            VPTerrain.Busy = true;

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

            VPTerrain.Busy = true;

            foreach (var tree in trees)
                bot.Property.AddObject(tree);

            while (building)
                bot.Pump();

            VPTerrain.Busy = false;
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

            for (var i = 0; i < n; i++)
            {
                var treeHeight = (float) ( height - .1 - (rand.NextDouble() / 6) );
                var treeX      = (float) ( x + rand.NextDouble() );
                var treeZ      = (float) ( z + rand.NextDouble() );
                var treePos    = new Vector3D(treeX, treeHeight, treeZ);
                var treeRot    = Quaternion.CreateFromYawPitchRoll((float) (rand.NextDouble() * Math.PI), 0f, 0f);
                var scale      = Math.Round( 1.5 + ( rand.NextDouble() * 2 ), 4);
                var color      = scale > 3.5
                    ? colorsLarge[ rand.Next(0, colorsLarge.Length - 1) ]
                    : scale < 2
                        ? "ffffff"
                        : colorsMid[ rand.Next(0, colorsMid.Length - 1) ];

                var tree = new VPObject(model, treePos, treeRot)
                {
                    //Model    = models[ rand.Next(0, models.Length - 1) ],
                    Action   = template.LFormat(scale * 1.5, scale * 1.5, color),
                    //Action   = template.LFormat(scale),
                };

                if (scale < 2)
                    tree.Action += ", visible no radius=150";

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
