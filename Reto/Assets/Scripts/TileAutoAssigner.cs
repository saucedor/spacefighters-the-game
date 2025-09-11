using UnityEngine;

[ExecuteInEditMode]  // Funciona también en el editor, no solo en Play
public class TileAutoAssigner : MonoBehaviour
{
    public Transform tilesRoot; // Arrastra aquí tu objeto "Tiles"

    [ContextMenu("Asignar filas y columnas")]
    void AssignTiles()
    {
        if (tilesRoot == null)
        {
            Debug.LogError("⚠️ No asignaste tilesRoot (ej. 'Tiles').");
            return;
        }

        int rowIndex = 0;
        foreach (Transform fila in tilesRoot)
        {
            int colIndex = 0;
            foreach (Transform tileObj in fila)
            {
                Tile tile = tileObj.GetComponent<Tile>();
                if (tile != null)
                {
                    tile.row = rowIndex;
                    tile.col = colIndex;
                    UnityEditor.EditorUtility.SetDirty(tile); // Guarda cambios en editor
                    Debug.Log($"✅ Tile asignado: {tileObj.name} -> row={rowIndex}, col={colIndex}");
                }
                colIndex++;
            }
            rowIndex++;
        }
    }
}
