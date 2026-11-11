using System;
using System.Collections.Generic;
using System.Linq;
using DiceDrive.Entities;

namespace DiceDrive.Board
{
    public enum EncounterKind
    {
        Enemy,
        HiddenDevil
    }

    /// <summary>How the player deals with an enemy blocking the walk.</summary>
    public enum EncounterChoice
    {
        /// <summary>No usable card: take the hit, the enemy evaporates, stop on its tile.</summary>
        TakeHit,
        /// <summary>Damage card: kill the enemy, stop on its tile.</summary>
        Kill,
        /// <summary>Jump card: hop over the enemy and keep walking.</summary>
        Jump
    }

    /// <summary>One enemy or hidden devil fixed to a path position. Authoritative gameplay state.</summary>
    public sealed class PathEncounter
    {
        public int Index { get; }
        public EncounterKind Kind { get; }
        public bool Resolved { get; internal set; }
        public bool Alive { get; internal set; } = true;
        public bool Revealed { get; internal set; }
        public bool Bypassed { get; internal set; }

        /// <summary>Free slot for the presenter (e.g. the enemy GameObject).</summary>
        public object Tag { get; set; }

        public PathEncounter(int index, EncounterKind kind, object tag = null)
        {
            Index = index;
            Kind = kind;
            Revealed = kind == EncounterKind.Enemy;
            Tag = tag;
        }
    }

    [Serializable]
    public sealed class PathRuleSettings
    {
        public int startingHp = 6;
        public int maxHp = 6;
        public int enemyHitDamage = 1;
        public int devilDamage = 1;

        [UnityEngine.Tooltip("Positions a Jump card skips (the enemy's tile).")]
        public int jumpSkip = 1;
    }

    /// <summary>
    /// Path rules (plain C#): position, HP and encounters by path index. Positions form a graph (forks allowed):
    /// before walking, the presenter picks one of <see cref="RoutesFor"/> and sets it with <see cref="SetRoute"/>;
    /// the player then steps along it one position at a time and asks these primitives what happens. Resolved
    /// encounters never trigger again.
    /// </summary>
    public sealed class PathSession
    {
        private readonly PathRuleSettings _settings;
        private readonly Func<int, IReadOnlyList<int>> _next;
        private readonly Dictionary<int, List<PathEncounter>> _byIndex = new Dictionary<int, List<PathEncounter>>();
        private readonly List<PathEncounter> _encounters = new List<PathEncounter>();
        private readonly List<int> _route = new List<int>();

        /// <summary>Number of positions (0 = start).</summary>
        public int Count { get; }
        public int Position { get; private set; }
        public PlayerState Player { get; }
        public bool IsDead => !Player.IsAlive;
        public bool AtExit => IsExit(Position);
        public IReadOnlyList<PathEncounter> Encounters => _encounters;

        /// <summary>Positions still ahead on the chosen route (not including the current one).</summary>
        public IReadOnlyList<int> Route => _route;

        /// <summary>The position the next step goes to: the route's next one, else the only way on; -1 if none/ambiguous.</summary>
        public int Next => _route.Count > 0 ? _route[0] : Single(Successors(Position));

        public event Action<int> HpChanged;
        public event Action PlayerDied;

        /// <summary>Straight path: 0 → 1 → … → <paramref name="exitIndex"/>.</summary>
        public PathSession(int exitIndex, PathRuleSettings settings, int startIndex = 0)
            : this(exitIndex + 1, i => i < exitIndex ? new[] { i + 1 } : new int[0], settings, startIndex)
        {
        }

        /// <param name="count">Number of positions.</param>
        /// <param name="next">Positions reachable in one step from a position (empty = exit).</param>
        public PathSession(int count, Func<int, IReadOnlyList<int>> next, PathRuleSettings settings, int startIndex = 0)
        {
            if (count < 2) throw new ArgumentOutOfRangeException(nameof(count));
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _settings = settings ?? new PathRuleSettings();
            Count = count;
            Position = Math.Max(0, Math.Min(startIndex, count - 1));
            var max = Math.Max(1, _settings.maxHp);
            Player = new PlayerState { MaxHp = max, Hp = Math.Max(0, Math.Min(_settings.startingHp, max)) };
        }

        public IReadOnlyList<int> Successors(int index) => _next(index) ?? new int[0];
        public bool IsExit(int index) => Successors(index).Count == 0;

        /// <summary>
        /// Every route a roll of <paramref name="steps"/> can take from the current position, one per distinct
        /// destination (a route stops early at an exit). Each route lists the positions after the current one.
        /// </summary>
        public List<IReadOnlyList<int>> RoutesFor(int steps)
        {
            var routes = new List<IReadOnlyList<int>>();
            var destinations = new HashSet<int>();
            var current = new List<int>();

            void Walk(int from, int left)
            {
                var next = Successors(from);
                if (left == 0 || next.Count == 0)
                {
                    if (current.Count > 0 && destinations.Add(from)) routes.Add(current.ToArray());
                    return;
                }

                foreach (var n in next)
                {
                    current.Add(n);
                    Walk(n, left - 1);
                    current.RemoveAt(current.Count - 1);
                }
            }

            Walk(Position, Math.Max(0, steps));
            return routes;
        }

        /// <summary>Sets the positions to walk (must start next to the current position and follow the links).</summary>
        public void SetRoute(IEnumerable<int> route)
        {
            var list = route?.ToList() ?? new List<int>();
            var from = Position;
            foreach (var n in list)
            {
                if (!Successors(from).Contains(n)) throw new ArgumentException($"Position {n} doesn't follow {from}.", nameof(route));
                from = n;
            }

            _route.Clear();
            _route.AddRange(list);
        }

        public void ClearRoute() => _route.Clear();

        public PathEncounter AddEncounter(int index, EncounterKind kind, object tag = null)
        {
            if (index < 1 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            var encounter = new PathEncounter(index, kind, tag);
            if (!_byIndex.TryGetValue(index, out var list)) _byIndex[index] = list = new List<PathEncounter>();
            list.Add(encounter);
            _encounters.Add(encounter);
            return encounter;
        }

        /// <summary>The unresolved, living visible enemy on a position, or null.</summary>
        public PathEncounter EnemyAt(int index) => Find(index, EncounterKind.Enemy);

        /// <summary>
        /// Moves one position forward onto a tile without a visible enemy. Returns the hidden devil it triggered
        /// (revealed, player hurt) or null.
        /// </summary>
        public PathEncounter StepForward()
        {
            var next = Next;
            if (AtExit || IsDead || next < 0) throw new InvalidOperationException("Can't step forward (no route chosen?).");
            if (EnemyAt(next) != null) throw new InvalidOperationException("An enemy blocks the next position.");

            MoveTo(next, 1);
            var devil = Find(Position, EncounterKind.HiddenDevil);
            if (devil != null)
            {
                devil.Revealed = true;
                devil.Resolved = true;
                Hurt(_settings.devilDamage);
            }

            return devil;
        }

        /// <summary>Damage card: the enemy dies and the player stands on its tile.</summary>
        public void DefeatEnemy(PathEncounter enemy)
        {
            CheckBlocking(enemy);
            enemy.Alive = false;
            enemy.Resolved = true;
            MoveTo(enemy.Index, 1);
        }

        /// <summary>No card: the player is hurt, the enemy evaporates, the player stands on its tile.</summary>
        public int TakeEnemyHit(PathEncounter enemy)
        {
            CheckBlocking(enemy);
            enemy.Alive = false;
            enemy.Resolved = true;
            MoveTo(enemy.Index, 1);
            return Hurt(_settings.enemyHitDamage);
        }

        /// <summary>
        /// Landing position of a jump over this enemy, or -1 if it can't be jumped (past an exit, enemy beyond, or a
        /// fork right after the enemy with no route chosen through it).
        /// </summary>
        public int JumpLanding(PathEncounter enemy)
        {
            var landing = enemy.Index;
            var skip = Math.Max(1, _settings.jumpSkip);
            for (var k = 1; k <= skip; k++)
            {
                // Along the chosen route first (route[0] is the enemy), then the only way on.
                landing = k < _route.Count ? _route[k] : Single(Successors(landing));
                if (landing < 0 || EnemyAt(landing) != null) return -1;
            }

            return landing;
        }

        /// <summary>Jump card: hop over the enemy (it stays alive, never hurts the player). Returns positions moved.</summary>
        public int JumpOver(PathEncounter enemy, out PathEncounter devilAtLanding)
        {
            CheckBlocking(enemy);
            var landing = JumpLanding(enemy);
            if (landing < 0) throw new InvalidOperationException("This enemy can't be jumped.");

            enemy.Bypassed = true;
            enemy.Resolved = true;
            var moved = 1 + Math.Max(1, _settings.jumpSkip);
            MoveTo(landing, moved);

            devilAtLanding = Find(landing, EncounterKind.HiddenDevil);
            if (devilAtLanding != null)
            {
                devilAtLanding.Revealed = true;
                devilAtLanding.Resolved = true;
                Hurt(_settings.devilDamage);
            }

            return moved;
        }

        public int Heal(int amount)
        {
            if (IsDead || amount <= 0) return 0;
            var before = Player.Hp;
            Player.Hp = Math.Min(Player.MaxHp, Player.Hp + amount);
            if (Player.Hp != before) HpChanged?.Invoke(Player.Hp);
            return Player.Hp - before;
        }

        private void CheckBlocking(PathEncounter enemy)
        {
            if (enemy == null || enemy.Index != Next || EnemyAt(enemy.Index) != enemy)
            {
                throw new InvalidOperationException("That enemy is not blocking the next position.");
            }
        }

        /// <summary>Moves <paramref name="steps"/> positions ahead and drops them from the route.</summary>
        private void MoveTo(int index, int steps)
        {
            Position = index;
            _route.RemoveRange(0, Math.Min(steps, _route.Count));
        }

        private static int Single(IReadOnlyList<int> list) => list.Count == 1 ? list[0] : -1;

        private PathEncounter Find(int index, EncounterKind kind)
        {
            if (!_byIndex.TryGetValue(index, out var list)) return null;
            foreach (var e in list)
            {
                if (e.Kind == kind && !e.Resolved && e.Alive) return e;
            }

            return null;
        }

        private int Hurt(int amount)
        {
            amount = Math.Max(0, amount);
            if (amount == 0 || IsDead) return 0;
            var before = Player.Hp;
            Player.Hp = Math.Max(0, Player.Hp - amount);
            HpChanged?.Invoke(Player.Hp);
            if (Player.Hp == 0) PlayerDied?.Invoke();
            return before - Player.Hp;
        }
    }
}
