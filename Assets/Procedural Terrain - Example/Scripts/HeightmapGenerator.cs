using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProcTerrain;

public class HeightmapGenerator : HeightmapProvider
{
	// Noise generator
	private FastNoise noiseGenerator = new FastNoise();

	private float terrainSize;

	public override void Init(int seed, float terrainSize)
    {
		this.terrainSize = terrainSize;
		noiseGenerator.SetSeed(seed);

		noiseGenerator.SetNoiseType(FastNoise.NoiseType.Simplex);
		noiseGenerator.SetFrequency(20.0f);
	}

	// Converts noise from range -1.0f - 1.0f to 0.0f - 1.0f
	private float GetNoiseNormalised(float x, float z)
	{
		return (noiseGenerator.GetNoise(x, z) + 1.0f) / 2.0f;
	}

    public override float GetHeight(float x, float z)
	{
		float nx = x / terrainSize;
		float nz = z / terrainSize;

		float height = 1.0f * (GetNoiseNormalised(nx, nz))
		 + 0.5f * GetNoiseNormalised(2.0f * nx, 2.0f * nz)
		 + 0.25f * GetNoiseNormalised(4.0f * nx, 4.0f * nz);

		return 100.0f * Mathf.Pow(height, 1.5f);
	}
}
