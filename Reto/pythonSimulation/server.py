from spacefighters_6x8 import SpaceFightersModel

from flask import Flask, request, jsonify
import os

app = Flask(__name__)
model = None

# -------------------------
# Helpers de conversión
# -------------------------
def to_unity_dict(m: SpaceFightersModel):
    """Convierte el estado interno del modelo al JSON que Unity espera."""
    data = {
        "turn": m.turn,
        "status": m.status,
        "rescued": sum(a.victims_saved for a in m.schedule.agents),
        "lost": m.count_lost(),
        "damage": m.damage_markers,
        "agents": [],
        "pois": [],
        "fire": [],
        "smoke": [],
        "walls": []
    }

    # --- Agentes ---
    for a in m.schedule.agents:
        if hasattr(a, "pos") and a.pos is not None:
            row, col = a.pos[1], a.pos[0]   # Mesa usa (x,y) = (col,row)
            data["agents"].append({
                "id": a.unique_id,
                "pos": [row, col],
                "carrying": a.carrying,
                "carrying_status": a.carrying_status or ""
            })

    # --- POIs ---
    for p in m.poi_positions:
        row, col = p[1], p[0]
        status = m.victim_status.get(p, "none")
        data["pois"].append({
            "pos": [row, col],
            "status": status
        })

    # --- Fuego ---
    for f in m.fire_positions:
        row, col = f[1], f[0]
        data["fire"].append({"row": row, "col": col})

    # --- Humo ---
    for s in m.smoke_positions:
        row, col = s[1], s[0]
        data["smoke"].append({"row": row, "col": col})

    # --- Paredes ---
    for e, dmg in m.wall_damage_edge.items():
        a, b = list(e)
        a_row, a_col = a[1], a[0]
        b_row, b_col = b[1], b[0]
        if dmg == 0:
            state = "intact"
        elif dmg == 1:
            state = "damaged"
        else:
            state = "destroyed"
        data["walls"].append({
            "a": {"row": a_row, "col": a_col},
            "b": {"row": b_row, "col": b_col},
            "state": state
        })

    return data


# -------------------------
# Endpoints
# -------------------------
@app.route("/config", methods=["POST"])
def config():
    """Inicializa el modelo con parámetros (opcionales) enviados por Unity."""
    global model
    params = request.json or {}
    default_params = dict(
        n_astronauts=4,
        n_fire=6,
        n_smoke=2,
        max_ap=4,
        rescued_target=7,
        lost_threshold=4,
        damage_threshold=25,
        initial_damage=0,
        default_strategy=params.get("strategy", "reasoned"),
        replenish_pois=False,
        map_file="mapa_6x8.txt"  # usa el archivo existente en el folder
    )
    model = SpaceFightersModel(**default_params)
    return jsonify({"status": "ok", "message": "Modelo inicializado"})


@app.route("/all", methods=["GET"])
def all_state():
    """Avanza la simulación 1 paso y devuelve estado completo."""
    if not model:
        return jsonify({"error": "Modelo no inicializado"}), 400
    model.step()
    return jsonify(to_unity_dict(model))


@app.route("/steps/<int:n>", methods=["POST"])
def step_n(n):
    """Avanza n pasos y devuelve estado."""
    if not model:
        return jsonify({"error": "Modelo no inicializado"}), 400
    for _ in range(n):
        model.step()
    return jsonify(to_unity_dict(model))


# -------------------------
# Main
# -------------------------
if __name__ == "__main__":
    port = int(os.environ.get("PORT", 5000))
    app.run(host="0.0.0.0", port=port, debug=True)
