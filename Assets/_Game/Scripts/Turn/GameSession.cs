using System;
using System.Collections.Generic;
using System.Linq;
using DiceDrive.Cards;
using DiceDrive.Core;
using DiceDrive.DiceSystem;
using DiceDrive.Entities;
using DiceDrive.Grid;

namespace DiceDrive.Turn
{
    public sealed class GameSession
    {
        private readonly RunConfig _config;
        private MapData _map;
        private readonly Random _random;
        private RewardSelection _rewardSelection;
        private MapData _pendingDestination;
        private ThemeData _pendingTheme;
        private bool _transitionInProgress;
        private readonly Pathfinder _pathfinder = new Pathfinder();
        private readonly List<EnemyState> _enemies = new List<EnemyState>();
        private readonly List<GridCoord> _openedTreasure = new List<GridCoord>();
        private readonly HashSet<GridCoord> _forfeitedExits = new HashSet<GridCoord>();
        private PathGraph _pathGraph;
        private int _linearMovementRemaining;
        private bool _linearMovementActive;
        private GridCoord? _branchDirection;
        private ExitSpawn _pendingExit;
        private bool _hasPendingExit;
        private List<CardData> _runDeckOrder = new List<CardData>();

        private GridMap _grid;
        private PlayerState _player;
        private Deck _deck;
        private Hand _hand;
        private SpentPile _spent;
        private DiceSet _dice;
        private TorchTimer _torch;
        private List<CardData> _levelStartDeckOrder = new List<CardData>();

        private int _turnNumber;
        private int _currentRoll;
        private int _nextEnemyId = 1;
        private int _entryHp;
        private int _diceRolledThisTurn;
        private int _freeRerollsRemaining;
        private int _levelNumber = 1;
        private bool _levelComplete;
        private bool _runOver;
        private bool _retryInProgress;

        public GridMap Grid => _grid;
        public PlayerState Player => _player;
        public IReadOnlyList<EnemyState> Enemies => _enemies;
        public Deck DrawPile => _deck;
        public Hand PlayerHand => _hand;
        public SpentPile Spent => _spent;
        public DiceSet Dice => _dice;
        public TorchTimer Torch => _torch;
        public TurnPhase Phase { get; private set; }
        public int TurnNumber => _turnNumber;
        public int CurrentRoll => _currentRoll;
        public int FreeRerollsRemaining => _freeRerollsRemaining;
        public int LevelNumber => _levelNumber;
        public bool CanPaidReroll => _levelNumber >= 2;
        public int Gold { get; private set; }
        public bool IsLevelComplete => _levelComplete;
        public bool IsRunOver => _runOver;
        public IReadOnlyList<CardData> RunDeck => _levelStartDeckOrder;
        public IReadOnlyList<GridCoord> OpenedTreasure => _openedTreasure;
        public MapData MapData => _map;
        public RewardSelection RewardSelection => _rewardSelection;
        public bool IsOnExit => TryGetCurrentExit(out _);
        public bool IsOnTreasure => _map != null && _map.TryGetTreasureAt(_player != null ? _player.Position : default, out _);
        public bool IsLinearMap => _map != null && _map.IsLinear;
        public PathGraph PathGraph => _pathGraph;
        public bool IsAwaitingPathChoice => Phase == TurnPhase.AwaitingPathChoice;
        public bool IsAwaitingExitChoice => Phase == TurnPhase.AwaitingExitChoice;
        public bool HasAnyChoicePrompt => IsAwaitingPathChoice || IsAwaitingExitChoice;
        public GridCoord? BranchDirection => _branchDirection;
        public IReadOnlyList<GridCoord> BranchOptions =>
            _pathGraph != null && _player != null
                ? _pathGraph.GetForwardOptions(_player.Position)
                : Array.Empty<GridCoord>();

        public event Action<TurnPhase> PhaseChanged;
        public event Action<int> DiceRolled;
        public event Action<CardInstance, GridCoord> CardPlayed;
        public event Action<CardInstance> CardDrawn;
        public event Action<CardInstance> CardDiscarded;
        public event Action<EnemyState, int, EnemyAttackTiming> EnemyAttacked;
        public event Action<EnemyState> EnemyDied;
        public event Action<int, DamageSource> PlayerDamaged;
        public event Action<GridCoord, IReadOnlyList<CardInstance>, int> TreasureOpened;
        public event Action LevelRetried;
        public event Action LevelCompleted;
        public event Action RunEnded;
        public event Action<MapData> LevelTransitioned;

        public GameSession(RunConfig config, MapData map, Random random)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public void StartLevel(int? startingHp = null, int levelNumber = 1)
        {
            if (Phase != TurnPhase.NotStarted)
            {
                return;
            }

            _levelNumber = Math.Max(1, levelNumber);
            _freeRerollsRemaining = Math.Max(0, _config.freeRerollsPerMap);
            _entryHp = startingHp ?? _config.maxHp;
            _player = new PlayerState
            {
                MaxHp = _config.maxHp,
                Hp = _entryHp,
                Position = _map.playerSpawn
            };

            _runDeckOrder = _config.startingDeck != null
                ? new List<CardData>(_config.startingDeck)
                : new List<CardData>();
            _levelStartDeckOrder = BuildLevelDeck(_runDeckOrder, _map);

            _deck = new Deck(CreateInstances(_levelStartDeckOrder));
            _deck.Shuffle(_random);
            _levelStartDeckOrder = _deck.Cards.Select(card => card.Data).ToList();

            ResetLinearState(true);
            _openedTreasure.Clear();
            _levelComplete = false;
            _runOver = false;
            _retryInProgress = false;
            _transitionInProgress = false;
            _rewardSelection = null;
            _pendingDestination = null;
            _pendingTheme = null;
            _turnNumber = 0;
            _currentRoll = 0;
            _diceRolledThisTurn = 0;
            Gold = 0;

            BuildWorld();

            _hand = CreateHand();
            _spent = new SpentPile();
            _dice = CreateDiceSet();

            _torch = new TorchTimer(_config.torchSeconds);
            _torch.Burnout += OnTorchBurnout;
            _torch.Start();

            DrawCards(_config.handSize, false);
            BeginTurn();
        }

        public void TickTorch(float deltaSeconds)
        {
            if (_runOver || _levelComplete || Phase == TurnPhase.Reward ||
                Phase == TurnPhase.Loading || Phase == TurnPhase.LevelRetry)
            {
                return;
            }

            _torch?.Tick(deltaSeconds);
        }

        public ActionResult RollCurrentDie()
        {
            if (Phase != TurnPhase.AwaitingRoll)
            {
                return ActionResult.Fail(ActionStatus.InvalidPhase, "The session is not waiting for a roll.");
            }

            if (_dice == null || _dice.Current == null)
            {
                return ActionResult.Fail(ActionStatus.InvalidPhase, "No die is available.");
            }

            _currentRoll = _dice.RollCurrent(_random);
            _diceRolledThisTurn++;
            Phase = TurnPhase.AwaitingCard;
            PhaseChanged?.Invoke(Phase);
            DiceRolled?.Invoke(_currentRoll);
            return ActionResult.Ok();
        }

        public IEnumerable<CardInstance> GetPlayableCards()
        {
            if (_hand == null || Phase != TurnPhase.AwaitingCard)
            {
                yield break;
            }

            foreach (var card in _hand.Cards)
            {
                if (IsTypePlayable(card) &&
                    (IsLinearMap ? CanPlayAuto(card, out _) : HasAnyValidTarget(card)))
                {
                    yield return card;
                }
            }
        }

        public bool HasPlayableCard()
        {
            return GetPlayableCards().Any();
        }

        public bool CanPlay(CardInstance card, GridCoord target, out string reason)
        {
            reason = null;

            if (Phase != TurnPhase.AwaitingCard)
            {
                reason = "The session is not waiting for a card.";
                return false;
            }

            if (card == null || card.Data == null)
            {
                reason = "Invalid card.";
                return false;
            }

            if (!IsTypePlayable(card))
            {
                reason = "The card type is locked for this roll.";
                return false;
            }

            if (IsLinearMap)
            {
                return CanPlayAuto(card, out reason);
            }

            return HasValidTarget(card, target, out reason);
        }

        public ActionResult PlayCard(CardInstance card, GridCoord target)
        {
            if (IsLinearMap)
            {
                return PlayCardAuto(card);
            }

            if (!CanPlay(card, target, out var reason))
            {
                return ActionResult.Fail(ActionStatus.InvalidTarget, reason);
            }

            _hand.Remove(card);
            _spent.Add(card);
            ExecuteEffect(card, target);
            CardPlayed?.Invoke(card, target);
            return FinishCardPlay();
        }

        public ActionResult PlayCardAuto(CardInstance card)
        {
            if (!CanPlayAuto(card, out var reason))
            {
                return ActionResult.Fail(ActionStatus.InvalidTarget, reason);
            }

            var effect = card.Data.effect;
            _hand.Remove(card);
            _spent.Add(card);
            ExecuteLinearEffect(card);
            CardPlayed?.Invoke(card, _player.Position);

            if (effect == CardEffectType.Walk || effect == CardEffectType.Jump)
            {
                return ActionResult.Ok();
            }

            return FinishCardPlay();
        }

        private ActionResult FinishCardPlay()
        {
            if (_runOver || _levelComplete || Phase == TurnPhase.LevelRetry ||
                Phase == TurnPhase.Reward || Phase == TurnPhase.Loading ||
                Phase == TurnPhase.AwaitingRoll || Phase == TurnPhase.AwaitingPathChoice ||
                Phase == TurnPhase.AwaitingExitChoice || _linearMovementActive || _hasPendingExit)
            {
                return ActionResult.Ok();
            }

            if (_dice.Advance())
            {
                Phase = TurnPhase.AwaitingRoll;
                PhaseChanged?.Invoke(Phase);
            }
            else
            {
                EndTurn();
            }

            return ActionResult.Ok();
        }

        public ActionResult Reroll()
        {
            if (Phase != TurnPhase.AwaitingCard)
            {
                return ActionResult.Fail(ActionStatus.InvalidPhase, "The session is not waiting for a card.");
            }

            if (HasPlayableCard())
            {
                return ActionResult.Fail(ActionStatus.CannotReroll, "A playable card exists.");
            }

            if (_freeRerollsRemaining > 0)
            {
                _freeRerollsRemaining--;
            }
            else
            {
                if (_levelNumber < 2)
                {
                    return SkipCurrentDie();
                }

                if (_player.Hp <= 1)
                {
                    DamagePlayer(1, DamageSource.Reroll);
                    return ActionResult.Ok();
                }

                DamagePlayer(1, DamageSource.Reroll);
            }

            _currentRoll = _dice.RollCurrent(_random);
            DiceRolled?.Invoke(_currentRoll);
            return ActionResult.Ok();
        }

        private ActionResult SkipCurrentDie()
        {
            if (_dice.Advance())
            {
                Phase = TurnPhase.AwaitingRoll;
                PhaseChanged?.Invoke(Phase);
            }
            else
            {
                EndTurn();
            }

            return ActionResult.Ok();
        }

        public IReadOnlyDictionary<GridCoord, PathResult> GetWalkPaths()
        {
            return _pathfinder.FindPaths(
                _grid,
                _player.Position,
                _currentRoll,
                BlocksWalk,
                TileCost);
        }

        public bool IsValidJump(GridCoord target)
        {            var start = _player.Position;
            var dx = target.X - start.X;
            var dy = target.Y - start.Y;

            if (Math.Abs(dx) > 2 || Math.Abs(dy) > 2)
            {
                return false;
            }

            if (Math.Abs(dx) != 0 && Math.Abs(dx) != 2)
            {
                return false;
            }

            if (Math.Abs(dy) != 0 && Math.Abs(dy) != 2)
            {
                return false;
            }

            if (Math.Abs(dx) != 2 && Math.Abs(dy) != 2)
            {
                return false;
            }

            if (!_grid.InBounds(target) || !_grid.IsTerrainWalkable(target))
            {
                return false;
            }

            var mid = new GridCoord(start.X + dx / 2, start.Y + dy / 2);
            if (!_grid.InBounds(mid) || _grid.IsWall(mid))
            {
                return false;
            }

            foreach (var enemy in _enemies)
            {
                if (!enemy.IsAlive || enemy.Data == null)
                {
                    continue;
                }

                if (enemy.Position == mid && !enemy.Data.CanBeJumpedOver)
                {
                    return false;
                }

                if (enemy.Position == target && enemy.Data.BlocksMovement)
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsInEnemyRange(EnemyState enemy, GridCoord coord)
        {
            return IsInRange(enemy, coord);
        }

        public IEnumerable<GridCoord> GetEnemyRangeTiles(EnemyState enemy)
        {
            if (enemy == null || enemy.Data == null)
            {
                yield break;
            }

            if (enemy.Data.IsMelee)
            {
                yield return enemy.Position;
                yield break;
            }

            foreach (var direction in GridCoord.EightDirections)
            {
                var coord = enemy.Position + direction;
                if (_grid.InBounds(coord))
                {
                    yield return coord;
                }
            }
        }

        private void BeginTurn()
        {
            if (_runOver || _levelComplete)
            {
                return;
            }

            _turnNumber++;
            Phase = TurnPhase.TurnStart;
            PhaseChanged?.Invoke(Phase);

            ResolveSlowEnemyAttacksAtTurnStart();
            if (_runOver)
            {
                return;
            }

            UpdateSlowTelegraphs();

            _dice.BeginTurn();
            _currentRoll = 0;
            _diceRolledThisTurn = 0;
            Phase = TurnPhase.AwaitingRoll;
            PhaseChanged?.Invoke(Phase);
        }

        private void EndTurn()
        {
            Phase = TurnPhase.Drawing;
            PhaseChanged?.Invoke(Phase);

            if (!DrawCards(_diceRolledThisTurn, true))
            {
                return;
            }

            if (_runOver || _levelComplete)
            {
                return;
            }

            BeginTurn();
        }

        private void BuildWorld()
        {
            _grid = _map.CreateGrid();
            _pathGraph = _map.IsLinear ? new PathGraph(_map) : null;

            foreach (var coord in _openedTreasure)
            {
                if (_grid.InBounds(coord))
                {
                    _grid.SetBaseType(coord, TileType.Floor);
                }
            }

            var start = _map.hasPlayerSpawn ? _map.playerSpawn : new GridCoord(0, 0);
            if (!_grid.InBounds(start))
            {
                start = new GridCoord(0, 0);
            }

            _player.Position = start;
            _grid.Get(_player.Position).HasPlayer = true;

            _enemies.Clear();
            _nextEnemyId = 1;

            if (_map.enemySpawns == null)
            {
                return;
            }

            foreach (var spawn in _map.enemySpawns)
            {
                if (spawn.data == null || !_grid.InBounds(spawn.coord) ||
                    (_map.IsLinear && !_map.IsPath(spawn.coord)))
                {
                    continue;
                }

                var enemy = new EnemyState(_nextEnemyId++, spawn.data, spawn.coord);
                _enemies.Add(enemy);
                _grid.Get(spawn.coord).EnemyIds.Add(enemy.Id);
            }
        }

        private Hand CreateHand()
        {
            var hand = new Hand(_config.handCap);
            hand.CardDiscarded += card => CardDiscarded?.Invoke(card);
            return hand;
        }

        private DiceSet CreateDiceSet()
        {
            var dice = _config.startingDice == null
                ? Enumerable.Empty<Die>()
                : _config.startingDice.Where(data => data != null).Select(data => new Die(data.sides));

            return new DiceSet(dice);
        }

        private static List<CardInstance> CreateInstances(IEnumerable<CardData> cards)
        {
            var instances = new List<CardInstance>();
            if (cards == null)
            {
                return instances;
            }

            foreach (var card in cards)
            {
                if (card != null)
                {
                    instances.Add(new CardInstance(card));
                }
            }

            return instances;
        }

        private static List<CardData> BuildLevelDeck(IEnumerable<CardData> runDeck, MapData map)
        {
            var result = new List<CardData>();
            if (runDeck == null)
            {
                return result;
            }

            foreach (var card in runDeck)
            {
                if (card == null)
                {
                    continue;
                }

                if (map != null && map.IsLinear && !MapValidator.IsLinearAllowed(card))
                {
                    continue;
                }

                result.Add(card);
            }

            return result;
        }

        private void ResetLinearState(bool clearForfeitedExits)
        {
            _linearMovementRemaining = 0;
            _linearMovementActive = false;
            _branchDirection = null;
            _pendingExit = default;
            _hasPendingExit = false;

            if (clearForfeitedExits)
            {
                _forfeitedExits.Clear();
            }
        }

        private bool DrawCards(int count, bool triggerRetry = true)
        {
            for (var i = 0; i < count; i++)
            {
                if (!_deck.TryDraw(out var card))
                {
                    if (triggerRetry)
                    {
                        RetryLevel();
                        return false;
                    }

                    return false;
                }

                _hand.Add(card, _spent);
                CardDrawn?.Invoke(card);
            }

            return true;
        }

        private bool IsTypePlayable(CardInstance card)
        {
            if (card == null || card.Data == null || card.Data.cardType == null)
            {
                return false;
            }

            if (card.Data.cardType.typeId == CardTypeId.Normal)
            {
                return true;
            }

            return card.Data.cardType.luckyNumber == _currentRoll;
        }

        public bool CanPlayAuto(CardInstance card, out string reason)
        {
            reason = null;

            if (Phase != TurnPhase.AwaitingCard)
            {
                reason = "The session is not waiting for a card.";
                return false;
            }

            if (card == null || card.Data == null)
            {
                reason = "Invalid card.";
                return false;
            }

            if (!IsTypePlayable(card))
            {
                reason = "The card type is locked for this roll.";
                return false;
            }

            if (!IsLinearMap || !MapValidator.IsLinearAllowed(card.Data))
            {
                reason = "The card cannot be played on this map.";
                return false;
            }

            if (_pathGraph != null && _player != null &&
                _pathGraph.IsBranch(_player.Position) && !_branchDirection.HasValue)
            {
                reason = "Choose a branch first.";
                return false;
            }

            switch (card.Data.effect)
            {
                case CardEffectType.Walk:
                    if (!HasAnyLinearForwardStep())
                    {
                        reason = "There is no forward path.";
                        return false;
                    }

                    return true;
                case CardEffectType.Jump:
                    return CanLinearJump(out reason);
                case CardEffectType.Damage:
                    if (FindLinearDamageTarget(card.Data.rangeValue) == null)
                    {
                        reason = "No enemy is directly ahead within range.";
                        return false;
                    }

                    return true;
                case CardEffectType.Heal:
                    if (_player.Hp >= _player.MaxHp)
                    {
                        reason = "HP is already full.";
                        return false;
                    }

                    return true;
                case CardEffectType.Draw:
                    if (_deck.Count < Math.Max(1, card.Data.drawCount))
                    {
                        reason = "The deck cannot supply the draw.";
                        return false;
                    }

                    return true;
                default:
                    reason = "The card cannot be played on a linear map.";
                    return false;
            }
        }

        private bool HasAnyLinearForwardStep()
        {
            if (_pathGraph == null || _player == null)
            {
                return false;
            }

            return _pathGraph.GetForwardOptions(_player.Position).Count > 0;
        }

        private void ExecuteLinearEffect(CardInstance card)
        {
            switch (card.Data.effect)
            {
                case CardEffectType.Walk:
                    StartLinearWalk();
                    break;
                case CardEffectType.Jump:
                    StartLinearJump();
                    break;
                case CardEffectType.Damage:
                    var target = FindLinearDamageTarget(card.Data.rangeValue);
                    if (target != null)
                    {
                        DamageEnemy(target, card.Data.damage);
                    }
                    break;
                case CardEffectType.Heal:
                    ExecuteHeal(card.Data.heal);
                    break;
                case CardEffectType.Draw:
                    DrawCards(card.Data.drawCount);
                    break;
            }
        }

        private void StartLinearWalk()
        {
            _linearMovementActive = true;
            _linearMovementRemaining = _currentRoll;
            ContinueLinearMovement();
        }

        private bool CanLinearJump(out string reason)
        {
            reason = null;
            if (_pathGraph == null || _player == null)
            {
                reason = "No path is available.";
                return false;
            }

            if (_pathGraph.IsBranch(_player.Position) && !_branchDirection.HasValue)
            {
                reason = "Choose a branch first.";
                return false;
            }

            if (!TryGetLinearStep(_player.Position, out var first))
            {
                reason = "There is no forward path.";
                return false;
            }

            if (_pathGraph.IsBranch(first))
            {
                reason = "Jump is unavailable when the next step is a branch.";
                return false;
            }

            if (_grid.IsWall(first))
            {
                reason = "A wall blocks the jump.";
                return false;
            }

            var firstEnemy = GetEnemiesAt(first);
            if (firstEnemy.Count > 0 && firstEnemy[0].Data != null && !firstEnemy[0].Data.CanBeJumpedOver)
            {
                reason = "A flying enemy blocks the jump.";
                return false;
            }

            if (!_pathGraph.TryGetSingleForward(first, out var second))
            {
                reason = "The jump has no landing path.";
                return false;
            }

            if (!_pathGraph.IsPath(second) || _grid.IsHole(second) || _grid.IsWall(second))
            {
                reason = "The landing tile is not valid.";
                return false;
            }

            foreach (var enemy in GetEnemiesAt(second))
            {
                if (enemy.Data != null && enemy.Data.BlocksMovement)
                {
                    reason = "A blocking enemy occupies the landing tile.";
                    return false;
                }
            }

            return true;
        }

        private void StartLinearJump()
        {
            if (!CanLinearJump(out _) ||
                !TryGetLinearStep(_player.Position, out var first) ||
                !_pathGraph.TryGetSingleForward(first, out var landing))
            {
                FinishCardPlay();
                return;
            }

            _linearMovementActive = true;
            _linearMovementRemaining = 0;
            EnterTile(landing);

            if (_runOver || _levelComplete || Phase == TurnPhase.LevelRetry ||
                Phase == TurnPhase.Reward || Phase == TurnPhase.Loading)
            {
                ResetLinearState(false);
                return;
            }

            ResolveLinearStop(landing);
        }

        private void ContinueLinearMovement()
        {
            while (_linearMovementRemaining > 0)
            {
                if (_runOver || _levelComplete || Phase == TurnPhase.LevelRetry)
                {
                    ResetLinearState(false);
                    return;
                }

                if (_pathGraph.IsBranch(_player.Position) && !_branchDirection.HasValue)
                {
                    Phase = TurnPhase.AwaitingPathChoice;
                    PhaseChanged?.Invoke(Phase);
                    return;
                }

                if (!TryGetLinearStep(_player.Position, out var next))
                {
                    FinishLinearMovement();
                    return;
                }

                if (_grid.IsHole(next))
                {
                    KillOnHole();
                    return;
                }

                if (_grid.IsWall(next))
                {
                    FinishLinearMovement();
                    return;
                }

                _linearMovementRemaining--;
                EnterTile(next);

                if (_runOver || _levelComplete || Phase == TurnPhase.LevelRetry ||
                    Phase == TurnPhase.Reward || Phase == TurnPhase.Loading)
                {
                    ResetLinearState(false);
                    return;
                }

                if (_pathGraph.IsTerminalExit(next))
                {
                    ResetLinearState(false);
                    RequestExit();
                    return;
                }

                if (TryGetCurrentExit(out var exit) && !_forfeitedExits.Contains(next))
                {
                    _pendingExit = exit;
                    _hasPendingExit = true;
                    Phase = TurnPhase.AwaitingExitChoice;
                    PhaseChanged?.Invoke(Phase);
                    return;
                }

                if (_pathGraph.IsBranch(next))
                {
                    _branchDirection = null;
                    Phase = TurnPhase.AwaitingPathChoice;
                    PhaseChanged?.Invoke(Phase);
                    return;
                }
            }

            FinishLinearMovement();
        }

        private void ResolveLinearStop(GridCoord coord)
        {
            if (_pathGraph.IsTerminalExit(coord))
            {
                ResetLinearState(false);
                RequestExit();
                return;
            }

            if (TryGetCurrentExit(out var exit) && !_forfeitedExits.Contains(coord))
            {
                _pendingExit = exit;
                _hasPendingExit = true;
                Phase = TurnPhase.AwaitingExitChoice;
                PhaseChanged?.Invoke(Phase);
                return;
            }

            if (_pathGraph.IsBranch(coord))
            {
                _branchDirection = null;
                Phase = TurnPhase.AwaitingPathChoice;
                PhaseChanged?.Invoke(Phase);
                return;
            }

            FinishLinearMovement();
        }

        private void FinishLinearMovement()
        {
            _linearMovementActive = false;
            _linearMovementRemaining = 0;
            if (Phase == TurnPhase.AwaitingPathChoice || Phase == TurnPhase.AwaitingExitChoice)
            {
                Phase = TurnPhase.AwaitingCard;
            }

            FinishCardPlay();
        }

        public bool ChooseBranch(GridCoord next)
        {
            if (Phase != TurnPhase.AwaitingPathChoice || _player == null || _pathGraph == null)
            {
                return false;
            }

            if (!IsForwardOption(_player.Position, next))
            {
                return false;
            }

            _branchDirection = next;
            if (_linearMovementActive && _linearMovementRemaining > 0)
            {
                ContinueLinearMovement();
            }
            else
            {
                FinishLinearMovement();
            }

            return true;
        }

        public bool ContinuePastExit()
        {
            if (Phase != TurnPhase.AwaitingExitChoice || !_hasPendingExit || _player == null)
            {
                return false;
            }

            _forfeitedExits.Add(_player.Position);
            _hasPendingExit = false;
            _pendingExit = default;

            if (_linearMovementActive && _linearMovementRemaining > 0)
            {
                ContinueLinearMovement();
            }
            else
            {
                FinishLinearMovement();
            }

            return true;
        }

        public void RetryCurrentLevel()
        {
            RetryLevel();
        }

        public bool EnterPendingExit()
        {
            if (Phase != TurnPhase.AwaitingExitChoice || !_hasPendingExit)
            {
                return false;
            }

            var pending = _pendingExit;
            var wasActive = _linearMovementActive;
            var remaining = _linearMovementRemaining;
            _hasPendingExit = false;
            _pendingExit = default;
            _linearMovementActive = false;
            _linearMovementRemaining = 0;

            if (RequestExit())
            {
                return true;
            }

            _pendingExit = pending;
            _hasPendingExit = true;
            _linearMovementActive = wasActive;
            _linearMovementRemaining = remaining;
            Phase = TurnPhase.AwaitingExitChoice;
            return false;
        }

        private bool TryGetLinearStep(GridCoord from, out GridCoord next)
        {
            next = default;
            if (_pathGraph == null)
            {
                return false;
            }

            if (_pathGraph.IsBranch(from))
            {
                if (!_branchDirection.HasValue || !IsForwardOption(from, _branchDirection.Value))
                {
                    return false;
                }

                next = _branchDirection.Value;
                return true;
            }

            return _pathGraph.TryGetSingleForward(from, out next);
        }

        private bool IsForwardOption(GridCoord from, GridCoord next)
        {
            if (_pathGraph == null)
            {
                return false;
            }

            foreach (var option in _pathGraph.GetForwardOptions(from))
            {
                if (option == next)
                {
                    return true;
                }
            }

            return false;
        }

        private EnemyState FindLinearDamageTarget(int range)
        {
            if (_pathGraph == null || _player == null)
            {
                return null;
            }

            if (range <= 0)
            {
                var sameTile = GetEnemiesAt(_player.Position);
                return sameTile.Count > 0 ? sameTile[0] : null;
            }

            var coord = _player.Position;
            for (var step = 1; step <= range; step++)
            {
                if (!TryGetLinearStep(coord, out var next))
                {
                    return null;
                }

                coord = next;
                var enemies = GetEnemiesAt(coord);
                if (enemies.Count > 0)
                {
                    return enemies[0];
                }

                if (_pathGraph.IsBranch(coord))
                {
                    return null;
                }
            }

            return null;
        }

        private void KillOnHole()
        {
            DamagePlayer(Math.Max(1, _player.Hp), DamageSource.Other);
        }

        private bool HasAnyValidTarget(CardInstance card)
        {
            switch (card.Data.effect)
            {
                case CardEffectType.Walk:
                    return GetWalkPaths().Count > 0;
                case CardEffectType.Jump:
                    return HasAnyJumpLanding();
                case CardEffectType.Damage:
                    return HasAdjacentEnemy();
                case CardEffectType.RemoveObstacle:
                    return HasAdjacentDamaging();
                case CardEffectType.Heal:
                    return _player.Hp < _player.MaxHp;
                case CardEffectType.Draw:
                    return _deck.Count >= Math.Max(1, card.Data.drawCount);
                case CardEffectType.CrossDamage:
                    return HasEnemyInCross();
                case CardEffectType.Flood:
                    return HasFloodTarget();
                default:
                    return false;
            }
        }

        private bool HasValidTarget(CardInstance card, GridCoord target, out string reason)
        {
            reason = null;

            switch (card.Data.effect)
            {
                case CardEffectType.Walk:
                    if (!GetWalkPaths().ContainsKey(target))
                    {
                        reason = "The tile is not reachable.";
                        return false;
                    }

                    return true;

                case CardEffectType.Jump:
                    if (!IsValidJump(target))
                    {
                        reason = "The landing tile is not valid.";
                        return false;
                    }

                    return true;

                case CardEffectType.Damage:
                    if (!IsAdjacent8(_player.Position, target) || !HasEnemyAt(target))
                    {
                        reason = "The target must be an adjacent enemy.";
                        return false;
                    }

                    return true;

                case CardEffectType.RemoveObstacle:
                    if (!IsAdjacent8(_player.Position, target) || !_grid.IsDamaging(target))
                    {
                        reason = "The target must be an adjacent damaging obstacle.";
                        return false;
                    }

                    return true;

                case CardEffectType.Heal:
                    if (_player.Hp >= _player.MaxHp)
                    {
                        reason = "HP is already full.";
                        return false;
                    }

                    return true;

                case CardEffectType.Draw:
                    if (_deck.Count < Math.Max(1, card.Data.drawCount))
                    {
                        reason = "The deck cannot supply the draw.";
                        return false;
                    }

                    return true;

                case CardEffectType.CrossDamage:
                    if (!HasEnemyInCross())
                    {
                        reason = "No enemy in the cross.";
                        return false;
                    }

                    return true;

                case CardEffectType.Flood:
                    if (!HasFloodTarget())
                    {
                        reason = "No valid flood target.";
                        return false;
                    }

                    return true;

                default:
                    reason = "Unsupported card effect.";
                    return false;
            }
        }

        private void ExecuteEffect(CardInstance card, GridCoord target)
        {
            switch (card.Data.effect)
            {
                case CardEffectType.Walk:
                    ExecuteWalk(target);
                    break;
                case CardEffectType.Jump:
                    ExecuteJump(target);
                    break;
                case CardEffectType.Damage:
                    ExecuteDamage(target, card.Data.damage);
                    break;
                case CardEffectType.Heal:
                    ExecuteHeal(card.Data.heal);
                    break;
                case CardEffectType.RemoveObstacle:
                    ExecuteRemoveObstacle(target);
                    break;
                case CardEffectType.Draw:
                    DrawCards(card.Data.drawCount);
                    break;
                case CardEffectType.CrossDamage:
                    ExecuteCross(card.Data.damage);
                    break;
                case CardEffectType.Flood:
                    ExecuteFlood(card.Data.damage);
                    break;
            }
        }

        private void ExecuteWalk(GridCoord target)
        {
            var paths = GetWalkPaths();
            if (!paths.TryGetValue(target, out var path))
            {
                return;
            }

            for (var i = 1; i < path.Tiles.Count; i++)
            {
                EnterTile(path.Tiles[i]);
                if (_runOver || _levelComplete || Phase == TurnPhase.LevelRetry || Phase == TurnPhase.AwaitingRoll)
                {
                    return;
                }
            }
        }

        private void ExecuteJump(GridCoord target)
        {
            if (!IsValidJump(target))
            {
                return;
            }

            EnterTile(target);
        }

        private void ExecuteDamage(GridCoord target, int damage)
        {
            var enemies = GetEnemiesAt(target);
            if (enemies.Count > 0)
            {
                DamageEnemy(enemies[0], Math.Max(1, damage));
            }
        }

        private void ExecuteHeal(int heal)
        {
            _player.Hp = Math.Min(_player.MaxHp, _player.Hp + Math.Max(1, heal));
        }

        private void ExecuteRemoveObstacle(GridCoord target)
        {
            _grid.SetBaseType(target, TileType.Floor);
            _grid.SetSand(target, false);
        }

        private void ExecuteCross(int damage)
        {
            var coords = new List<GridCoord> { _player.Position };
            foreach (var direction in GridCoord.FourDirections)
            {
                coords.Add(_player.Position + direction);
            }

            foreach (var coord in coords)
            {
                foreach (var enemy in GetEnemiesAt(coord))
                {
                    DamageEnemy(enemy, Math.Max(1, damage));
                    if (_runOver || _levelComplete || Phase == TurnPhase.AwaitingRoll)
                    {
                        return;
                    }
                }
            }
        }

        private void ExecuteFlood(int damage)
        {
            foreach (var direction in GridCoord.EightDirections)
            {
                var coord = _player.Position + direction;
                if (!_grid.InBounds(coord) || _grid.IsWall(coord))
                {
                    continue;
                }

                _grid.SetSand(coord, true);

                foreach (var enemy in GetEnemiesAt(coord))
                {
                    if (enemy.Data != null && enemy.Data.IgnoresSand)
                    {
                        continue;
                    }

                    DamageEnemy(enemy, Math.Max(1, damage));
                    if (_runOver || _levelComplete || Phase == TurnPhase.AwaitingRoll)
                    {
                        return;
                    }
                }
            }
        }

        private void EnterTile(GridCoord coord)
        {
            if (_runOver || _levelComplete)
            {
                return;
            }

            var previous = _player.Position;
            _grid.Get(previous).HasPlayer = false;
            _player.Position = coord;
            _grid.Get(coord).HasPlayer = true;

            if (previous != coord && _pathGraph != null && _pathGraph.IsBranch(previous))
            {
                _branchDirection = null;
            }

            if (_map.TryGetTreasureAt(coord, out _))
            {
                OpenTreasure(coord);
            }

            if (_grid.IsDamaging(coord))
            {
                DamagePlayer(1, DamageSource.DamagingTile);
                if (_runOver || Phase == TurnPhase.LevelRetry || Phase == TurnPhase.AwaitingRoll)
                {
                    return;
                }
            }

            ResolveEntry(coord);
        }

        private void ResolveEntry(GridCoord coord)
        {
            for (var i = 0; i < _enemies.Count; i++)
            {
                var enemy = _enemies[i];
                if (!enemy.IsAlive || enemy.Data == null)
                {
                    continue;
                }

                var inRange = IsInRange(enemy, coord);
                if (enemy.Data.IsFast)
                {
                    if (!inRange)
                    {
                        continue;
                    }

                    DamagePlayer(enemy.Data.damage, DamageSource.Enemy);
                    EnemyAttacked?.Invoke(enemy, enemy.Data.damage, EnemyAttackTiming.Entry);
                    if (_runOver || Phase == TurnPhase.LevelRetry || Phase == TurnPhase.AwaitingRoll)
                    {
                        return;
                    }
                }
                else if (inRange)
                {
                    enemy.TelegraphTurn = _turnNumber + 1;
                }
                else
                {
                    enemy.TelegraphTurn = -1;
                }
            }
        }

        private void ResolveSlowEnemyAttacksAtTurnStart()
        {
            for (var i = 0; i < _enemies.Count; i++)
            {
                var enemy = _enemies[i];
                if (!enemy.IsAlive || enemy.Data == null || enemy.Data.IsFast)
                {
                    continue;
                }

                if (enemy.TelegraphTurn != _turnNumber)
                {
                    continue;
                }

                if (!IsInRange(enemy, _player.Position))
                {
                    enemy.TelegraphTurn = -1;
                    continue;
                }

                DamagePlayer(enemy.Data.damage, DamageSource.Enemy);
                EnemyAttacked?.Invoke(enemy, enemy.Data.damage, EnemyAttackTiming.TurnStart);
                if (_runOver || Phase == TurnPhase.LevelRetry || Phase == TurnPhase.AwaitingRoll)
                {
                    return;
                }

                enemy.TelegraphTurn = _turnNumber + 1;
            }
        }

        private void UpdateSlowTelegraphs()
        {
            foreach (var enemy in _enemies)
            {
                if (!enemy.IsAlive || enemy.Data == null || enemy.Data.IsFast)
                {
                    continue;
                }

                if (IsInRange(enemy, _player.Position))
                {
                    if (enemy.TelegraphTurn < 0)
                    {
                        enemy.TelegraphTurn = _turnNumber + 1;
                    }
                }
                else
                {
                    enemy.TelegraphTurn = -1;
                }
            }
        }

        private bool BlocksWalk(GridCoord coord)
        {
            foreach (var enemy in _enemies)
            {
                if (enemy.IsAlive && enemy.Data != null && enemy.Data.BlocksMovement && enemy.Position == coord)
                {
                    return true;
                }
            }

            return false;
        }

        private int TileCost(GridCoord coord)
        {
            var cost = _grid.IsDamaging(coord) ? 1 : 0;

            foreach (var enemy in _enemies)
            {
                if (enemy.IsAlive && enemy.Data != null && enemy.Data.IsFast && IsInRange(enemy, coord))
                {
                    cost++;
                }
            }

            return cost;
        }

        private bool HasAdjacentEnemy()
        {
            foreach (var direction in GridCoord.EightDirections)
            {
                if (HasEnemyAt(_player.Position + direction))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAdjacentDamaging()
        {
            foreach (var direction in GridCoord.EightDirections)
            {
                if (_grid.IsDamaging(_player.Position + direction))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAnyJumpLanding()
        {
            foreach (var direction in GridCoord.EightDirections)
            {
                if (IsValidJump(_player.Position + new GridCoord(direction.X * 2, direction.Y * 2)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasEnemyInCross()
        {
            if (HasEnemyAt(_player.Position))
            {
                return true;
            }

            foreach (var direction in GridCoord.FourDirections)
            {
                if (HasEnemyAt(_player.Position + direction))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasFloodTarget()
        {
            foreach (var direction in GridCoord.EightDirections)
            {
                var coord = _player.Position + direction;
                if (!_grid.InBounds(coord) || _grid.IsWall(coord))
                {
                    continue;
                }

                if (_grid.IsHole(coord) || _grid.IsDamaging(coord))
                {
                    return true;
                }

                foreach (var enemy in _enemies)
                {
                    if (enemy.IsAlive && enemy.Data != null && !enemy.Data.IgnoresSand && enemy.Position == coord)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasEnemyAt(GridCoord coord)
        {
            foreach (var enemy in _enemies)
            {
                if (enemy.IsAlive && enemy.Position == coord)
                {
                    return true;
                }
            }

            return false;
        }

        private List<EnemyState> GetEnemiesAt(GridCoord coord)
        {
            var result = new List<EnemyState>();
            foreach (var enemy in _enemies)
            {
                if (enemy.IsAlive && enemy.Position == coord)
                {
                    result.Add(enemy);
                }
            }

            return result;
        }

        private static bool IsInRange(EnemyState enemy, GridCoord coord)
        {
            if (enemy == null || enemy.Data == null)
            {
                return false;
            }

            if (enemy.Data.IsMelee)
            {
                return enemy.Position == coord;
            }

            return enemy.Position.ChebyshevDistance(coord) == 1;
        }

        private static bool IsAdjacent8(GridCoord a, GridCoord b)
        {
            var dx = Math.Abs(a.X - b.X);
            var dy = Math.Abs(a.Y - b.Y);
            return dx <= 1 && dy <= 1 && (dx != 0 || dy != 0);
        }

        private void DamagePlayer(int amount, DamageSource source)
        {
            if (amount <= 0 || _runOver)
            {
                return;
            }

            _player.Hp = Math.Max(0, _player.Hp - amount);
            PlayerDamaged?.Invoke(amount, source);

            if (_player.Hp <= 0)
            {
                RetryLevel();
            }
        }

        private void DamageEnemy(EnemyState enemy, int damage)
        {
            if (enemy == null || !enemy.IsAlive || _runOver || _levelComplete)
            {
                return;
            }

            enemy.Hp -= Math.Max(1, damage);
            if (enemy.Hp > 0)
            {
                return;
            }

            enemy.Hp = 0;
            _enemies.Remove(enemy);
            _grid.Get(enemy.Position).EnemyIds.Remove(enemy.Id);
            EnemyDied?.Invoke(enemy);

            var drawCount = enemy.Data != null && enemy.Data.bodyType == EnemyBodyType.Tank ? 2 : 1;
            DrawCards(drawCount);
        }

        private void OpenTreasure(GridCoord coord)
        {
            if (_openedTreasure.Contains(coord))
            {
                return;
            }

            if (!_map.TryGetTreasureAt(coord, out var treasure))
            {
                return;
            }

            _openedTreasure.Add(coord);
            if (_grid.InBounds(coord))
            {
                _grid.SetBaseType(coord, TileType.Floor);
            }

            var gold = treasure.data != null ? treasure.data.gold : treasure.gold;
            Gold += gold;

            if (treasure.data != null && treasure.data.heal > 0 && _player != null)
            {
                _player.Hp = Math.Min(_player.MaxHp, _player.Hp + treasure.data.heal);
            }

            var added = new List<CardInstance>();
            var cardData = treasure.data != null
                ? treasure.data.Draw(_random)
                : new List<CardData>(treasure.cards ?? Array.Empty<CardData>());

            foreach (var data in cardData)
            {
                if (data == null)
                {
                    continue;
                }

                if (_map.IsLinear && !MapValidator.IsLinearAllowed(data))
                {
                    continue;
                }

                var card = new CardInstance(data);
                _deck.Add(card);
                _levelStartDeckOrder.Add(data);
                _runDeckOrder.Add(data);
                added.Add(card);
            }

            TreasureOpened?.Invoke(coord, added, gold);
        }

        private void CompleteLevel()
        {
            if (_levelComplete || _runOver)
            {
                return;
            }

            _levelComplete = true;
            _torch?.Pause();
            Phase = TurnPhase.LevelComplete;
            PhaseChanged?.Invoke(Phase);
            LevelCompleted?.Invoke();
        }

        private void RetryLevel()
        {
            if (_retryInProgress)
            {
                return;
            }

            _retryInProgress = true;
            ResetLinearState(true);
            Phase = TurnPhase.LevelRetry;
            PhaseChanged?.Invoke(Phase);
            LevelRetried?.Invoke();

            _torch?.Pause();
            _runOver = false;
            _player.Hp = Math.Max(1, _entryHp);
            BuildWorld();

            _deck = new Deck(CreateInstances(_levelStartDeckOrder));
            _deck.Shuffle(_random);
            _hand = CreateHand();
            _spent = new SpentPile();
            _dice = CreateDiceSet();
            _turnNumber = 0;
            _currentRoll = 0;
            _freeRerollsRemaining = Math.Max(0, _config.freeRerollsPerMap);
            _diceRolledThisTurn = 0;
            _levelComplete = false;

            _torch?.Reset(_config.torchSeconds);
            _torch?.Start();

            DrawCards(_config.handSize, false);
            BeginTurn();
            _retryInProgress = false;
        }

        public bool TryGetCurrentExit(out ExitSpawn exit)
        {
            if (_map == null || _player == null)
            {
                exit = default;
                return false;
            }

            if (_forfeitedExits.Contains(_player.Position))
            {
                exit = default;
                return false;
            }

            return _map.TryGetExitAt(_player.Position, out exit);
        }

        public bool RequestExit()
        {
            if (_transitionInProgress || Phase == TurnPhase.Reward || Phase == TurnPhase.Loading ||
                Phase == TurnPhase.LevelRetry || _rewardSelection != null)
            {
                return false;
            }

            if (!TryGetCurrentExit(out var exit))
            {
                return false;
            }

            var destination = exit.destination.ResolveMap();
            if (destination == null)
            {
                UnityEngine.Debug.LogWarning("Exit has no valid destination map.");
                return false;
            }

            ResetLinearState(false);
            _pendingDestination = destination;
            _pendingTheme = exit.destination.ResolveTheme();
            _rewardSelection = new RewardSelection(_map.rewardPool, _random);
            _player.Hp = Math.Min(_player.MaxHp, _player.Hp + 1);
            _torch?.Pause();
            Phase = TurnPhase.Reward;
            PhaseChanged?.Invoke(Phase);
            return true;
        }

        public bool RerollReward(int index)
        {
            if (_rewardSelection == null)
            {
                return false;
            }

            var changed = _rewardSelection.Reroll(index);
            if (changed)
            {
                PhaseChanged?.Invoke(Phase);
            }

            return changed;
        }

        public bool ConfirmReward(int index)
        {
            if (_rewardSelection == null || Phase != TurnPhase.Reward)
            {
                return false;
            }

            if (!_rewardSelection.TryChoose(index, out var tuple))
            {
                return false;
            }

            foreach (var card in tuple.ToArray())
            {
                if (card == null || (_map.IsLinear && !MapValidator.IsLinearAllowed(card)))
                {
                    continue;
                }

                _runDeckOrder.Add(card);
                _levelStartDeckOrder.Add(card);
            }

            _rewardSelection = null;
            Phase = TurnPhase.Loading;
            PhaseChanged?.Invoke(Phase);
            return true;
        }

        public bool CompleteTransition()
        {
            if (Phase != TurnPhase.Loading || _pendingDestination == null)
            {
                return false;
            }

            _transitionInProgress = true;
            _map = _pendingDestination;
            _pendingDestination = null;
            _pendingTheme = null;
            _levelNumber = Math.Max(1, _levelNumber + 1);
            _openedTreasure.Clear();
            _levelComplete = false;
            _runOver = false;
            _retryInProgress = false;
            ResetLinearState(true);

            BuildWorld();
            _levelStartDeckOrder = BuildLevelDeck(_runDeckOrder, _map);

            _deck = new Deck(CreateInstances(_levelStartDeckOrder));
            _deck.Shuffle(_random);
            _hand = CreateHand();
            _spent = new SpentPile();
            _dice = CreateDiceSet();
            _turnNumber = 0;
            _currentRoll = 0;
            _diceRolledThisTurn = 0;
            _freeRerollsRemaining = Math.Max(0, _config.freeRerollsPerMap);
            _entryHp = _player.Hp;

            if (_torch == null)
            {
                _torch = new TorchTimer(_config.torchSeconds);
            }
            else
            {
                _torch.Reset(_config.torchSeconds);
            }

            _torch.Burnout -= OnTorchBurnout;
            _torch.Burnout += OnTorchBurnout;
            _torch.Start();

            DrawCards(_config.handSize, false);
            BeginTurn();
            _transitionInProgress = false;
            LevelTransitioned?.Invoke(_map);
            return true;
        }

        private void OnTorchBurnout()
        {
            if (_runOver || _levelComplete)
            {
                return;
            }

            RetryLevel();
        }
    }
}
