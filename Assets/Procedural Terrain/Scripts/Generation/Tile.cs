using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProcTerrain;

namespace ProcTerrain
{

	public class Tile
	{
		public Vector2Int location;
		public float[,] heightmap;

		public Tile(Vector2Int location, int tileResolution)
		{
			this.location = location;
			this.heightmap = new float[tileResolution + 3, tileResolution + 3];
		}
	}

}
