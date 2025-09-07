# export_states.py
import json
from space_fighters import SpaceFightersModel

def export_state(model, step):
    state = {
        "turn": model.turn,
        "status": model.status,
        "agents": [
            {
                "id": a.unique_id,
                "pos": a.pos,
                "carrying": a.carrying,
                "carrying_status": a.carrying_status
            }
            for a in model.schedule.agents
        ],
        "tiles": {
            "fire": model.fire_positions,
            "smoke": model.smoke_positions,
            "poi": [
                {"pos": p, "status": model.victim_status[p]}
                for p in model.poi_positions
            ],
            "walls": list(model.wall_damage.keys()),
            "doors": [
                {"pos": d, "state": s}
                for d, s in model.door_state.items()
            ]
        }
    }
    with open(f"output/state_{step}.json", "w") as f:
        json.dump(state, f, indent=2)

def main():
    model = SpaceFightersModel(seed=42)
    max_steps = 5  # exportamos solo 5 turnos para probar
    for step in range(max_steps):
        export_state(model, step)
        model.step()

if __name__ == "__main__":
    import os
    os.makedirs("output", exist_ok=True)
    main()
