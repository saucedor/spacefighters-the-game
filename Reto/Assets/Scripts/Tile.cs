using UnityEngine;
using System.Text.RegularExpressions;

[ExecuteAlways]  // Esto permite que corra también en el editor
public class Tile : MonoBehaviour {
    public int row;
    public int col;

    void OnValidate() {
        var match = Regex.Match(gameObject.name, @"[Tt]ai?l[_](\d+)_(\d+)");
        if (match.Success) {
            row = int.Parse(match.Groups[1].Value);
            col = int.Parse(match.Groups[2].Value);
        }
    }
}
