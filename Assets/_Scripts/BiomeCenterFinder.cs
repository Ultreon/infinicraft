using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Infinicraft
{
    public static class BiomeCenterFinder
    {
        public static List<Vector2Int> neightbours8Direction = new List<Vector2Int>()
        {
            new Vector2Int( 0, 1),
            new Vector2Int( 1, 1),
            new Vector2Int( 1, 0),
            new Vector2Int( 1,-1),
            new Vector2Int( 0,-1),
            new Vector2Int(-1,-1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, 1)
        };

        public static List<Vector3Int> CalculatedBiomeCenters(Vector3 playerPosition, int drawRange, int chunkSize)
        {
            int biomeLength = drawRange * chunkSize;

            Vector3Int origin = new Vector3Int(Mathf.RoundToInt(playerPosition.x / biomeLength) * biomeLength, 0, Mathf.RoundToInt(playerPosition.y / biomeLength));
            HashSet<Vector3Int> biomeCentersTemp = new HashSet<Vector3Int>();
            
            biomeCentersTemp.Add(origin);

            foreach (Vector3Int offsetXZ in neightbours8Direction)
            {
                Vector3Int newBiomePoint_1 = new Vector3Int(origin.x + offsetXZ.x * biomeLength, 0, origin.z + offsetXZ.y * biomeLength);
                Vector3Int newBiomePoint_2 = new Vector3Int(origin.x + offsetXZ.x * biomeLength, 0, origin.z + offsetXZ.y * 2 * biomeLength);
                Vector3Int newBiomePoint_3 = new Vector3Int(origin.x + offsetXZ.x * 2 * biomeLength, 0, origin.z + offsetXZ.y * biomeLength);
                Vector3Int newBiomePoint_4 = new Vector3Int(origin.x + offsetXZ.x * 2 * biomeLength, 0, origin.z + offsetXZ.y * 2 * biomeLength);

                biomeCentersTemp.Add(newBiomePoint_1);
                biomeCentersTemp.Add(newBiomePoint_2);
                biomeCentersTemp.Add(newBiomePoint_3);
                biomeCentersTemp.Add(newBiomePoint_4);
            }

            return new List<Vector3Int>(biomeCentersTemp);
        }
    }
}
