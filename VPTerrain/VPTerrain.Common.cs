using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VPTerrain
{
    public enum Operations
    {
        PalmGen,
        SaveHeightmap,
        LoadHeightmap,
        TreeGen,
        OceanGen,
        GenUndo,
        Exit
    }

    public static class Consts
    {
        // VP terrain structure:
        // 1 cell is 10x10 meters
        // Nodes contain 8x8 cells
        // Tiles contain 4x4 nodes

        public const int CellPerNode = 8;
        public const int NodePerTile = 4;
        public const int CellPerTile = CellPerNode * NodePerTile; // 32

        public const int HeightmapGroundLevel = 0;
    }
}
