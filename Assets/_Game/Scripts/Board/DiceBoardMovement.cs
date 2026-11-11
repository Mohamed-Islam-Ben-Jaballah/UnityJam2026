using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DiceDrive.Characters;
using DiceDrive.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DiceDrive.Board
{
    /// <summary>
    /// Turn flow on the fixed path:
    /// roll → (optional single reroll) → click a highlighted destination → the player walks exactly the rolled
    /// number of positions. Where the path forks within the roll, every reachable destination is highlighted and the
    /// clicked one decides which branch is taken.
    /// When an enemy blocks the next position the walk pauses (<see cref="EncounterStarted"/>) until
    /// <see cref="ResolveEncounter"/> is called: Kill (Damage card) or TakeHit stop on the enemy's tile, Jump hops
    /// over it and keeps walking. Hidden devils trigger when crossed (hurt, stop).
    /// Rules/state live in <see cref="PathSession"/>; this component animates them.
    /// </summary>
    public sealed class DiceBoardMovement : MonoBehaviour
    {
        public enum TurnState { AwaitingRoll, AwaitingGo, Walking, AwaitingEncounter, Dead, Finished }

        [Serializable]
        public sealed class EnemyPlacement
        {
            [Min(1)] public int pathIndex = 1;
            public CharacterActor enemy;
        }

        [Serializable]
        public sealed class HiddenDevilPlacement
        {
            [Min(1)] public int pathIndex = 1;
            [Tooltip("Optional visual, hidden until the devil is revealed. A CharacterActor on it plays the attack.")]
            public GameObject visual;
        }

        [Header("References (auto-found when empty)")]
        [SerializeField] private DiceView dice;
        [SerializeField] private CharacterActor player;
        [SerializeField] private BoardPath path;
        [SerializeField] private BoardTileGlow[] glows;
        [SerializeField] private string playerTag = "Player";

        [Header("Rules")]
        [SerializeField] private PathRuleSettings rules = new PathRuleSettings();
        [SerializeField, Min(2)] private int dieSides = 6;
        [SerializeField, Min(0)] private int rerollsPerTurn = 1;

        [Header("Encounters")]
        [Tooltip("Visible enemies and their path index. Context menu 'Assign Enemies From Scene' fills it. " +
                 "If empty at play time, enemies standing on the path are assigned automatically.")]
        [SerializeField] private List<EnemyPlacement> enemies = new List<EnemyPlacement>();
        [SerializeField] private List<HiddenDevilPlacement> hiddenDevils = new List<HiddenDevilPlacement>();

        [Header("Presentation")]
        [Tooltip("How close the player walks up to an enemy before striking or being struck (world units).")]
        [SerializeField, Min(0.1f)] private float attackReach = 1.4f;
        [Tooltip("Seconds an enemy takes to fade out after hitting the player.")]
        [SerializeField, Min(0f)] private float evaporateDuration = 0.5f;
        [SerializeField] private Color encounterColor = new Color(1f, 0.25f, 0.2f, 1f);
        [Tooltip("Highlight of the destination tile the player clicks to walk.")]
        [SerializeField] private Color destinationColor = new Color(1f, 0.85f, 0.3f, 1f);

        private PathSession _session;
        private Vector3 _offset;
        private int _roll;
        private int _rerollsLeft;
        private EncounterChoice? _choice;
        private List<IReadOnlyList<int>> _routes = new List<IReadOnlyList<int>>();
        private TurnState _state = TurnState.AwaitingRoll;

        public PathSession Session => _session;
        public BoardPath Path => path;
        public int Position => _session?.Position ?? 0;
        public int Hp => _session?.Player.Hp ?? 0;
        public int MaxHp => _session?.Player.MaxHp ?? 0;
        public int CurrentRoll => _roll;
        public int RerollsLeft => _rerollsLeft;
        public bool IsBusy => _state == TurnState.Walking || _state == TurnState.AwaitingEncounter;

        /// <summary>The enemy currently blocking the walk (while <see cref="TurnState.AwaitingEncounter"/>).</summary>
        public PathEncounter PendingEnemy { get; private set; }

        /// <summary>True if the pending enemy can be jumped over (landing inside the path, no enemy there).</summary>
        public bool CanJumpPending => PendingEnemy != null && _session.JumpLanding(PendingEnemy) >= 0;

        public TurnState State
        {
            get => _state;
            private set
            {
                if (_state == value) return;
                _state = value;
                StateChanged?.Invoke(value);
            }
        }

        public event Action<TurnState> StateChanged;
        public event Action<PathEncounter> EncounterStarted;
        public event Action<int, int> HpChanged; // hp, max
        public event Action PlayerDied;
        public event Action ExitReached;
        public event Action TurnFinished;
        public event Action<CharacterActor, int> EnemyDefeated;
        public event Action<GameObject, int> DevilRevealed;
        /// <summary>Raised when the player clicks a path tile that isn't the rolled destination (tile index).</summary>
        public event Action<int> IllegalMoveAttempted;

        // ------------------------------------------------------------------ setup

        private void Awake()
        {
            ResolveReferences();
            if (path != null && path.Count < 2) path.AutoBuild(); // new level with an empty path: build it from the tilemaps
            if (player == null || path == null || path.Count < 2)
            {
                Debug.LogError("[DiceBoardMovement] Needs a player (tag 'Player') and a BoardPath with at least 2 positions.", this);
                enabled = false;
                return;
            }

            path.Validate();
            _session = new PathSession(path.Count, path.Next, rules);
            _session.HpChanged += hp => HpChanged?.Invoke(hp, _session.Player.MaxHp);
            _session.PlayerDied += () => PlayerDied?.Invoke();

            var nearest = path.NearestIndex(player.transform.position);
            _offset = player.transform.position - path.GetPosition(nearest);
            if (nearest != 0) player.SnapTo(path.GetPosition(0) + _offset);

            RegisterEncounters();
        }

        private void OnEnable()
        {
            if (dice == null) return;
            dice.RollStarted += OnDiceRollStarted;
            dice.Landed += ReceiveRoll;
        }

        private void OnDisable()
        {
            if (dice == null) return;
            dice.RollStarted -= OnDiceRollStarted;
            dice.Landed -= ReceiveRoll;
        }

        private void ResolveReferences()
        {
            if (dice == null) dice = FindAnyObjectByType<DiceView>(FindObjectsInactive.Include);
            if (path == null) path = GetComponent<BoardPath>() ?? FindAnyObjectByType<BoardPath>();
            if (player == null)
            {
                GameObject tagged = null;
                try { tagged = GameObject.FindWithTag(playerTag); } catch (UnityException) { /* tag not defined */ }
                if (tagged != null) player = tagged.GetComponentInParent<CharacterActor>();
            }

            if (glows == null || !glows.Any(g => g != null)) glows = FindObjectsByType<BoardTileGlow>(FindObjectsSortMode.None);
        }

        private void RegisterEncounters()
        {
            if (enemies.Count == 0) enemies = FindEnemiesOnPath();

            foreach (var e in enemies)
            {
                if (e.enemy != null && ValidIndex(e.pathIndex)) _session.AddEncounter(e.pathIndex, EncounterKind.Enemy, e.enemy);
            }

            foreach (var d in hiddenDevils)
            {
                if (!ValidIndex(d.pathIndex)) continue;
                if (d.visual != null) d.visual.SetActive(false);
                _session.AddEncounter(d.pathIndex, EncounterKind.HiddenDevil, d.visual);
            }
        }

        private bool ValidIndex(int index)
        {
            if (index >= 1 && index < path.Count) return true;
            Debug.LogWarning($"[DiceBoardMovement] Encounter index {index} is outside 1..{path.Count - 1}; ignored.", this);
            return false;
        }

        private List<EnemyPlacement> FindEnemiesOnPath()
        {
            var result = new List<EnemyPlacement>();
            foreach (var actor in FindObjectsByType<CharacterActor>(FindObjectsSortMode.None))
            {
                if (actor == player || actor.CompareTag(playerTag)) continue;
                var index = path.IndexAt(actor.transform.position);
                if (index >= 1) result.Add(new EnemyPlacement { pathIndex = index, enemy = actor });
            }

            return result.OrderBy(e => e.pathIndex).ToList();
        }

        [ContextMenu("Assign Enemies From Scene")]
        private void AssignEnemiesFromScene()
        {
            ResolveReferences();
            if (path == null) return;
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Assign Enemies");
#endif
            enemies = FindEnemiesOnPath();
            Debug.Log($"[DiceBoardMovement] Assigned {enemies.Count} enemies: " +
                      string.Join(", ", enemies.Select(e => $"{e.enemy.name}@{e.pathIndex}")), this);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        // ------------------------------------------------------------------ roll / reroll / go

        private void OnDiceRollStarted()
        {
            if (State == TurnState.AwaitingGo && _rerollsLeft > 0) _rerollsLeft--; // this roll is the reroll
        }

        /// <summary>Die result: first roll of the turn, or the reroll.</summary>
        public void ReceiveRoll(int roll)
        {
            if (!enabled || _session == null) return;

            if (State == TurnState.AwaitingRoll)
            {
                if (_session.IsDead || _session.AtExit) return;
                _roll = roll;
                _rerollsLeft = rerollsPerTurn;
                State = TurnState.AwaitingGo;
            }
            else if (State == TurnState.AwaitingGo)
            {
                _roll = roll; // reroll result
            }
            else
            {
                return;
            }

            if (dice != null) dice.Interactable = _rerollsLeft > 0; // die stays clickable only for the reroll
            _routes = _session.RoutesFor(_roll);
            Highlight(Destinations, destinationColor);
        }

        /// <summary>
        /// Where the current roll can end the walk if nothing interrupts it: one position per route (several when
        /// the path forks within the roll).
        /// </summary>
        public IEnumerable<int> Destinations => _routes.Select(r => r[r.Count - 1]);

        // Walk starts by clicking the highlighted destination tile.
        private void Update()
        {
            if (State != TurnState.AwaitingGo || (dice != null && dice.IsRolling)) return;

            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            var cam = Camera.main;
            if (cam == null) return;
            Vector3 world = cam.ScreenToWorldPoint(pointer.position.ReadValue());
            world.z = 0f;
            int tile = path.IndexAt(world);
            if (tile < 0) return; // clicked off the path: ignore

            // Any highlighted destination is valid (several when the path forks); the clicked one picks the branch.
            if (!IsDestination(tile) || !Go(tile)) IllegalMoveAttempted?.Invoke(tile);
        }

        /// <summary>True if <paramref name="tile"/> is one of the current roll's <see cref="Destinations"/>.</summary>
        public bool IsDestination(int tile) => Destinations.Contains(tile);

        /// <summary>DEVELOPMENT ONLY: roll (or reroll) to a chosen result.</summary>
        public bool ForceRoll(int value)
        {
            var canRoll = State == TurnState.AwaitingRoll || (State == TurnState.AwaitingGo && _rerollsLeft > 0);
            if (!canRoll || value < 1 || value > dieSides || (dice != null && dice.IsRolling)) return false;
            if (dice != null && dice.isActiveAndEnabled) dice.PlayRoll(value, dieSides);
            else
            {
                OnDiceRollStarted();
                ReceiveRoll(value);
            }

            return true;
        }

        /// <summary>Starts the walk with the current roll (only when it has a single possible destination).</summary>
        public bool Go() => _routes.Count == 1 && Go(Destinations.First());

        /// <summary>Starts the walk towards one of the <see cref="Destinations"/>, taking that branch at any fork.</summary>
        public bool Go(int destination)
        {
            if (State != TurnState.AwaitingGo || (dice != null && dice.IsRolling)) return false;
            var route = _routes.FirstOrDefault(r => r[r.Count - 1] == destination);
            if (route == null) return false;

            _session.SetRoute(route);
            if (dice != null) dice.Interactable = false;
            ClearHighlight();
            StartCoroutine(WalkRoutine());
            return true;
        }

        /// <summary>Answers the pending enemy encounter.</summary>
        public bool ResolveEncounter(EncounterChoice choice)
        {
            if (State != TurnState.AwaitingEncounter || _choice != null) return false;
            if (choice == EncounterChoice.Jump && !CanJumpPending) return false;
            _choice = choice;
            return true;
        }

        // ------------------------------------------------------------------ walk

        private IEnumerator WalkRoutine()
        {
            State = TurnState.Walking;
            var remaining = _roll;

            while (remaining > 0 && !_session.AtExit && !_session.IsDead)
            {
                // Walk freely up to the next blocker (enemy, hidden devil) or the end of the roll.
                var segment = new List<Vector3>();
                PathEncounter devil = null;
                while (remaining > 0 && !_session.AtExit && _session.Next >= 0 && _session.EnemyAt(_session.Next) == null)
                {
                    devil = _session.StepForward();
                    remaining--;
                    segment.Add(PointAt(_session.Position));
                    if (devil != null) break;
                }

                if (segment.Count > 0) yield return player.MoveAlongRoutine(segment);

                if (devil != null)
                {
                    yield return DevilRoutine(devil);
                    break; // a hidden devil stops the walk
                }

                if (remaining == 0 || _session.AtExit || _session.Next < 0) break;

                // An enemy blocks the next position: ask for a card.
                PendingEnemy = _session.EnemyAt(_session.Next);
                _choice = null;
                Highlight(new[] { PendingEnemy.Index }, encounterColor);
                State = TurnState.AwaitingEncounter;
                EncounterStarted?.Invoke(PendingEnemy);
                while (_choice == null) yield return null;

                var enemy = PendingEnemy;
                var actor = enemy.Tag as CharacterActor;
                ClearHighlight();
                State = TurnState.Walking;

                switch (_choice.Value)
                {
                    case EncounterChoice.Kill:
                        _session.DefeatEnemy(enemy);
                        yield return KillRoutine(actor, enemy.Index);
                        remaining = 0;
                        break;

                    case EncounterChoice.Jump:
                        remaining = Math.Max(0, remaining - _session.JumpOver(enemy, out var landingDevil));
                        yield return player.JumpToRoutine(PointAt(_session.Position));
                        if (landingDevil != null)
                        {
                            yield return DevilRoutine(landingDevil, alreadyThere: true);
                            remaining = 0;
                        }

                        break;

                    default:
                        _session.TakeEnemyHit(enemy);
                        yield return TakeHitRoutine(actor, enemy.Index);
                        remaining = 0;
                        break;
                }

                PendingEnemy = null;
            }

            EndTurn();
        }

        private void EndTurn()
        {
            _session.ClearRoute();
            _routes.Clear();
            if (_session.IsDead)
            {
                State = TurnState.Dead;
            }
            else if (_session.AtExit)
            {
                State = TurnState.Finished;
                ExitReached?.Invoke();
            }
            else
            {
                State = TurnState.AwaitingRoll;
                if (dice != null) dice.Interactable = true;
            }

            TurnFinished?.Invoke();
        }

        private IEnumerator KillRoutine(CharacterActor enemy, int index)
        {
            if (enemy != null)
            {
                yield return player.MoveAlongRoutine(new List<Vector3> { NearPoint(enemy.transform.position) });
                player.Face(enemy.transform.position);
                yield return player.AttackTargetRoutine(enemy, true);
                EnemyDefeated?.Invoke(enemy, index);
                Destroy(enemy.gameObject);
            }

            yield return player.MoveAlongRoutine(new List<Vector3> { PointAt(index) });
        }

        private IEnumerator TakeHitRoutine(CharacterActor enemy, int index)
        {
            var dies = _session.IsDead;
            if (enemy != null)
            {
                yield return player.MoveAlongRoutine(new List<Vector3> { NearPoint(enemy.transform.position) });
                yield return enemy.AttackTargetRoutine(player, dies);
                yield return Evaporate(enemy.gameObject);
            }
            else
            {
                yield return dies ? player.DieRoutine() : player.ReceiveHitRoutine(PointAt(index));
            }

            if (dies)
            {
                if (!player.IsDead) yield return player.DieRoutine();
                yield break;
            }

            yield return player.MoveAlongRoutine(new List<Vector3> { PointAt(index) });
        }

        private IEnumerator DevilRoutine(PathEncounter devil, bool alreadyThere = false)
        {
            var visual = devil.Tag as GameObject;
            if (visual != null) visual.SetActive(true);
            DevilRevealed?.Invoke(visual, devil.Index);

            var dies = _session.IsDead;
            var actor = visual != null ? visual.GetComponentInChildren<CharacterActor>() : null;
            if (actor != null) yield return actor.AttackTargetRoutine(player, dies);
            else yield return dies ? player.DieRoutine() : player.ReceiveHitRoutine(PointAt(devil.Index));
        }

        /// <summary>Fades every sprite on the enemy out, then removes it.</summary>
        private IEnumerator Evaporate(GameObject enemy)
        {
            var sprites = enemy.GetComponentsInChildren<SpriteRenderer>();
            var start = sprites.Select(s => s.color).ToArray();
            for (var t = 0f; t < evaporateDuration; t += Time.deltaTime)
            {
                var a = 1f - t / evaporateDuration;
                for (var i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null) sprites[i].color = new Color(start[i].r, start[i].g, start[i].b, start[i].a * a);
                }

                yield return null;
            }

            Destroy(enemy);
        }

        private Vector3 PointAt(int index) => path.GetPosition(index) + _offset;

        private Vector3 NearPoint(Vector3 target)
        {
            var from = player.transform.position;
            var delta = target - from;
            return delta.magnitude <= attackReach ? from : target - delta.normalized * attackReach;
        }

        // ------------------------------------------------------------------ highlights

        private void Highlight(IEnumerable<int> indices, Color color)
        {
            var cells = indices.SelectMany(path.GetCells).Distinct().ToList();
            foreach (var glow in glows)
            {
                if (glow != null) glow.SetHighlights(cells.Where(glow.IsBoardCell), color);
            }
        }

        private void ClearHighlight()
        {
            foreach (var glow in glows)
            {
                if (glow != null) glow.ClearHighlights();
            }
        }
    }
}
