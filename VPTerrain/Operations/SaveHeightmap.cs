using System;
using System.Drawing;
using System.IO;
using VP;

namespace VPTerrain
{
    public class SaveHeightmap : IOperation
    {
        const string tag = "SaveHeightmap";

        int originTileX;
        int originTileZ;
        int widthX;
        int widthZ;

        int ratio;
        int totalNodes;
        int totalTiles;
        int imageX;
        int imageZ;

        int      scanned;
        bool     scanning;
        Instance bot;
        Bitmap   heightmap;

        public void Run(Instance bot)
        {
            originTileX = ConsoleEx.AskInt("What origin tile X to query from?");
            originTileZ = ConsoleEx.AskInt("What origin tile Z to query from?");
            widthX      = ConsoleEx.AskInt("What width in tiles to query from origin X?");
            widthZ      = ConsoleEx.AskInt("What height in tiles to query from origin Z?");
            ratio       = ConsoleEx.AskInt("What is the desired pixel-value-to-height ratio? (e.g. 50 : 1 cell high)");

            totalTiles = widthX * widthZ;
            totalNodes = (widthX * Consts.NodePerTile) * (widthZ * Consts.NodePerTile);
            imageX     = widthX * Consts.CellPerTile;
            imageZ     = widthZ * Consts.CellPerTile;

            Console.WriteLine("### Will scan a total of {0} tiles and {1} nodes, saving an image of {2}x{3}", totalTiles, totalNodes, imageX, imageZ);
            Console.WriteLine("### Image will be saved in the {0} directory", bot.World);

            if ( !ConsoleEx.AskBool("Is this correct?") )
                Run(bot);
            else
            {
                this.bot = bot;

                if ( !Directory.Exists(bot.World) )
                    Directory.CreateDirectory(bot.World);

                CommenceScan();
                SaveResult();
            }
        }

        public void CommenceScan()
        {
            Log.Info(tag, "Commencing scan and fetch...");
            heightmap = new Bitmap(imageX, imageZ);
            scanning  = true;
            scanned   = 0;

            bot.Terrain.GetNode += onNodeGet;

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
        }

        public void SaveResult()
        {
            var file = "{0}x{1} to {2}x{3}.bmp".LFormat(
                originTileX, originTileZ,
                originTileX + widthX - 1,
                originTileZ + widthZ - 1);
            var path = "{0}/{1}".LFormat(bot.World, file);

            Log.Info(tag, "Saving to {0}", path);
            heightmap.Save(path);
        }

        void onNodeGet(Instance sender, TerrainNode node, int tileX, int tileZ)
        {
            Console.Title = "Saving node {0}x{1} for tile {2}x{3}".LFormat(node.X, node.Z, tileX, tileZ);
            var nodeOffsetX = node.X * Consts.CellPerNode;
            var nodeOffsetZ = node.Z * Consts.CellPerNode;
            var tileOffsetX = (tileX - originTileX) * Consts.CellPerTile;
            var tileOffsetZ = (tileZ - originTileZ) * Consts.CellPerTile;

            for (var x = 0; x < 8; x++)
            for (var z = 0; z < 8; z++)
            {
                var pixelX = x + nodeOffsetX + tileOffsetX;
                var pixelZ = z + nodeOffsetZ + tileOffsetZ;

                var cell    = node[x, z];
                var texture = cell.Hole ? 0xFF : Math.Min(cell.Texture, (ushort) 254);
                var height  = (int) (Consts.HeightmapGroundLevel + cell.Height * ratio);
                    height  = height.Clamp(0, 255);

                heightmap.SetPixel(pixelX, pixelZ, System.Drawing.Color.FromArgb(height, texture, ratio) );
            }

            scanned++;
            if (scanned >= totalNodes)
                scanning = false;
        }

        public void Dispose()
        {
            bot.Terrain.GetNode -= onNodeGet;

            if (heightmap != null)
                heightmap.Dispose();
        }
    }
}
