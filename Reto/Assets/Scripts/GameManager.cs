using System.Collections.Generic;
using UnityEngine;

// --- DATA CLASSES ---
[System.Serializable] public class PoiData { public int[] pos; public string status; }
[System.Serializable] public class DoorData { public int[] pos; public string state; }
[System.Serializable] public class TilesData {
    public List<int[]> fire;
    public List<int[]> smoke;
    public List<PoiData> poi;
    public List<int[]> walls;
    public List<DoorData> doors;
}
[System.Serializable] public class AgentData {
    public int id;
    public int[] pos;
    public bool carrying;
    public string carrying_status;
}
[System.Serializable] public class StateData {
    public int turn;
    public string status;
    public List<AgentData> agents;
    public TilesData tiles;
}

public class GameManager : MonoBehaviour {
    private string[] stateFiles = {
        "States/state_0",
        "States/state_1",
        "States/state_2",
        "States/state_3",
        "States/state_4",
        "States/state_5"
    };

    public GameObject firePrefab, smokePrefab, poiPrefab;
    public BoardController board;  

    private int currentStep = 0;
    private List<GameObject> markers = new List<GameObject>();

    void Start() {
        LoadStep(0);
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            currentStep = (currentStep + 1) % stateFiles.Length;
            LoadStep(currentStep);
        }
    }

    void LoadStep(int step) {
        TextAsset asset = Resources.Load<TextAsset>(stateFiles[step]);
        if (asset == null) {
            Debug.LogError("No se encontró el archivo: " + stateFiles[step]);
            return;
        }

        StateData state = JsonUtility.FromJson<StateData>(asset.text);
        Debug.Log($"Turno {state.turn}, Estado: {state.status}");

        ClearOldMarkers();

        // 🔥 Fuego
        if (state.tiles.fire != null) {
            foreach (var pos in state.tiles.fire) {
                Vector3 worldPos = board.GetWorldPosition(pos[1], pos[0]); 
                markers.Add(Instantiate(firePrefab, worldPos, Quaternion.identity, board.transform));
            }
        }

        // 💨 Humo
        if (state.tiles.smoke != null) {
            foreach (var pos in state.tiles.smoke) {
                Vector3 worldPos = board.GetWorldPosition(pos[1], pos[0]);
                markers.Add(Instantiate(smokePrefab, worldPos, Quaternion.identity, board.transform));
            }
        }

        // ❓ POI
        if (state.tiles.poi != null) {
            foreach (var p in state.tiles.poi) {
                Vector3 worldPos = board.GetWorldPosition(p.pos[1], p.pos[0]);
                markers.Add(Instantiate(poiPrefab, worldPos, Quaternion.identity, board.transform));
            }
        }
    }

    void ClearOldMarkers() {
        foreach (var obj in markers) {
            Destroy(obj);
        }
        markers.Clear();
    }
}
