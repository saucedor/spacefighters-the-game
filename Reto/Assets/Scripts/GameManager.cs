using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// -----------------
// Clases para JSON
// -----------------
[System.Serializable]
public class AgentData {
    public int id;
    public int[] pos;  // [row, col]
    public bool carrying;
    public string carrying_status;
}

[System.Serializable]
public class PoiData {
    public int[] pos;  // [row, col]
    public string status;
}

[System.Serializable]
public class DoorData {
    public int[][] pos;
    public string state;
}

[System.Serializable]
public class GameState {
    public int turn;
    public string status;
    public int rescued;
    public int lost;
    public int damage;
    public AgentData[] agents;
    public PoiData[] pois;
    public int[][] fire;
    public int[][] smoke;
    public int[][][] walls;
    public DoorData[] doors;
}

// -----------------
// GameManager
// -----------------
public class GameManager : MonoBehaviour {
    [Header("Prefabs")]
    public GameObject astronautPrefab;
    public GameObject poiPrefab;
    public GameObject firePrefab;
    public GameObject smokePrefab;
    public GameObject wallPrefab;

    [Header("Configuración de Simulación")]
    [Tooltip("Elige la estrategia de los astronautas: reasoned o random")]
    public string strategy = "reasoned";
    [Tooltip("Segundos entre turnos (0.1 = muy rápido, 1.0 = tiempo real)")]
    public float speed = 0.5f;

    private Dictionary<int, GameObject> agentObjects = new Dictionary<int, GameObject>();
    private List<GameObject> fireObjects = new List<GameObject>();
    private List<GameObject> smokeObjects = new List<GameObject>();
    private List<GameObject> poiObjects = new List<GameObject>();
    private List<GameObject> wallObjects = new List<GameObject>();

    // Diccionario de tiles por (row,col)
    private Dictionary<(int,int), Tile> tileLookup = new Dictionary<(int,int), Tile>();

    void Start() {
        BuildTileDictionary();
        StartCoroutine(StartSimulation());
        InvokeRepeating(nameof(RequestState), 1f, 1f);
    }

    void BuildTileDictionary() {
        tileLookup.Clear();
        GameObject tilesRoot = GameObject.Find("Tiles");
        if (tilesRoot != null) {
            foreach (Tile tile in tilesRoot.GetComponentsInChildren<Tile>()) {
                tileLookup[(tile.row, tile.col)] = tile;
            }
            Debug.Log($"✅ Tiles cargados en diccionario: {tileLookup.Count}");
        } else {
            Debug.LogError("❌ No encontré el GameObject 'Tiles' en la escena.");
        }
    }

    Tile FindTile(int row, int col) {
        if (tileLookup.TryGetValue((row, col), out Tile tile)) {
            return tile;
        }
        Debug.LogWarning($"⚠️ Tile no encontrado en diccionario: ({row},{col})");
        return null;
    }

    IEnumerator StartSimulation() {
        string json = "{\"strategy\":\"" + strategy + "\",\"speed\":" + speed.ToString("0.0") + "}";
        var request = new UnityWebRequest("http://127.0.0.1:5000/config", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            Debug.Log("Config OK: " + request.downloadHandler.text);
        } else {
            Debug.LogError("Error config: " + request.error);
        }
    }

    void RequestState() {
        StartCoroutine(GetState());
    }

    IEnumerator GetState() {
        var request = UnityWebRequest.Get("http://127.0.0.1:5000/all");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            string json = request.downloadHandler.text;
            GameState state = JsonUtility.FromJson<GameState>(json);
            UpdateScene(state);
        } else {
            Debug.LogError("Error state: " + request.error);
        }
    }

    void ClearObjects(List<GameObject> objects) {
        foreach (var obj in objects) Destroy(obj);
        objects.Clear();
    }

    void UpdateScene(GameState state) {
        // --- Astronautas ---
        foreach (AgentData agent in state.agents) {
            Tile tile = FindTile(agent.pos[0], agent.pos[1]);
            if (tile == null) continue;

            if (!agentObjects.ContainsKey(agent.id)) {
                var obj = Instantiate(astronautPrefab, tile.transform.position, Quaternion.identity);
                agentObjects[agent.id] = obj;
                Debug.Log($"👨‍🚀 Spawn agente {agent.id} en {tile.row},{tile.col}");
            } else {
                agentObjects[agent.id].transform.position = tile.transform.position;
            }
        }

        // --- Fuego ---
        ClearObjects(fireObjects);
        foreach (int[] pos in state.fire) {
            Tile tile = FindTile(pos[0], pos[1]);
            if (tile == null) continue;
            fireObjects.Add(Instantiate(firePrefab, tile.transform.position, Quaternion.identity));
        }

        // --- Humo ---
        ClearObjects(smokeObjects);
        foreach (int[] pos in state.smoke) {
            Tile tile = FindTile(pos[0], pos[1]);
            if (tile == null) continue;
            smokeObjects.Add(Instantiate(smokePrefab, tile.transform.position, Quaternion.identity));
        }

        // --- POIs ---
        ClearObjects(poiObjects);
        foreach (PoiData poi in state.pois) {
            Tile tile = FindTile(poi.pos[0], poi.pos[1]);
            if (tile == null) continue;
            poiObjects.Add(Instantiate(poiPrefab, tile.transform.position, Quaternion.identity));
        }
        
        // --- Paredes ---
        // Ya no instanciamos, solo actualizamos los muros existentes
        WallTile[] allWalls = FindObjectsOfType<WallTile>();

        foreach (var wallData in state.walls) {
            int[] a = wallData[0];
            int[] b = wallData[1];

            foreach (WallTile wall in allWalls) {
                if (wall.Matches(a, b)) {
                    wall.SetState("damaged"); // o "destroyed", según venga de la simulación
                }
            }
        }

    }
}
