using UnityEngine;

namespace Infinicraft.BlockLayers
{
    public class SoilLayerHandler : BlockLayerHandler
    {
        public BlockType soilBlockType;

        protected override bool TryHandling(ChunkData chunkData, int x, int y, int z, int surfaceHeightNoise, Vector2Int mapSeedOffset)
        {
            //if (y >= surfaceHeightNoise - (chunkData.worldReference.earthThickness - 1) && y < surfaceHeightNoise)
            //{
            //    Vector3Int pos = new Vector3Int(x, y, z);
            //    Chunk.SetBlock(chunkData, pos, soilBlockType);
            //    return true;
            //}
            //return false;
            return false;
        }
    }
}
