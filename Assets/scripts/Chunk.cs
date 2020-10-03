using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public GameObject chunkObject;
    MeshFilter meshFilter;
    MeshCollider meshCollider;
    MeshRenderer meshRenderer;

    Vector3Int chunkPosition;

    float[,,] terrainMap;

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    int width { get { return GameData.ChunkWidth; } }
    int height { get { return GameData.ChunkHeight; } }
    float terrainSurface{ get { return GameData.terrainSurface; } }

    public Chunk (Vector3Int _position)
    {
        chunkObject = new GameObject();
        chunkObject.name = string.Format("Chunk {0}, {1}", _position.x, _position.z);
        chunkPosition = _position;
        chunkObject.transform.position = chunkPosition;

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshCollider = chunkObject.AddComponent<MeshCollider>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshRenderer.material = Resources.Load<Material>("Materials/GroundMaterial");
        chunkObject.transform.tag = "Terrain";
        terrainMap = new float[width + 1, height + 1, width + 1];
        PopulateTerrainMap();
        CreateMeshData();

    }

    void CreateMeshData()
    {

        ClearMeshData();

        // Loop through each "cube" in our terrain.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < width; z++)
                {

                    // Pass the value into our MarchCube function.
                    MarchCube(new Vector3Int(x, y, z));

                }
            }
        }

        BuildMesh();

    }

    void PopulateTerrainMap()
    {

        // The data points for terrain are stored at the corners of our "cubes", so the terrainMap needs to be 1 larger
        // than the width/height of our mesh.
        for (int x = 0; x < width + 1; x++)
        {
            for (int z = 0; z < width + 1; z++)
            {
                for (int y = 0; y < height + 1; y++)
                {

                    // Get a terrain height using regular old Perlin noise.
                    float thisHeight = GameData.GetTerrainHeight(x + chunkPosition.x, z + chunkPosition.z);

                    // Set the value of this point in the terrainMap.
                    terrainMap[x, y, z] = (float)y - thisHeight;

                }
            }
        }
    }

    void MarchCube(Vector3Int position)
    {
        float[] cube = new float[8];
        for (int i = 0; i < 8; i++)
        {

            cube[i] = SampleTerrain(position + GameData.CornerTable[i]);

        }

        // Get the configuration index of this cube.
        int configIndex = GetCubeConfiguration(cube);

        // If the configuration of this cube is 0 or 255 (completely inside the terrain or completely outside of it) we don't need to do anything.
        if (configIndex == 0 || configIndex == 255)
            return;

        // Loop through the triangles. There are never more than 5 triangles to a cube and only three vertices to a triangle.
        int edgeIndex = 0;
        for (int i = 0; i < 5; i++)
        {
            for (int p = 0; p < 3; p++)
            {

                // Get the current indice. We increment triangleIndex through each loop.
                int indice = GameData.TriangleTable[configIndex, edgeIndex];

                // If the current edgeIndex is -1, there are no more indices and we can exit the function.
                if (indice == -1)
                    return;

                // Get the vertices for the start and end of this edge.
                Vector3 vert1 = position + GameData.CornerTable[GameData.EdgeIndexes[indice, 0]];
                Vector3 vert2 = position + GameData.CornerTable[GameData.EdgeIndexes[indice, 1]];

                // Get the midpoint of this edge.
                Vector3 vertPosition = (vert1 + vert2) / 2f;

                // Add to our vertices and triangles list and incremement the edgeIndex.
                vertices.Add(vertPosition);
                triangles.Add(vertices.Count - 1);
                edgeIndex++;

            }
        }
    }

    int GetCubeConfiguration(float[] cube)
    {

        // Starting with a configuration of zero, loop through each point in the cube and check if it is below the terrain surface.
        int configurationIndex = 0;
        for (int i = 0; i < 8; i++)
        {

            // If it is, use bit-magic to the set the corresponding bit to 1. So if only the 3rd point in the cube was below
            // the surface, the bit would look like 00100000, which represents the integer value 32.
            if (cube[i] > terrainSurface)
                configurationIndex |= 1 << i;

        }

        return configurationIndex;

    }

    void ClearMeshData()
    {

        vertices.Clear();
        triangles.Clear();

    }

    public void PlaceTerrain(Vector3 pos)
    {

        Vector3Int v3Int = new Vector3Int(Mathf.CeilToInt(pos.x), Mathf.CeilToInt(pos.y), Mathf.CeilToInt(pos.z));
        terrainMap[v3Int.x, v3Int.y, v3Int.z] = 0f;
        CreateMeshData();

    }

    public void RemoveTerrain(Vector3 pos)
    {

        Vector3Int v3Int = new Vector3Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
        terrainMap[v3Int.x, v3Int.y, v3Int.z] = 1f;
        CreateMeshData();

    }

    float SampleTerrain (Vector3Int point)
    {

        return terrainMap[point.x, point.y, point.z];

    }

    void BuildMesh()
    {

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

    }

}