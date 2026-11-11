using System;
using System.Collections.Generic;
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
    /// Fixed-path rules (plain C#): position, HP and encounters by path index. The presenter walks the player one
    /// position at a time and asks these primitives what happens; resolved encounters never trigger again.
    /// </summary>
    public sealed class PathSession
    {
        private readonly PathRuleSettings _settings;
        private readonly Dictionary<int, List<PathEncounter>> _byIndex = new Dictionary<int, List<PathEncounter>>();
        private readonly List<PathEncounter> _encounters = new List<PathEncounter>();

        public int ExitIndex { get; }
        public int Position { get; private set; }
        public PlayerState Player { get; }
        public bool IsDead => !Player.IsAlive;
        public bool AtExit => Position >= ExitIndex;
        public IReadOnlyList<PathEncounter> Encounters => _encounters;

        public event Action<int> HpChanged;
        public event Action PlayerDied;

        public PathSession(int exitIndex, PathRuleSettings settings, int startIndex = 0)
        {
            if (exitIndex < 1) throw new ArgumentOutOfRangeException(nameof(exitIndex));
            _settings = settings ?? new PathRuleSettings();
            ExitIndex = exitIndex;
            Position = Math.Max(0, Math.Min(startIndex, exitIndex));
            var max = Math.Max(1, _settings.maxHp);
            Player = new PlayerState { MaxHp = max, Hp = Math.Max(0, Math.Min(_settings.startingHp, max)) };
        }

        public PathEncounter AddEncounter(int index, EncounterKind kind, object tag = null)
        {
            if (index < 1 || index > ExitIndex) throw new ArgumentOutOfRangeException(nameof(index));
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
            if (AtExit || IsDead) throw new InvalidOperationException("Can't step forward.");
            if (EnemyAt(Position + 1) != null) throw new InvalidOperationException("An enemy blocks the next position.");

            Position++;
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
            Position = enemy.Index;
        }

        /// <summary>No card: the player is hurt, the enemy evaporates, the player stands on its tile.</summary>
        public int TakeEnemyHit(PathEncounter enemy)
        {
            CheckBlocking(enemy);
            enemy.Alive = false;
            enemy.Resolved = true;
            Position = enemy.Index;
            return Hurt(_settings.enemyHitDamage);
        }

        /// <summary>Landing position of a jump over this enemy, or -1 if it can't be jumped (exit/enemy beyond).</summary>
        public int JumpLanding(PathEncounter enemy)
        {
            var landing = enemy.Index + Math.Max(1, _settings.jumpSkip);
            if (landing > ExitIndex) return -1; // never jump beyond the exit
            for (var i = enemy.Index + 1; i <= landing; i++)
            {
                if (EnemyAt(i) != null) return -1;
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
            var moved = landing - Position;
            Position = landing;

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
            if (enemy == null || enemy.Index != Position + 1 || EnemyAt(enemy.Index) != enemy)
            {
                throw new InvalidOperationException("That enemy is not blocking the next position.");
            }
        }

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
