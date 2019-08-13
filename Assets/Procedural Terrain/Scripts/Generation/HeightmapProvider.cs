using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProcTerrain;

namespace ProcTerrain
{

	public abstract class HeightmapProvider : MonoBehaviour
	{
		public abstract void Init(int seed, float terrainSize);
		public abstract float GetHeight(float x, float z);
	}

}
