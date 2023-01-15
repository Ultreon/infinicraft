using UnityEngine;

namespace Infinicraft.BlockLayers
{
    public class StoneLayerHandler : BlockLayerHandler
    {
        protected override bool TryHandling(ChunkData chunkData, int x, int y, int z, int surfaceHeightNoise, Vector2Int mapSeedOffset)
        {
            if (y < surfaceHeightNoise - 3)
            {
                Vector3Int pos = new Vector3Int(x, y, z);
                Chunk.SetBlock(chunkData, pos, BlockType.Stone);
                return true;
            }
            return false;
        }
    }
}
