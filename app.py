from flask import Flask, jsonify, request
import TacoRescue

app = Flask(__name__)

model = TacoRescue.TacoRescueModel()

@app.route("/")
def home():
    return "Flask API está en operación"

@app.route("/step", methods=["POST"])
def step():
    """Avanca un step más."""
    while not model.end_game():
        model.step()
        return jsonify({"step": model.steps})
    return jsonify({"step": "MAX"})

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
        "poi": model.fire.tolist()
    }
    return jsonify(state)

if __name__ == "__main__":
    import os
    port = int(os.environ.get("PORT", 5000))
    app.run(host="0.0.0.0", port=port)
