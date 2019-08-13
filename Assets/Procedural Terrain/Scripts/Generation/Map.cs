using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProcTerrain;

namespace ProcTerrain
{

	public class Map
	{
		public int seed;
		public Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>(); // Stores loaded tiles

		// Map generator
		private MapGenerator mapGenerator;

		public Map(int seed, float terrainSize, int numTiles, int tileResolution, HeightmapProvider heightmapProvider)
		{
			mapGenerator = new MapGenerator(seed, terrainSize, numTiles, tileResolution, heightmapProvider);

			this.seed = seed;
		}

		public void LoadTile(Vector2Int location)
		{
			tiles[location] = mapGenerator.LoadTile(location);
		}

		public void UnloadTile(Vector2Int location)
		{
			tiles.Remove(location);
		}

	}

}
