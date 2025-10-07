# TacoRescue

TacoRescue is a Unity project (with some helper Python tools) stored in this repository. This README explains how to open the Unity project, run the included Python utilities, and how to contribute or troubleshoot when files or assets look out of sync.

## Quick summary
- Unity project folder: `TacoRescue/`
- Main branch: `main`
- Useful scripts: `app.py`, `TacoRescue.py` and Jupyter notebooks `TacoRescueRandom.ipynb`, `TacoRescueStrat.ipynb`

## Project layout
- `TacoRescue/` — the Unity project (Assets, Packages, ProjectSettings, etc.)
  - `TacoRescue/Assets/` — game assets, prefabs, scenes and scripts
  - `TacoRescue/ProjectSettings/` — Unity project configuration
  - `TacoRescue/Packages/` — Unity package manifest
- Root files — Python helpers and notebooks: `app.py`, `TacoRescue.py`, `requirements.txt`, `*.ipynb`

## Open the Unity project
1. Open Unity Hub.
2. Click `Add` (or `Open`) and navigate to the project folder:

```
/Users/<your-username>/Documents/GitHub/TacoRescue_TC2008B/TacoRescue
```

3. Select the `TacoRescue` folder (do NOT open the repository root itself). Unity will import assets and generate the Library folder.
4. If Unity asks, use the editor version listed in `TacoRescue/ProjectSettings/ProjectVersion.txt` if available. The file currently contains an editor version string; use the closest matching Unity Editor or the latest LTS if you don’t have an exact match.

Notes:
- Importing will take time for large assets — be patient while Unity regenerates the `Library/` folder.
- If binary assets are missing after cloning, you may need to run Git LFS if the repo uses it:

```
git lfs fetch --all
git lfs pull
```

## Using Visual Studio / VS Code with Unity
- Open the folder `TacoRescue` in VS Code or open the generated solution `TacoRescue/TacoRescue.sln` in Visual Studio.
- In Unity, go to `Edit > Preferences > External Tools` and set your preferred IDE (Visual Studio / VS Code). Then choose `Assets > Open C# Project` to regenerate the project files if IntelliSense is out of sync.

## Running Python helpers
There are small Python scripts and notebooks in the repo. To run them locally:

1. Create and activate a virtual environment (macOS / zsh):

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install --upgrade pip
pip install -r requirements.txt
```

2. Run a script (example):

```bash
python app.py
```

Notebooks can be opened with Jupyter Lab / Notebook:

```bash
jupyter lab
```

## Common troubleshooting
- If Unity shows no files but VS Code does:
  - Make sure you opened the `TacoRescue` folder in Unity Hub (not the repo root).
  - In Unity: `Assets > Refresh` and `File > Open C# Project`.
  - Ensure you cloned the repository to the path you expect and open that path in Finder.
- If you see many deleted files in `git status`:
  - Cancel any ongoing git operations, remove `.git/index.lock` if safe, then run `git checkout -- .` to discard local changes (only when you intend to throw away local edits).
- If large assets are missing: run Git LFS commands shown above.

## Git workflow / branch guidance
- Keep `main` clean. Create feature branches for changes:

```bash
git checkout -b feature/<short-description>
```

- Commit often with clear messages and push to your fork or origin. When ready, open a pull request against `main`.

## Contributing
- Open an issue with a clear title and reproduction steps for bugs.
- For code contributions, create a branch and open a pull request. Include screenshots or short recordings for Unity-related UI/scene changes when helpful.

## Contact / Ownership
- Repository owner: `CsVazquezz` (GitHub)
- If you need access to large asset files or a specific Unity editor version, contact the owner or the person who invited you to the project.

## License
Check for a `LICENSE` file in the repository root. If none exists, ask the repo owner which license applies before re-using assets or code.

----
If you want, I can also:
- Add a CONTRIBUTING.md with pull request templates.
- Create a minimal Unity README inside `TacoRescue/` with recommended Editor settings.
- Add a small GitHub Actions workflow to validate that Python scripts run in CI.

Tell me which of those you'd like next and I'll add it.

## How the project works (high-level)

This section explains how the game's runtime and the agents work so that new contributors and reviewers can quickly understand the architecture and algorithmic decisions.

Notes about assumptions: the repository contains grid manager and view classes (`Assets/Scripts/Framework/Views/AgentGrid.cs`, `FireGrid.cs`, `PoiGrid.cs`, `WallDamageGrid.cs`) and a main controller (`TacoRescueController.cs`). Based on those files, the following describes the intended architecture — if any detail below doesn't match the code, treat it as a design summary you can adapt while exploring the actual source files.

### Core idea
- The game world is represented as a 2D grid of cells (managed by `AgentGrid` and the various grid views). Game entities (agents, fires, POIs, doors and wall-damage) are mapped on that grid.
- Agents operate as autonomous actors that sense their local neighborhood, plan a path to a target (POI, victim, exit, fire), and execute actions (move, rescue, extinguish, open door).

### Agent architecture (component overview)
- Agent (GameObject + AgentController / scripts)
  - Perception: each agent reads nearby grid cells for fire, damage, POIs, and obstacles.
  - Planner: pathfinding module (grid-based, typically A* or Dijkstra over walkable cells) creates a sequence of waypoints to the chosen target.
  - Decision maker: a lightweight finite-state machine (FSM) or prioritized behavior selector chooses between goals such as `FindVictim`, `ExtinguishFire`, `RepairWall`, `ReturnToBase`.
  - Actuator: movement and interaction code that moves the agent along the planned path and triggers events (pickup, extinguish, interact).

### Typical agent loop (pseudocode)

1. Perceive: read surrounding cells (range R) and update internal blackboard/state.
2. Evaluate goals: compute a small list of candidate goals with heuristic priorities (distance, severity, time-since-last-visited).
3. Plan: call pathfinder to compute path to top-priority goal.
4. Act: move one step along the path, perform necessary actions when arriving.
5. Re-evaluate every tick or when an event occurs (fire spreads, new POI appears, block removed).

### Pathfinding and heuristics
- Pathfinding is grid-based. The project likely uses a simple A* over walkable cells; the heuristic is Euclidean or Manhattan distance depending on 4/8-neighborhood movement.
- Goal scoring uses a small heuristic combining:
  - distance cost (shorter preferred)
  - urgency/severity (e.g., fires or critically injured victims get higher weight)
  - availability (avoid goals already claimed by another agent)

If you'd like, I can search the repository for an explicit A* implementation and link the exact file here.

### Simulation & experiment code
- The repository contains Jupyter notebooks (`TacoRescueRandom.ipynb` and `TacoRescueStrat.ipynb`) and Python helper scripts. These are used for experimenting with agent strategies, visualizing results, or running batch simulations outside Unity.

## Developer notes — where to look in the code
- `TacoRescue/Assets/Scripts/Framework/Controllers/TacoRescueController.cs` — main game controller: orchestrates ticks and high-level game state.
- `TacoRescue/Assets/Scripts/Framework/Views/AgentGrid.cs` — grid manager and utility functions for converting between world positions and grid indices.
- `TacoRescue/Assets/Scripts/Framework/Views/*Grid.cs` — FireGrid, PoiGrid, WallDamageGrid: each manages a domain-specific layer of the grid.
- `TacoRescue/Assets/Prefabs/` — look here for agent prefabs and example configured components.

## Running and tuning the agent algorithm

1. Open the project in Unity (see earlier instructions).
2. Open a scene such as `TacoRescue/Assets/Scenes/SampleScene.unity` or `PruebaAPI.unity`.
3. In Play Mode, attach the Unity Profiler or open the Console to observe agent logs.
4. Tuning knobs:
   - perception range (cells)
   - decision tick rate (how often agents re-evaluate goals)
   - heuristic weights (distance vs urgency)

Many of these values are defined as public fields on MonoBehaviours and can be tweaked in the Inspector at runtime.

## Testing and debugging
- Unit / automated tests: this repo currently does not include C# unit tests by default. You can add Test Runner tests in `Assets/Tests/` and run them with the Unity Test Runner.
- Quick debug workflow:
  - Add `Debug.Log` messages in `AgentController` and `TacoRescueController` to see agent state transitions.
  - Use gizmos to draw planned paths and target cells in the Scene view (add `OnDrawGizmos` to the agent script).
  - Run the notebooks to reproduce strategy behavior outside Unity and validate heuristic changes quickly.

## Edge cases & gotchas
- Concurrency: multiple agents trying to claim the same target — introduce simple reservation/claiming to avoid duplicated effort.
- Stuck agents: ensure the pathfinder returns `null` or a failure state and agents can fallback (replan with a different goal).
- Large asset imports: Unity may regenerate `Library/` which takes time; don't confuse this with missing files in the repo.

## Suggested next documentation updates
- Add inline doc comments to `AgentGrid` and `TacoRescueController` explaining key data structures (grid indexing conventions, cell value ranges).
- Add a short `docs/` folder with diagrams (PNG) showing the grid layout and agent FSM.
- Add example debug scenes that enable visual gizmos and show agent behavior with varied heuristic weights.

If you want, I can now:
- Search the project for specific function names (A* or pathfinding) and link exact file lines in this README.
- Create a `docs/` folder and add a simple FSM diagram and a short `TUTORIAL.md` explaining how to tweak agent parameters.

Which follow-up would you like me to do next? (I can automatically search and link the code, or add a docs folder with a diagram.)
# TacoRescue_TC2008B