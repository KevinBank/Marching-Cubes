using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    private void Start()
    {
        Chunk chunk = new Chunk(Vector3Int.zero);
    }
}
