using UnityEngine;
using System.Text.RegularExpressions;

public class Tile : MonoBehaviour {
    public int row;
    public int col;

    void Awake() {
        var match = Regex.Match(gameObject.name, @"Tile_(\d+)_(\d+)");
        if (match.Success) {
            row = int.Parse(match.Groups[1].Value);
            col = int.Parse(match.Groups[2].Value);
        }
    }
}
