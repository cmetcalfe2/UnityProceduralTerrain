using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using System.Threading;
using ProcTerrain;

namespace ProcTerrain
{

	public enum MeshUpdateThreadStatus
	{
		Idle,
		Updating,
		Finished
	};

	public class ProceduralTerrain : MonoBehaviour
	{
		public Transform player;

		public int mapSeed;

		public float size;
		public int numTiles;
		public int tileResolution;

		public int[] lodDistances;

		public Material terrainMaterial;

		public HeightmapProvider heightmapProvider;

		public string terrainLayer;

		//public Crest.OceanDepthCache oceanDepthCache; // Crest ocean depth buffer support

		// Generated map
		private Map map;

		// Tile information
		private float tileSize;
		private TerrainTileMesh[,] tileMeshes;
		private int[,] tileLODs;
		private int[,] tileEdgeTransitionFlags;

		// Mesh rendering
		private List<GameObject>[] lodGameObjects;
		private List<CombinedTerrainTileMesh>[] lodMeshes;
		private List<MeshRenderer>[] meshRenderers;
		private List<MeshFilter>[] meshFilters;

		// Player position
		private Vector2Int curPlayerTilePos;
		private Vector2Int prevPlayerTilePos;

		// Mesh updater thread variables
		private Thread meshUpdaterThread;
		private int meshUpdateThreadStatus;
		private int meshUpdaterThreadStopped = 0;

		private Dictionary<Vector2Int, bool> tileRequiresUpdate = new Dictionary<Vector2Int, bool>();

		private Dictionary<TileEdgeTransitionFlags, Vector2Int> tileTransitionCheckDirections = new Dictionary<TileEdgeTransitionFlags, Vector2Int>
		{
			{TileEdgeTransitionFlags.Top, new Vector2Int(0, 1) },
			{TileEdgeTransitionFlags.Bottom, new Vector2Int(0, -1) },
			{TileEdgeTransitionFlags.Left, new Vector2Int(-1, 0) },
			{TileEdgeTransitionFlags.Right, new Vector2Int(1, 0) },
		};

		// Start is called before the first frame update
		void Start()
		{
			// Initialise variables
			tileSize = size / numTiles;
			prevPlayerTilePos = Vector2Int.one * int.MaxValue;
			map = new Map(mapSeed, size, numTiles, tileResolution, heightmapProvider);

			// Create mesh renderers and filters
			int numLODs = lodDistances.Length;
			lodGameObjects = new List<GameObject>[numLODs];
			tileMeshes = new TerrainTileMesh[numTiles, numTiles];
			tileLODs = new int[numTiles, numTiles];
			tileEdgeTransitionFlags = new int[numTiles, numTiles];
			lodMeshes = new List<CombinedTerrainTileMesh>[numLODs];
			meshRenderers = new List<MeshRenderer>[numLODs];
			meshFilters = new List<MeshFilter>[numLODs];

			// Create LOD meshes
			for (int i = 0; i < numLODs; i++)
			{
				lodGameObjects[i] = new List<GameObject>(8);
				lodMeshes[i] = new List<CombinedTerrainTileMesh>(8);
				meshRenderers[i] = new List<MeshRenderer>(8);
				meshFilters[i] = new List<MeshFilter>(8);

				for (int m = 0; m < 8; m++)
				{
					lodGameObjects[i].Add(new GameObject());
					lodGameObjects[i][m].transform.parent = transform;
					lodGameObjects[i][m].name = "LOD " + i + " mesh " + m;
					lodGameObjects[i][m].layer = LayerMask.NameToLayer(terrainLayer);

					CombinedMeshType meshType = CombinedMeshType.Corner;

					if (i == 0)
						meshType = CombinedMeshType.Center;
					else if (m > 3)
						meshType = CombinedMeshType.Edge;

					int prevLODDistance = (i == 0) ? 0 : lodDistances[i - 1];

					lodMeshes[i].Add(new CombinedTerrainTileMesh(meshType, tileResolution / MathUtilities.Pow(2, i), lodDistances[i], Mathf.Max(0, lodDistances[i] - prevLODDistance)));

					meshRenderers[i].Add(lodGameObjects[i][m].AddComponent(typeof(MeshRenderer)) as MeshRenderer);
					meshRenderers[i][m].sharedMaterial = terrainMaterial;

					meshFilters[i].Add(lodGameObjects[i][m].AddComponent(typeof(MeshFilter)) as MeshFilter);
					meshFilters[i][m].mesh = lodMeshes[i][m].mesh;
				}
			}

			// Create tile objects
			for (int x = 0; x < numTiles; x++)
			{
				for (int z = 0; z < numTiles; z++)
				{
					tileLODs[x, z] = -1;
					tileMeshes[x, z] = new TerrainTileMesh(tileResolution);
				}
			}

			// Initialise heightmap provider
			heightmapProvider.Init(mapSeed, size);

			// Start mesh updater thread
			ThreadStart MeshUpdaterThreadStart = MeshUpdateThread;
			meshUpdaterThread = new Thread(MeshUpdaterThreadStart);
			meshUpdateThreadStatus = (int)MeshUpdateThreadStatus.Idle;
			meshUpdaterThread.Start();
		}

		private void OnDisable()
		{
			// End mesh updater thread
			Interlocked.Exchange(ref meshUpdaterThreadStopped, 1);
		}

		// Update is called once per frame
		void Update()
		{
			MeshUpdateThreadStatus updaterStatus = (MeshUpdateThreadStatus)meshUpdateThreadStatus;

			if (player)
			{
				if (updaterStatus == MeshUpdateThreadStatus.Idle)
				{
					curPlayerTilePos = GetPlayerTilePos();

					if (curPlayerTilePos != prevPlayerTilePos)
					{
						// Set updater thread status to updating
						Debug.Log("Updating tiles");
						Debug.Log(curPlayerTilePos);
						Interlocked.Exchange(ref meshUpdateThreadStatus, (int)MeshUpdateThreadStatus.Updating);

						prevPlayerTilePos = curPlayerTilePos;
					}
				}
				else if (updaterStatus == MeshUpdateThreadStatus.Finished)
				{
					Debug.Log("Updating tiles finished");

					// Apploy LOD meshes
					lodMeshes[0][0].ApplyToMesh();
					meshFilters[0][0].sharedMesh = lodMeshes[0][0].mesh;
					for (int i = 1; i < lodDistances.Length; i++)
					{
						for (int j = 0; j < 8; j++)
						{
							lodMeshes[i][j].ApplyToMesh();
							meshFilters[i][j].sharedMesh = lodMeshes[i][j].mesh;
							/* Crest ocean depth buffer support
							if (oceanDepthCache != null)
							{
								oceanDepthCache.PopulateCache();
							}*/
						}
					}

					// Reset updater thread to idle status
					Interlocked.Exchange(ref meshUpdateThreadStatus, (int)MeshUpdateThreadStatus.Idle);
				}
			}
		}

		private Vector2Int GetPlayerTilePos()
		{
			Vector3 localPos = transform.InverseTransformPoint(player.position);

			return new Vector2Int(Mathf.FloorToInt(localPos.x / tileSize), Mathf.FloorToInt(localPos.z / tileSize));
		}

		private void MeshUpdateThread()
		{
			Profiler.BeginThreadProfiling("Custom Threads", "Mesh Updater Thread");

			while (meshUpdaterThreadStopped == 0)
			{
				MeshUpdateThreadStatus status = (MeshUpdateThreadStatus)meshUpdateThreadStatus;
				if (status == MeshUpdateThreadStatus.Updating)
				{
					// Main thread has requested mesh update
					// Determine which tiles need updating
					Debug.Log("Determining tiles to update");
					GetTilesRequiringUpdate();

					// Update tile edge flags
					Debug.Log("Updating tile edge transition flags");
					UpdateTileEdgeFlags(tileRequiresUpdate);

					// Update tile meshes
					Debug.Log("Regenerating tile meshes");
					foreach (var tileRequiringUpdate in tileRequiresUpdate)
					{
						if (tileRequiringUpdate.Value == true)
						{
							int x = tileRequiringUpdate.Key.x;
							int z = tileRequiringUpdate.Key.y;
							Vector2Int tileLocation = new Vector2Int(x, z);

							// Don't generate meshes for unloaded tiles
							if (map.tiles.ContainsKey(tileLocation))
							{
								TerrainMeshGenerator.GenerateTile(tileMeshes[x, z], tileRequiringUpdate.Key, tileSize, tileResolution, tileLODs[x, z], map.tiles[tileLocation].heightmap, tileEdgeTransitionFlags[x, z]);
							}
						}
					}

					// Combine tile meshes
					CombineTileMeshes();

					// Set update thread status to finished
					Interlocked.Exchange(ref meshUpdateThreadStatus, (int)MeshUpdateThreadStatus.Finished);
				}
				else
				{
					Thread.Sleep(100);
				}
			}
		}

		private void GetTilesRequiringUpdate()
		{
			// Reset tiles requiring updates
			for (int x = 0; x < numTiles; x++)
			{
				for (int z = 0; z < numTiles; z++)
				{
					tileRequiresUpdate[new Vector2Int(x, z)] = false;
				}
			}

			for (int x = 0; x < numTiles; x++)
			{
				for (int z = 0; z < numTiles; z++)
				{
					// Determine miminmum distance of tile
					Vector2Int tilePos = new Vector2Int(x, z);
					Vector2Int distance = tilePos - curPlayerTilePos;
					int maxDistance = 0;

					if (Mathf.Abs(distance.x) > Mathf.Abs(distance.y))
					{
						maxDistance = Mathf.Abs(distance.x);
					}
					else
					{
						maxDistance = Mathf.Abs(distance.y);
					}

					// Determine new LOD of tile
					int newLOD = -1;
					for (int lod = 0; lod < lodDistances.Length; lod++)
					{
						if (maxDistance <= lodDistances[lod])
						{
							newLOD = lod;
							break;
						}
					}

					// If LOD has changed, add to update list
					if (newLOD != tileLODs[x, z])
					{
						// Add tile and neighbours to update list (need seams updated)
						Vector2Int[] tilesToUpdate =
						{
							new Vector2Int(x - 1, z - 1),
							new Vector2Int(x - 1, z),
							new Vector2Int(x - 1, z + 1),
							new Vector2Int(x, z - 1),
							new Vector2Int(x, z),
							new Vector2Int(x, z + 1),
							new Vector2Int(x + 1, z - 1),
							new Vector2Int(x + 1, z),
							new Vector2Int(x + 1, z + 1)
						};

						for (int i = 0; i < tilesToUpdate.Length; i++)
						{
							if (tilesToUpdate[i].x >= 0 &&
								tilesToUpdate[i].y >= 0 &&
								tilesToUpdate[i].x < numTiles &&
								tilesToUpdate[i].y < numTiles)
							{
								tileRequiresUpdate[tilesToUpdate[i]] = true;
							}
						}

						// Load tile if newly in view range
						// Or unload if moved out of view range
						if (newLOD != -1 && tileLODs[x, z] == -1)
						{
							map.LoadTile(new Vector2Int(x, z));
						}
						else if (newLOD == -1 && tileLODs[x, z] != -1)
						{
							map.UnloadTile(new Vector2Int(x, z));
						}

						// Save new LOD
						tileLODs[x, z] = newLOD;
					}
				}
			};
		}

		private void UpdateTileEdgeFlags(Dictionary<Vector2Int, bool> tilesRequiringUpdate)
		{
			Vector2Int tileLocation;
			int tileLOD;
			int tileEdgeFlags;

			Vector2Int neighbourTileLocation;

			foreach (var tile in tilesRequiringUpdate)
			{
				// Get current tile location and LOD
				tileLocation = tile.Key;
				tileLOD = tileLODs[tileLocation.x, tileLocation.y];

				// Reset edge flags
				tileEdgeFlags = 0;

				// Loop through edge transition flags and check if they should be applied
				foreach (var neighbourDirection in tileTransitionCheckDirections)
				{
					neighbourTileLocation = tileLocation + neighbourDirection.Value;

					if (neighbourTileLocation.x < 0 || neighbourTileLocation.x >= numTiles || neighbourTileLocation.y < 0 || neighbourTileLocation.y >= numTiles)
					{
						continue;
					}

					// Neighbour LOD is lower than current tile, so transition is needed
					if (tileLODs[neighbourTileLocation.x, neighbourTileLocation.y] > tileLOD)
					{
						tileEdgeFlags = tileEdgeFlags | (int)neighbourDirection.Key;
					}
				}

				// Save edge transition flags
				tileEdgeTransitionFlags[tileLocation.x, tileLocation.y] = tileEdgeFlags;
			}
		}

		private void CombineTileMeshes()
		{
			// Combine all LOD 0 tiles into one mesh
			List<TerrainTileMesh> meshesToCombine = new List<TerrainTileMesh>();
			for (int x = curPlayerTilePos.x - lodDistances[0]; x <= curPlayerTilePos.x + lodDistances[0]; x++)
			{
				if (x < 0 || x >= numTiles)
					continue;

				for (int z = curPlayerTilePos.y - lodDistances[0]; z <= curPlayerTilePos.y + lodDistances[0]; z++)
				{
					if (z < 0 || z >= numTiles)
						continue;

					meshesToCombine.Add(tileMeshes[x, z]);
				}
			}

			lodMeshes[0][0].CombineTerrainTileMeshes(meshesToCombine);

			KeyValuePair<Vector2Int, Vector2Int>[] combineRanges = new KeyValuePair<Vector2Int, Vector2Int>[8];

			// Combine other LODs
			for (int lod = 1; lod < lodDistances.Length; lod++)
			{
				Vector2Int lodTopLeftCorner = new Vector2Int(curPlayerTilePos.x - lodDistances[lod], curPlayerTilePos.y + lodDistances[lod]);
				Vector2Int lodTopRightCorner = new Vector2Int(curPlayerTilePos.x + lodDistances[lod], curPlayerTilePos.y + lodDistances[lod]);
				Vector2Int lodBottomLeftCorner = new Vector2Int(curPlayerTilePos.x - lodDistances[lod], curPlayerTilePos.y - lodDistances[lod]);
				Vector2Int lodBottomRightCorner = new Vector2Int(curPlayerTilePos.x + lodDistances[lod], curPlayerTilePos.y - lodDistances[lod]);

				int lodSize = lodDistances[lod] - lodDistances[lod - 1];

				combineRanges[0] = new KeyValuePair<Vector2Int, Vector2Int>(
					new Vector2Int(lodTopLeftCorner.x, lodTopLeftCorner.y - (lodSize - 1)),
					new Vector2Int(lodTopLeftCorner.x + lodSize, lodTopLeftCorner.y + 1)); // Top left corner

				combineRanges[1] = new KeyValuePair<Vector2Int, Vector2Int>(
					new Vector2Int(lodTopRightCorner.x - (lodSize - 1), lodTopRightCorner.y - (lodSize - 1)),
					new Vector2Int(lodTopRightCorner.x + 1, lodTopRightCorner.y + 1)); // Top right corner

				combineRanges[2] = new KeyValuePair<Vector2Int, Vector2Int>(
					new Vector2Int(lodBottomLeftCorner.x, lodBottomLeftCorner.y),
					new Vector2Int(lodBottomLeftCorner.x + lodSize, lodBottomLeftCorner.y + lodSize)); // Bottom left corner

				combineRanges[3] = new KeyValuePair<Vector2Int, Vector2Int>(
					new Vector2Int(lodBottomRightCorner.x - (lodSize - 1), lodBottomRightCorner.y),
					new Vector2Int(lodBottomRightCorner.x + 1, lodBottomRightCorner.y + lodSize)); // Bottom right corner

				combineRanges[4] = new KeyValuePair<Vector2Int, Vector2Int>(
					new Vector2Int(lodTopLeftCorner.x + lodSize, lodTopLeftCorner.y - (lodSize - 1)),
					new Vector2Int(lodTopRightCorner.x - (lodSize - 1), lodTopLeftCorner.y + 1)); // Top edge

				combineRanges[5] = new KeyValuePair<Vector2Int, Vector2Int>(
					new Vector2Int(lodBottomLeftCorner.x + lodSize, lodBottomLeftCorner.y),
					new Vector2Int(lodBottomRightCorner.x - (lodSize - 1), lodBottomLeftCorner.y + lodSize)); // Bottom edge

				combineRanges[6] = new KeyValuePair<Vector2Int, Vector2Int>(
					new Vector2Int(lodBottomLeftCorner.x, lodBottomLeftCorner.y + lodSize),
					new Vector2Int(lodBottomLeftCorner.x + lodSize, lodTopLeftCorner.y - (lodSize - 1))); // Left edge

				combineRanges[7] = new KeyValuePair<Vector2Int, Vector2Int>(
					new Vector2Int(lodBottomRightCorner.x - (lodSize - 1), lodBottomRightCorner.y + lodSize),
					new Vector2Int(lodBottomRightCorner.x + 1, lodTopRightCorner.y - (lodSize - 1))); // Right edge
			
				for (int i = 0; i < 8; i++)
				{
					meshesToCombine.Clear();

					int minX = Mathf.Max(0, Mathf.Min(numTiles - 1, combineRanges[i].Key.x));
					int minZ = Mathf.Max(0, Mathf.Min(numTiles - 1, combineRanges[i].Key.y));
					int maxX = Mathf.Max(0, Mathf.Min(numTiles - 1, combineRanges[i].Value.x));
					int maxZ = Mathf.Max(0, Mathf.Min(numTiles - 1, combineRanges[i].Value.y));

					for (int x = minX; x < maxX; x++)
					{
						if (x < 0 || x >= numTiles)
							continue;

						for (int z = minZ; z < maxZ; z++)
						{
							if (z < 0 || z >= numTiles)
								continue;

							meshesToCombine.Add(tileMeshes[x, z]);
						}
					}

					lodMeshes[lod][i].CombineTerrainTileMeshes(meshesToCombine);
				}
			}

		}
	}

}
