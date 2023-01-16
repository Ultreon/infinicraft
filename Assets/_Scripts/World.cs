using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Infinicraft
{
    public class World : MonoBehaviour
    {
        public int mapSizeInChunks = 6;
        public int chunkSize = 16, chunkHeight = 100;
        public int chunkDrawingRange = 8;

        public GameObject chunkPrefab;
        public WorldRenderer worldRenderer;

        public TerrainGenerator terrainGenerator;
        public Vector2Int mapSeedOffset;

        CancellationTokenSource taskTokenSource = new();

        //public Dictionary<Vector3Int, ChunkData> chunkDataDictionary = new Dictionary<Vector3Int, ChunkData>();
        //public Dictionary<Vector3Int, ChunkRenderer> chunkDictionary = new Dictionary<Vector3Int, ChunkRenderer>();

        public UnityEvent OnWorldCreated, OnNewChunksGenerated;

        public WorldData worldData { get; private set; }
        public bool IsWorldCreated { get; private set; }

        private void Awake()
        {
            worldData = new WorldData
            {
                chunkHeight = this.chunkHeight,
                chunkSize = this.chunkSize,
                chunkDataDictionary = new Dictionary<Vector3Int, ChunkData>(),
                chunkDictionary = new Dictionary<Vector3Int, ChunkRenderer>()
            };
        }

        private void OnDisable()
        {
            taskTokenSource.Cancel();
        }

        public async void GenerateWorld()
        {
            await GenerateWorld(Vector3Int.zero);
        }

        private async Task GenerateWorld(Vector3Int position)
        {
            terrainGenerator.GenerateBiomePoints(position, chunkDrawingRange, chunkSize, mapSeedOffset);

            WorldGenerationData worldGenerationData = await Task.Run(() => GetPositionsThatPlayerSees(position), taskTokenSource.Token);

            foreach (Vector3Int pos in worldGenerationData.chunkPositionsToRemove)
            {
                WorldDataHelper.RemoveChunk(this, pos);
            }

            foreach (Vector3Int pos in worldGenerationData.chunkDataToRemove)
            {
                WorldDataHelper.RemoveChunkData(this, pos);
            }

            ConcurrentDictionary<Vector3Int, ChunkData> dataDict = null;

            try
            {
                dataDict = await CalculateWorldChunkData(worldGenerationData.chunkDataPositionsToCreate);
            }
            catch (Exception e)
            {
                Debug.Log("World Chunk Data calculation has cancelled!");
                Debug.LogWarning(e);
                return;
            }


            foreach (var calcData in dataDict)
            {
                worldData.chunkDataDictionary.Add(calcData.Key, calcData.Value);
            }

            foreach (var chunkData in worldData.chunkDataDictionary.Values)
            {
                AddTreeLeaves(chunkData);
            }

            ConcurrentDictionary<Vector3Int, MeshData> meshDataDict = new();
            List<ChunkData> dataToRender = worldData.chunkDataDictionary
                .Where(it => worldGenerationData.chunkPositionsToCreate.Contains(it.Key))
                .Select(it => it.Value)
                .ToList();

            try
            {
                meshDataDict = await CreateMeshDataAsync(dataToRender);
            }
            catch (Exception e)
            {
                Debug.Log("Mesh Data creation has cancelled!");
                Debug.LogWarning(e);
                return;
            }

            StartCoroutine(ChunkCreationCoroutine(meshDataDict));
        }

        private void AddTreeLeaves(ChunkData chunkData)
        {
            foreach (var treeLeaves in chunkData.treeData.treeLeavesSolid)
            {
                Chunk.SetBlock(chunkData, treeLeaves, BlockType.TreeLeavesSolid);
            }
        }

        private Task<ConcurrentDictionary<Vector3Int, MeshData>> CreateMeshDataAsync(List<ChunkData> dataToRender)
        {
            ConcurrentDictionary<Vector3Int, MeshData> dict = new();
            return Task.Run(() =>
            {
                foreach (var data in dataToRender)
                {
                    if (taskTokenSource.Token.IsCancellationRequested)
                        taskTokenSource.Token.ThrowIfCancellationRequested();

                    MeshData meshData = Chunk.GetChunkMeshData(data);
                    dict.TryAdd(data.worldPosition, meshData);
                }
                return dict;
            }, taskTokenSource.Token);
        }

        private Task<ConcurrentDictionary<Vector3Int, ChunkData>> CalculateWorldChunkData(List<Vector3Int> chunkDataPositionsToCreate)
        {
            ConcurrentDictionary<Vector3Int, ChunkData> dict = new();

            return Task.Run(() =>
            {
                foreach (var pos in chunkDataPositionsToCreate)
                {
                    if (taskTokenSource.Token.IsCancellationRequested)
                        taskTokenSource.Token.ThrowIfCancellationRequested();

                    ChunkData data = new ChunkData(chunkSize, chunkHeight, this, pos);
                    ChunkData newData = terrainGenerator.GenerateChunkData(data, mapSeedOffset);

                    dict.TryAdd(pos, newData);
                }
                return dict;
            }, taskTokenSource.Token);
        }

        IEnumerator ChunkCreationCoroutine(ConcurrentDictionary<Vector3Int, MeshData> meshDataDict)
        {
            foreach (var item in meshDataDict)
            {
                CreateChunk(worldData, item.Key, item.Value);
                yield return new WaitForEndOfFrame();
            }
            //Debug.Log("Done creating chunks");
            if (IsWorldCreated == false)
            {
                IsWorldCreated = true;
                OnWorldCreated?.Invoke();
            }
        }

        private void CreateChunk(WorldData worldData, Vector3Int position, MeshData meshData)
        {
            ChunkRenderer chunkRenderer = worldRenderer.RenderChunk(worldData, position, meshData);
            worldData.chunkDictionary.Add(position, chunkRenderer);
        }

        internal bool SetBlock(RaycastHit hit, BlockType blockType)
        {
            ChunkRenderer chunk = hit.collider.GetComponent<ChunkRenderer>();
            if (chunk == null)
                return false;

            Vector3Int pos = GetBlockPos(hit);

            if (pos.y <= 0)
                return false;

            WorldDataHelper.SetBlock(chunk.ChunkData.worldReference, pos, blockType);
            chunk.ModifiedByThePlayer = true;

            if (Chunk.IsOnEdge(chunk.ChunkData, pos))
            {
                List<ChunkData> neighbourDataList = Chunk.GetEdgeNeighbourChunk(chunk.ChunkData, pos);
                foreach (ChunkData neighbourData in neighbourDataList)
                {
                    //neighbourData.modifiedByThePlayer = true;
                    ChunkRenderer chunkToUpdate = WorldDataHelper.GetChunk(neighbourData.worldReference, neighbourData.worldPosition);
                    if (chunkToUpdate != null)
                        chunkToUpdate.UpdateChunk();
                }

            }

            chunk.UpdateChunk();
            return true;
        }

        private Vector3Int GetBlockPos(RaycastHit hit)
        {
            Vector3 pos = new Vector3(
                 GetBlockPositionIn(hit.point.x, hit.normal.x),
                 GetBlockPositionIn(hit.point.y, hit.normal.y),
                 GetBlockPositionIn(hit.point.z, hit.normal.z)
                 );

            return Vector3Int.RoundToInt(pos);
        }

        private float GetBlockPositionIn(float pos, float normal)
        {
            if (Mathf.Abs(pos % 1) == 0.5f)
            {
                pos -= (normal / 2);
            }


            return (float)pos;
        }

        private WorldGenerationData GetPositionsThatPlayerSees(Vector3Int playerPosition)
        {
            List<Vector3Int> allChunkPositionsNeeded = WorldDataHelper.GetChunkPositionsAroundPlayer(this, playerPosition);
            List<Vector3Int> allChunkDataPositionsNeeded = WorldDataHelper.GetDataPositionsAroundPlayer(this, playerPosition);

            List<Vector3Int> chunkPositionsToCreate = WorldDataHelper.SelectPositonsToCreate(worldData, allChunkPositionsNeeded, playerPosition);
            List<Vector3Int> chunkDataPositionsToCreate = WorldDataHelper.SelectDataPositonsToCreate(worldData, allChunkDataPositionsNeeded, playerPosition);

            List<Vector3Int> chunkPositionsToRemove = WorldDataHelper.GetUnnededChunks(worldData, allChunkPositionsNeeded);
            List<Vector3Int> chunkDataToRemove = WorldDataHelper.GetUnnededData(worldData, allChunkDataPositionsNeeded);

            WorldGenerationData data = new WorldGenerationData
            {
                chunkPositionsToCreate = chunkPositionsToCreate,
                chunkDataPositionsToCreate = chunkDataPositionsToCreate,
                chunkPositionsToRemove = chunkPositionsToRemove,
                chunkDataToRemove = chunkDataToRemove,
                chunkPositionsToUpdate = new List<Vector3Int>()
            };
            return data;

        }

        internal async void LoadAdditionalChunksRequest(GameObject player)
        {
            Debug.Log("Load more chunks");
            await GenerateWorld(Vector3Int.RoundToInt(player.transform.position));
            OnNewChunksGenerated?.Invoke();
        }

        internal BlockType GetBlockFromChunkCoordinates(ChunkData chunkData, int x, int y, int z)
        {
            Vector3Int pos = Chunk.ChunkPositionFromBlockCoords(this, x, y, z);
            ChunkData containerChunk = null;

            worldData.chunkDataDictionary.TryGetValue(pos, out containerChunk);

            if (containerChunk == null)
                return BlockType.Nothing;
            Vector3Int blockInCHunkCoordinates = Chunk.GetBlockInChunkCoordinates(containerChunk, new Vector3Int(x, y, z));
            return Chunk.GetBlockFromChunkCoordinates(containerChunk, blockInCHunkCoordinates);
        }

        public struct WorldGenerationData
        {
            public List<Vector3Int> chunkPositionsToCreate;
            public List<Vector3Int> chunkDataPositionsToCreate;
            public List<Vector3Int> chunkPositionsToRemove;
            public List<Vector3Int> chunkDataToRemove;
            public List<Vector3Int> chunkPositionsToUpdate;
        }
    }

    public struct WorldData
    {
        public Dictionary<Vector3Int, ChunkData> chunkDataDictionary;
        public Dictionary<Vector3Int, ChunkRenderer> chunkDictionary;
        public int chunkSize;
        public int chunkHeight;
    }
}

