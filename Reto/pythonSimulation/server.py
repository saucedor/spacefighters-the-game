import threading, time
from flask import Flask, request, jsonify
from flask_cors import CORS
from spacefighters_6x8 import SpaceFightersModel, PARAMS

app = Flask(__name__)
CORS(app)

model = None
running = False
speed = 0.5  # valor por defecto (0.5 segundos por turno)


def run_model_loop():
    """Loop que avanza la simulación automáticamente."""
    global model, running, speed
    while running and model is not None and not model.game_over:
        model.step()
        time.sleep(speed)


@app.route("/config", methods=["POST"])
def config():
    global model, running, speed
    data = request.json if request.is_json else {}
    params = {**PARAMS, **data}
    strategy = data.get("strategy", "reasoned")
    speed = float(data.get("speed", 0.5))  # 👈 velocidad configurable

    model = SpaceFightersModel(
        n_astronauts=params["n_astronauts"],
        n_fire=params["n_fire"],
        n_smoke=params["n_smoke"],
        max_ap=params["max_ap"],
        seed=params["seed"],
        rescued_target=params["rescued_target"],
        lost_threshold=params["lost_threshold"],
        damage_threshold=params["damage_threshold"],
        initial_damage=params["initial_damage"],
        replenish_pois=True,
        map_file="mapa_6x8.txt",
        default_strategy=strategy
    )

    # Iniciar loop automático
    running = True
    t = threading.Thread(target=run_model_loop, daemon=True)
    t.start()

    return jsonify({
        "status": "ok",
        "params": params,
        "strategy": strategy,
        "speed": speed
    })


@app.route("/all", methods=["GET"])
def get_all():
    global model
    if model is None:
        return jsonify({"error": "No hay simulación iniciada"}), 400
    return jsonify(model.to_dict())


@app.route("/stop", methods=["POST"])
def stop():
    """Detiene la simulación automática."""
    global running
    running = False
    return jsonify({"status": "stopped"})


if __name__ == "__main__":
    app.run(host="127.0.0.1", port=5000, debug=True)
