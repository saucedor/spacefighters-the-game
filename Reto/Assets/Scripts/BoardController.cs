using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour {
    public Dictionary<(int,int), Tile> tiles = new();

    void Awake() {
        foreach (var tile in GetComponentsInChildren<Tile>()) {
            tiles[(tile.row, tile.col)] = tile;
        }
    }

    public Vector3 GetWorldPosition(int r, int c) {
        if (tiles.TryGetValue((r, c), out var tile)) {
            return tile.transform.position;
        }
        Debug.LogWarning($"No hay tile en fila {r}, col {c}");
        return Vector3.zero;
    }
}
