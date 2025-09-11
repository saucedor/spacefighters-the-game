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
public class Coord {
    public int row;
    public int col;
}

[System.Serializable]
public class WallData {
    public Coord a;
    public Coord b;
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
    public Coord[] fire;
    public Coord[] smoke;
    public WallData[] walls;
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
    public string strategy = "reasoned";
    public float speed = 0.5f;

    private Dictionary<int, GameObject> agentObjects = new Dictionary<int, GameObject>();
    private Dictionary<(int,int), GameObject> fireMap = new Dictionary<(int,int), GameObject>();
    private Dictionary<(int,int), GameObject> smokeMap = new Dictionary<(int,int), GameObject>();
    private Dictionary<(int,int), GameObject> poiMap = new Dictionary<(int,int), GameObject>();

    private Dictionary<(int,int), Tile> tileLookup = new Dictionary<(int,int), Tile>();

    private bool isRequestingState = false;
    private bool simulationStarted = false;

    void Start() {
        BuildTileDictionary();
        StartCoroutine(StartSimulation());
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
        string json = "{\"strategy\":\"" + strategy + "\",\"speed\":" + speed.ToString("0.0") + ",\"auto_mode\":false}";
        var request = new UnityWebRequest("http://127.0.0.1:5000/config", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            Debug.Log("Config OK: " + request.downloadHandler.text);
            simulationStarted = true;
            StartCoroutine(GameLoop());
        } else {
            Debug.LogError("Error config: " + request.error);
        }
    }

    IEnumerator GameLoop() {
        while (simulationStarted) {
            if (!isRequestingState) {
                StartCoroutine(GetState());
            }
            yield return new WaitForSeconds(speed);
        }
    }

    IEnumerator GetState() {
        isRequestingState = true;

        var request = UnityWebRequest.Get("http://127.0.0.1:5000/all");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            string json = request.downloadHandler.text;
            try {
                GameState state = JsonUtility.FromJson<GameState>(json);
                UpdateScene(state);

                if (state.status != null && state.status.ToLower() == "finished") {
                    simulationStarted = false;
                    Debug.Log("🎮 Simulación terminada");
                }
            } catch (System.Exception e) {
                Debug.LogError($"Error parseando JSON: {e.Message}");
            }
        } else {
            Debug.LogError("Error state: " + request.error);
        }

        isRequestingState = false;
    }

    void UpdateScene(GameState state) {
        if (state.turn % 5 == 0) {
            Debug.Log($"🎮 Turno {state.turn} - Rescatados: {state.rescued}, Perdidos: {state.lost}");
        }

        UpdateAgents(state.agents);
        UpdateFires(state.fire);
        UpdateSmokes(state.smoke);
        UpdatePOIs(state.pois);

        // --- Paredes ---
        if (state.walls != null) {
            WallTile[] allWalls = FindObjectsOfType<WallTile>();
            foreach (WallData wallData in state.walls) {
                foreach (WallTile wall in allWalls) {
                    if (wall.Matches(wallData.a, wallData.b)) {
                        wall.SetState(wallData.state);
                    }
                }
            }
        }
    }

    void UpdateAgents(AgentData[] agents) {
        foreach (AgentData agent in agents) {
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
    }

    void UpdateFires(Coord[] fires) {
        foreach (var obj in fireMap.Values) obj.SetActive(false);
        if (fires == null) return;

        foreach (Coord f in fires) {
            var key = (f.row, f.col);
            if (!fireMap.ContainsKey(key)) {
                Tile tile = FindTile(f.row, f.col);
                if (tile == null) continue;
                var fireObj = Instantiate(firePrefab, tile.transform.position + Vector3.up * 0.2f, Quaternion.identity);
                fireMap[key] = fireObj;
            }
            fireMap[key].SetActive(true);
        }
    }

    void UpdateSmokes(Coord[] smokes) {
        foreach (var obj in smokeMap.Values) obj.SetActive(false);
        if (smokes == null) return;

        foreach (Coord s in smokes) {
            var key = (s.row, s.col);
            if (!smokeMap.ContainsKey(key)) {
                Tile tile = FindTile(s.row, s.col);
                if (tile == null) continue;
                var smokeObj = Instantiate(smokePrefab, tile.transform.position + Vector3.up * 0.2f, Quaternion.identity);
                smokeMap[key] = smokeObj;
            }
            smokeMap[key].SetActive(true);
        }
    }

    void UpdatePOIs(PoiData[] pois) {
        foreach (var obj in poiMap.Values) obj.SetActive(false);
        if (pois == null) return;

        foreach (PoiData poi in pois) {
            var key = (poi.pos[0], poi.pos[1]);
            if (!poiMap.ContainsKey(key)) {
                Tile tile = FindTile(poi.pos[0], poi.pos[1]);
                if (tile == null) continue;
                var poiObj = Instantiate(poiPrefab, tile.transform.position, Quaternion.identity);
                poiMap[key] = poiObj;
            }
            poiMap[key].SetActive(true);
        }
    }

    public void StopSimulation() {
        simulationStarted = false;
        StopAllCoroutines();
    }

    void OnDestroy() {
        StopSimulation();
    }
}
