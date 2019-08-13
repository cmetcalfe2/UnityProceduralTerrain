using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProcTerrain;

namespace ProcTerrain
{

	public struct Segment
	{
		Vector2 startPos;
		Vector2 endPos;

		public Segment(Vector2 startPos, Vector2 endPos)
		{
			this.startPos = startPos;
			this.endPos = endPos;
		}
	}

	public class MapGenerator
	{
		// Terrain info
		private int seed;
		private float terrainSize;
		private int numTiles;
		private float tileSize;
		private int tileResolution;

		// Heightmap provider
		private HeightmapProvider heightmapProvider;

		public MapGenerator(int seed, float terrainSize, int numTiles, int tileResolution, HeightmapProvider heightmapProvider)
		{
			this.seed = seed;

			this.terrainSize = terrainSize;
			this.numTiles = numTiles;
			this.tileResolution = tileResolution;
			this.tileSize = terrainSize / (float)numTiles;

			this.heightmapProvider = heightmapProvider;
		}

		public Tile LoadTile(Vector2Int location)
		{
			Tile tile = new Tile(location, tileResolution);

			GenerateTileHeightmap(tile);

			return tile;
		}

		private Segment[] GenerateIslandBoundaries(int numIterations)
		{
			int numSegments = 4 * numIterations * 2;

			Segment[] segments = new Segment[numSegments];

			// Initialise segment list with box edges
			Vector2[] corners =
			{
			new Vector2(terrainSize * 0.2f, terrainSize * 0.8f), // Top left corner
			new Vector2(terrainSize * 0.8f, terrainSize * 0.8f), // Top right corner
			new Vector2(terrainSize * 0.2f, terrainSize * 0.2f), // Bottom left corner
			new Vector2(terrainSize * 0.8f, terrainSize * 0.2f), // Bottom right corner
		};

			segments[0] = new Segment(corners[0], corners[1]);
			segments[1] = new Segment(corners[1], corners[3]);
			segments[2] = new Segment(corners[3], corners[2]);
			segments[3] = new Segment(corners[2], corners[0]);

			return segments;
		}

		private void GenerateTileHeightmap(Tile tile)
		{
			float positionStep = tileSize / tileResolution;

			float xPos = -(positionStep * tileResolution) + (tile.location.x * tileSize);
			float zPos = -(positionStep * tileResolution) + (tile.location.y * tileSize);

			for (int x = -1; x <= tileResolution + 1; x++)
			{
				zPos = -(positionStep * tileResolution) + (tile.location.y * tileSize);

				for (int z = -1; z <= tileResolution + 1; z++)
				{
					tile.heightmap[x + 1, z + 1] = heightmapProvider.GetHeight(xPos, zPos);

					zPos += positionStep;
				}

				xPos += positionStep;
			}
		}
	}

}
