using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using VP;

namespace VPTerrain
{
    public class LoadHeightmap : IOperation
    {
        const string tag       = "LoadHeightmap";
        const string fileRegex = "(-?[0-9]+)x(-?[0-9]+) to (-?[0-9]+)x(-?[0-9]+)";

        int originTileX;
        int originTileZ;
        int lastTileX;
        int lastTileZ;
        int tilesX;
        int tilesZ;
        
        int totalNodes;
        int totalTiles;

        int      sent;
        bool     sending;
        string   path;
        Instance bot;
        Bitmap   heightmap;

        public void Run(Instance bot)
        {
            while (true)
            {
                path = ConsoleEx.Ask("What heightmap do you wish to load?").Trim('"');

                if ( !File.Exists(path) )
                {
                    Log.Warn(tag, "No such file: {0}", path);
                    continue;
                }

                if ( !resolveData(path) )
                {
                    Log.Warn(tag, "Could not determine heightmap area from file name");
                    continue;
                }

                break;
            }

            // TODO: verify heightmap data
            heightmap  = new Bitmap(path);
            tilesX     = lastTileX - originTileX + 1;
            tilesZ     = lastTileZ - originTileZ + 1;
            totalTiles = tilesX * tilesZ;
            totalNodes = (tilesX * Consts.NodePerTile) * (tilesZ * Consts.NodePerTile);

            Console.WriteLine("### Will write a total of {0} tiles and {1} nodes, region of {2}x{3}", totalTiles, totalNodes, tilesX, tilesZ);

            if ( !ConsoleEx.AskBool("Is this correct?") )
            {
                heightmap.Dispose();
                Run(bot);
            }
            else
            {
                this.bot = bot;
                CommenceWrite();
            }
        }

        public void CommenceWrite()
        {
            Log.Info(tag, "Commencing write...");
            VPTerrain.Busy = true;

            bot.Terrain.CallbackNodeSet += onNodeSet;

            for (var tileX = originTileX; tileX <= lastTileX; tileX++)
            for (var tileZ = originTileZ; tileZ <= lastTileZ; tileZ++)
            {
                var tileOffsetX = (tileX - originTileX) * Consts.CellPerTile;
                var tileOffsetZ = (tileZ - originTileZ) * Consts.CellPerTile;

                for (var nodeX = 0; nodeX < 4; nodeX++)
                for (var nodeZ = 0; nodeZ < 4; nodeZ++)
                {
                    var nodeOffsetX = nodeX * Consts.CellPerNode;
                    var nodeOffsetZ = nodeZ * Consts.CellPerNode;
                    var node        = new TerrainNode()
                    {
                        X = nodeX,
                        Z = nodeZ
                    };

                    for (var cellX = 0; cellX < 8; cellX++)
                    for (var cellZ = 0; cellZ < 8; cellZ++)
                    {
                        var pixelX  = cellX + nodeOffsetX + tileOffsetX;
                        var pixelZ  = cellZ + nodeOffsetZ + tileOffsetZ;
                        var pixel   = heightmap.GetPixel(pixelX, pixelZ);
                        var height  = (float) pixel.R;
                        var ratio   = Math.Max( (int) pixel.B, 1 );
                        var hole    = pixel.G == 0xFF;
                        var texture = hole ? 0 : pixel.G; 
                        var cell    = new TerrainCell
                        {
                            Height  = (height - Consts.HeightmapGroundLevel) / ratio,
                            Hole    = hole,
                            Texture = (ushort) texture
                        };

                        node[cellX, cellZ] = cell;
                    }

                    Console.Title = "Writing node {0}x{1} for tile {2}x{3}".LFormat(nodeX, nodeZ, tileX, tileZ);
                    bot.Terrain.SetNode(node, tileX, tileZ);
                }
            }

            Log.Info(tag, "Writing to network...");
            sending = true;

            while (sending)
                bot.Pump();

            VPTerrain.Busy = false;
        }

        void onNodeSet(Instance sender, ReasonCode result, TerrainNode node, int tileX, int tileZ)
        {
            Console.Title = "Sent node {0}x{1} for tile {2}x{3}".LFormat(node.X, node.Z, tileX, tileZ);

            sent++;
            if (sent >= totalNodes)
                sending = false;
        }

        public void Dispose()
        {
            bot.Terrain.CallbackNodeSet -= onNodeSet;

            if (heightmap != null)
                heightmap.Dispose();
        }

        bool resolveData(string path)
        {
            var      file = Path.GetFileNameWithoutExtension(path);
            string[] matches;

            if ( !TRegex.TryMatch(file, fileRegex, out matches) )
                return false;

            if ( !int.TryParse(matches[1], out originTileX) )
                return false;

            if ( !int.TryParse(matches[2], out originTileZ) )
                return false;

            if ( !int.TryParse(matches[3], out lastTileX) )
                return false;

            if ( !int.TryParse(matches[4], out lastTileZ) )
                return false;

            return true;
        }
    }
}
