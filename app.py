from flask import Flask, jsonify, request
import TacoRescueStrat

app = Flask(__name__)

model = TacoRescueStrat.TacoRescueModel()

@app.route("/")
def home():
    return "Flask API está en operación"

def convert_keys(d):
    if isinstance(d, dict):
        return {str(k): convert_keys(v) for k, v in d.items()}
    elif isinstance(d, list):
        return [convert_keys(i) for i in d]
    else:
        return d

@app.route("/step", methods=["POST"])
def step():
    global model
    if model.end_game():
        print("Se acabo la simulación, reiniciando el modelo...")
        model = TacoRescue.TacoRescueModel()
        return jsonify({"step": "Reinicado"})
    model.step()
    return jsonify({"step": model.steps})

@app.route("/state", methods=["GET"])
def get_state():
    """Regresa el estado actual de la simulación."""
    state = {
        "step": model.steps,
        "agents": [
            {
                "id": agent.id,
                "x": agent.pos[0],
                "y": agent.pos[1],
                "carrying_victim": getattr(agent, "carrying_victim", False),
                "AP": getattr(agent, "AP", False)
            }
            for i, agent in enumerate(model.schedule.agents)
        ],
        "events": model.events,
        "fire": model.fire.tolist(),
        "walls": model.walls.tolist(),
        "walls_damage": model.walls_damage.tolist(),
        "doors": model.doors,
        "poi": model.poi_unknown,
        "damage": model.damage,
        "rescued_count": model.rescued_count,
        "lost_victims": model.lost_victims
    }
    return jsonify(convert_keys(state))

if __name__ == "__main__":
    import os
    port = int(os.environ.get("PORT", 5000))
    app.run(host="0.0.0.0", port=port)
