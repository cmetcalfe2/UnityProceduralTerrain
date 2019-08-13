using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProcTerrain;

namespace ProcTerrain
{

	public class TerrainTileMesh
	{
		public List<Vector3> vertices;
		public List<Vector3> normals;
		public List<Vector2> uvs;
		public List<int> triangles;

		public TerrainTileMesh(int basePlaneResolution)
		{
			int numVertices = (basePlaneResolution + 1) * (basePlaneResolution + 1);

			vertices = new List<Vector3>(numVertices);
			normals = new List<Vector3>(numVertices);
			uvs = new List<Vector2>(numVertices);
			triangles = new List<int>(basePlaneResolution * basePlaneResolution * 6);
		}

		public void Clear()
		{
			vertices.Clear();
			normals.Clear();
			uvs.Clear();
			triangles.Clear();
		}
	}

	public enum CombinedMeshType
	{
		Center,
		Edge,
		Corner
	}

	public class CombinedTerrainTileMesh
	{
		public Mesh mesh;

		public CombinedMeshType type;

		public List<Vector3> vertices;
		public List<Vector3> normals;
		public List<Vector2> uvs;
		public List<int> triangles;

		public CombinedTerrainTileMesh(CombinedMeshType combinationType, int lodResolution, int lodDistance, int lodSize)
		{
			type = combinationType;
			mesh = new Mesh();
			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

			int numTiles = 0;

			if (type == CombinedMeshType.Corner)
			{
				numTiles = lodSize * lodSize;
			}
			else if (type == CombinedMeshType.Edge)
			{
				numTiles = (((lodDistance - lodSize) * 2) + 1) * lodSize;
			}
			else
			{
				numTiles = ((lodSize * 2) + 1) * ((lodSize * 2) + 1);
			}

			int numVertices = ((lodResolution + 1) * (lodResolution + 1)) * numTiles;
			int numTriangles = lodResolution * lodResolution * 2 * numTiles;

			vertices = new List<Vector3>(numVertices);
			normals = new List<Vector3>(numVertices);
			uvs = new List<Vector2>(numVertices);
			triangles = new List<int>(numTriangles);

		}

		public void CombineTerrainTileMeshes(List<TerrainTileMesh> tiles)
		{
			vertices.Clear();
			normals.Clear();
			uvs.Clear();
			triangles.Clear();

			int triangleOffset = 0;
			for (int i = 0; i < tiles.Count; i++)
			{
				TerrainTileMesh tile = tiles[i];
				int tileVertexCount = tile.vertices.Count;
				int tileTriangleCount = tile.triangles.Count;

				for (int j = 0; j < tileVertexCount; j++)
				{
					vertices.Add(tile.vertices[j]);
					normals.Add(tile.normals[j]);
					uvs.Add(tile.uvs[j]);
				}

				for (int t = 0; t < tileTriangleCount; t++)
				{
					triangles.Add(tile.triangles[t] + triangleOffset);
				}

				triangleOffset += tileVertexCount;
			}
		}

		public void ApplyToMesh()
		{
			mesh.Clear();
			mesh.SetVertices(vertices);
			mesh.SetNormals(normals);
			mesh.SetUVs(0, uvs);
			mesh.SetTriangles(triangles, 0);
		}
	}

}