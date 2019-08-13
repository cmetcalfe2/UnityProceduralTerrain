using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProcTerrain;

namespace ProcTerrain
{

	public enum TileEdgeTransitionFlags
	{
		None = 0x0,
		Top = 0x1,
		Bottom = 0x2,
		Left = 0x4,
		Right = 0x8
	}

	public class TerrainMeshGenerator
	{
		public static void GenerateTile(TerrainTileMesh tileMesh, Vector2Int tilePosition, float tileSize, int lod0Resolution, int lod, float[,] heightMap, int edgeTransitionFlags)
		{
			tileMesh.Clear();

			if (lod == -1)
			{
				return;
			}

			int heightmapStep = MathUtilities.Pow(2, lod);

			int planeResolution = lod0Resolution / heightmapStep;

			float positionStep = tileSize / planeResolution;
			float uvStep = 1.0f / planeResolution;

			Vector3 tileOrigin = new Vector3(tilePosition.x * tileSize, 0.0f, tilePosition.y * tileSize);

			Vector3 vertex = new Vector3();
			Vector3 normal = new Vector3(0.0f, 1.0f, 0.0f);
			Vector2 uv = Vector3.zero;

			float leftHeight, rightHeight, topHeight, bottomHeight;

			// Create vertices
			for (int z = 0, heightmapZ = 1; z <= planeResolution; z++, heightmapZ += heightmapStep)
			{
				for (int x = 0, heightmapX = 1; x <= planeResolution; x++, heightmapX += heightmapStep)
				{
					vertex = tileOrigin;

					vertex.x += x * positionStep;
					vertex.y += heightMap[heightmapX, heightmapZ];
					vertex.z += z * positionStep;

					/*leftHeight = heightMap[heightmapX - heightmapStep, heightmapZ];
					rightHeight = heightMap[heightmapX + heightmapStep, heightmapZ];
					topHeight = heightMap[heightmapX, heightmapZ + heightmapStep];
					bottomHeight = heightMap[heightmapX, heightmapZ - heightmapStep];*/

					leftHeight = heightMap[heightmapX - 1, heightmapZ];
					rightHeight = heightMap[heightmapX + 1, heightmapZ];
					topHeight = heightMap[heightmapX, heightmapZ + 1];
					bottomHeight = heightMap[heightmapX, heightmapZ - 1];

					Vector3 tangent = new Vector3(2.0f, rightHeight - leftHeight, 0.0f);
					Vector3 bitangent = new Vector3(0.0f, bottomHeight - topHeight, 2.0f);
					normal = Vector3.Cross(tangent, bitangent).normalized;

					//normal = (new Vector3(2.0f * (rightHeight - leftHeight), 2.0f * (topHeight - bottomHeight), -4.0f).normalized);

					uv.x = x * uvStep;
					uv.y = z * uvStep;

					tileMesh.vertices.Add(vertex);
					tileMesh.normals.Add(normal);
					tileMesh.uvs.Add(uv);

				}
			}

			// Update seams
			if ((edgeTransitionFlags & (int)TileEdgeTransitionFlags.Top) == (int)TileEdgeTransitionFlags.Top)
			{
				for (int x = 1, vert = ((planeResolution + 1) * planeResolution) + 1; x < planeResolution; x += 2, vert += 2)
				{
					tileMesh.vertices[vert] = new Vector3(tileMesh.vertices[vert].x, Mathf.Lerp(tileMesh.vertices[vert - 1].y, tileMesh.vertices[vert + 1].y, 0.5f), tileMesh.vertices[vert].z);
				}
			}

			if ((edgeTransitionFlags & (int)TileEdgeTransitionFlags.Bottom) == (int)TileEdgeTransitionFlags.Bottom)
			{
				for (int x = 1, vert = 1; x < planeResolution; x += 2, vert += 2)
				{
					tileMesh.vertices[vert] = new Vector3(tileMesh.vertices[vert].x, Mathf.Lerp(tileMesh.vertices[vert - 1].y, tileMesh.vertices[vert + 1].y, 0.5f), tileMesh.vertices[vert].z);
				}
			}

			if ((edgeTransitionFlags & (int)TileEdgeTransitionFlags.Left) == (int)TileEdgeTransitionFlags.Left)
			{
				for (int z = 1, vert = planeResolution + 1; z < planeResolution; z += 2, vert += (planeResolution + 1) * 2)
				{
					tileMesh.vertices[vert] = new Vector3(tileMesh.vertices[vert].x, Mathf.Lerp(tileMesh.vertices[vert - (planeResolution + 1)].y, tileMesh.vertices[vert + (planeResolution + 1)].y, 0.5f), tileMesh.vertices[vert].z);
				}
			}

			if ((edgeTransitionFlags & (int)TileEdgeTransitionFlags.Right) == (int)TileEdgeTransitionFlags.Right)
			{
				for (int z = 1, vert = (planeResolution * 2) + 1; z < planeResolution; z += 2, vert += (planeResolution + 1) * 2)
				{
					tileMesh.vertices[vert] = new Vector3(tileMesh.vertices[vert].x, Mathf.Lerp(tileMesh.vertices[vert - (planeResolution + 1)].y, tileMesh.vertices[vert + (planeResolution + 1)].y, 0.5f), tileMesh.vertices[vert].z);
				}
			}

			// Calculate indices
			for (int ti = 0, vi = 0, y = 0; y < planeResolution; y++, vi++)
			{
				for (int x = 0; x < planeResolution; x++, ti += 6, vi++)
				{
					tileMesh.triangles.Add(vi);
					tileMesh.triangles.Add(vi + planeResolution + 1);
					tileMesh.triangles.Add(vi + 1);
					tileMesh.triangles.Add(vi + 1);
					tileMesh.triangles.Add(vi + planeResolution + 1);
					tileMesh.triangles.Add(vi + planeResolution + 2);
				}
			}
		}

		private static Mesh CopyMesh(Mesh input)
		{
			Mesh output = new Mesh();

			output.vertices = input.vertices;
			output.normals = input.normals;
			output.uv = input.uv;
			output.triangles = input.triangles;

			return output;
		}
	}

}
