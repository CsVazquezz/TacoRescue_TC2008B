from flask import Flask, jsonify, request
import TacoRescue


# Inicializar el modelo
model = TacoRescue.TacoRescueModel()

app = Flask(__name__)

@app.route("/")
def home():
    return "Flask API está en operación"

@app.route("/step", methods=["POST"])
def step():
    """Avanca un step más."""
    if (model.steps < 50):
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
                "id": i,
                "x": agent.pos[0],
                "y": agent.pos[1],
                "carrying_victim": getattr(agent, "carrying_victim", False)
            }
            for i, agent in enumerate(model.schedule.agents)
        ],
        "fire": model.fire.tolist(),
        "walls": model.walls.tolist(),
        "walls_damage": model.walls_damage.tolist(),
        "victims": [{"x": x, "y": y} for (x, y) in model.victims],
        "false_alarms": [{"x": x, "y": y} for (x, y) in model.false_alarms]
    }
    return jsonify(state)

if __name__ == "__main__":
    import os
    port = int(os.environ.get("PORT", 5000))
    app.run(host="0.0.0.0", port=port)
