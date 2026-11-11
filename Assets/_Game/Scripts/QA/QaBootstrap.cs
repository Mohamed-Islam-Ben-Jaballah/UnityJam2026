using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DiceDrive.Cards;
using DiceDrive.Core;
using DiceDrive.Entities;
using DiceDrive.Grid;
using DiceDrive.Turn;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace DiceDrive.QA
{
    public sealed class QaBootstrap : MonoBehaviour
    {
        public RunConfig runConfig;
        public MapData mapData;
        public int seed = 12345;
        public float tileSize = 1f;
        public float dragLineStartOffset = 24f;
        public int levelNumber = 1;

        private GameSession _session;
        private Camera _camera;
        private Transform _worldRoot;
        private Canvas _canvas;
        private TextMeshProUGUI _statusText;
        private TextMeshProUGUI _messageText;
        private TextMeshProUGUI _diceText;
        private Button _rollButton;
        private Button _rerollButton;
        private Button _restartButton;
        private RectTransform _handPanel;
        private Font _font;
        private TMP_FontAsset _tmpFont;
        private QaTargetingController _targeting;
        private readonly List<GameObject> _cardObjects = new List<GameObject>();
        private readonly List<GameObject> _rewardRows = new List<GameObject>();
        private Button _takeExitButton;
        private GameObject _rewardPanel;
        private GameObject _loadingPanel;
        private RectTransform _loadingIcon;
        private Coroutine _loadingRoutine;
        private GameObject _choicePanel;
        private RectTransform _choiceContent;
        private TextMeshProUGUI _choiceTitle;
        private readonly List<GameObject> _choiceButtons = new List<GameObject>();

        public GameSession Session => _session;
        public QaTargetingController Targeting => _targeting;
        public bool IsLinearMap => _session != null && _session.IsLinearMap;

        private void Start()
        {
            if (runConfig == null || mapData == null)
            {
                Debug.LogError("QA bootstrap needs a RunConfig and a MapData asset.");
                enabled = false;
                return;
            }

            _font = LoadFont();
            _tmpFont = LoadTmpFont();
            CreateCamera();
            _worldRoot = new GameObject("World").transform;
            _worldRoot.SetParent(transform, false);
            CreateUI();
            CreateTargeting();
            CreateSession();
        }

        private void Update()
        {
            if (_session != null)
            {
                _session.TickTorch(Time.unscaledDeltaTime);
                RefreshStatus();
            }

            if (_loadingPanel != null && _loadingPanel.activeSelf && _loadingIcon != null)
            {
                _loadingIcon.Rotate(0f, 0f, -180f * Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void CreateSession()
        {
            Unsubscribe();
            _session = new GameSession(runConfig, mapData, new System.Random(seed));
            Subscribe();
            _session.StartLevel(null, levelNumber);
            _targeting?.SetSession(_session);
            UpdateCamera();
            RefreshAll();
        }

        private void Subscribe()
        {
            _session.PhaseChanged += OnPhaseChanged;
            _session.DiceRolled += OnDiceRolled;
            _session.CardDrawn += OnCardDrawn;
            _session.CardDiscarded += OnCardDiscarded;
            _session.CardPlayed += OnCardPlayed;
            _session.EnemyAttacked += OnEnemyAttacked;
            _session.EnemyDied += OnEnemyDied;
            _session.PlayerDamaged += OnPlayerDamaged;
            _session.TreasureOpened += OnTreasureOpened;
            _session.LevelRetried += OnLevelRetried;
            _session.LevelCompleted += OnLevelCompleted;
            _session.RunEnded += OnRunEnded;
            _session.LevelTransitioned += OnLevelTransitioned;
        }

        private void Unsubscribe()
        {
            if (_session == null)
            {
                return;
            }

            _session.PhaseChanged -= OnPhaseChanged;
            _session.DiceRolled -= OnDiceRolled;
            _session.CardDrawn -= OnCardDrawn;
            _session.CardDiscarded -= OnCardDiscarded;
            _session.CardPlayed -= OnCardPlayed;
            _session.EnemyAttacked -= OnEnemyAttacked;
            _session.EnemyDied -= OnEnemyDied;
            _session.PlayerDamaged -= OnPlayerDamaged;
            _session.TreasureOpened -= OnTreasureOpened;
            _session.LevelRetried -= OnLevelRetried;
            _session.LevelCompleted -= OnLevelCompleted;
            _session.RunEnded -= OnRunEnded;
            _session.LevelTransitioned -= OnLevelTransitioned;
        }

        private void OnPhaseChanged(TurnPhase phase)
        {
            if (phase != TurnPhase.AwaitingCard)
            {
                _targeting?.CancelDrag();
            }

            if (phase == TurnPhase.Reward)
            {
                ShowReward();
            }
            else
            {
                HideReward();
            }

            if (phase == TurnPhase.Loading)
            {
                ShowLoading();
            }
            else
            {
                HideLoading();
            }

            RefreshChoicePanel();
            RefreshHand();
            RefreshDice();
            RefreshStatus();
        }

        private void OnLevelTransitioned(MapData map)
        {
            _targeting?.SetSession(_session);
            UpdateCamera();
            RefreshAll();
        }

        private void OnDiceRolled(int roll)
        {
            RefreshHand();
            RefreshDice();
        }

        private void OnCardDrawn(CardInstance card)
        {
            RefreshHand();
        }

        private void OnCardDiscarded(CardInstance card)
        {
            RefreshHand();
        }

        private void OnCardPlayed(CardInstance card, GridCoord target)
        {
            RefreshWorld();
            RefreshHand();
            RefreshDice();
            RefreshStatus();
        }

        private void OnEnemyAttacked(EnemyState enemy, int damage, EnemyAttackTiming timing)
        {
            RefreshStatus();
        }

        private void OnEnemyDied(EnemyState enemy)
        {
            RefreshWorld();
        }

        private void OnPlayerDamaged(int amount, DamageSource source)
        {
            RefreshWorld();
            RefreshStatus();
        }

        private void OnTreasureOpened(GridCoord coord, IReadOnlyList<CardInstance> cards, int gold)
        {
            RefreshWorld();
            RefreshHand();
            RefreshStatus();
        }

        private void OnLevelRetried()
        {
            HideReward();
            HideLoading();
            RefreshAll();
        }

        private void OnLevelCompleted()
        {
            RefreshStatus();
        }

        private void OnRunEnded()
        {
            RefreshStatus();
        }

        private void OnTakeExit()
        {
            if (_session != null && _session.RequestExit())
            {
                RefreshStatus();
            }
        }

        private void ShowReward()
        {
            if (_rewardPanel == null)
            {
                CreateRewardPanel();
            }

            _rewardPanel.SetActive(true);
            RebuildRewardOptions();
        }

        private void HideReward()
        {
            if (_rewardPanel != null)
            {
                _rewardPanel.SetActive(false);
            }
        }

        private void CreateChoicePanel()
        {
            var panel = CreatePanel(_canvas.transform, "Choice Panel", new Color(0f, 0f, 0f, 0.72f));
            _choicePanel = panel.gameObject;
            var rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620f, 300f);

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 20);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _choiceTitle = CreateText(panel.transform, "Choice Title", "Choose", 34, TextAlignmentOptions.Center, Color.white);
            var titleLayout = _choiceTitle.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 56f;

            var content = new GameObject("Choice Content", typeof(RectTransform));
            content.transform.SetParent(panel.transform, false);
            _choiceContent = content.GetComponent<RectTransform>();
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8f;
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.AddComponent<LayoutElement>().preferredHeight = 170f;

            _choicePanel.SetActive(false);
        }

        private void RefreshChoicePanel()
        {
            if (_session == null)
            {
                HideChoicePanel();
                return;
            }

            if (_session.Phase != TurnPhase.AwaitingPathChoice &&
                _session.Phase != TurnPhase.AwaitingExitChoice)
            {
                HideChoicePanel();
                return;
            }

            if (_choicePanel == null)
            {
                CreateChoicePanel();
            }

            foreach (var button in _choiceButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }

            _choiceButtons.Clear();
            _choicePanel.SetActive(true);

            if (_session.Phase == TurnPhase.AwaitingPathChoice)
            {
                _choiceTitle.text = "Choose a path";
                if (_session.BranchOptions.Count == 0)
                {
                    var retry = CreateButton(_choiceContent, "Retry Level", () =>
                    {
                        _session?.RetryCurrentLevel();
                        RefreshAll();
                    });
                    retry.GetComponent<LayoutElement>().preferredHeight = 42f;
                    _choiceButtons.Add(retry.gameObject);
                    return;
                }

                foreach (var option in _session.BranchOptions)
                {
                    var coord = option;
                    var label = ArrowFor(_session.Player.Position, coord);
                    var button = CreateButton(_choiceContent, label, () =>
                    {
                        if (_session != null && _session.ChooseBranch(coord))
                        {
                            RefreshAll();
                        }
                    });
                    button.GetComponent<LayoutElement>().preferredHeight = 42f;
                    _choiceButtons.Add(button.gameObject);
                }

                return;
            }

            _choiceTitle.text = "Exit reached";
            var continueButton = CreateButton(_choiceContent, "Continue", () =>
            {
                if (_session != null && _session.ContinuePastExit())
                {
                    RefreshAll();
                }
            });
            continueButton.GetComponent<LayoutElement>().preferredHeight = 42f;
            _choiceButtons.Add(continueButton.gameObject);

            var enterButton = CreateButton(_choiceContent, "Enter Exit", () =>
            {
                if (_session != null && _session.EnterPendingExit())
                {
                    RefreshAll();
                }
            });
            enterButton.GetComponent<LayoutElement>().preferredHeight = 42f;
            _choiceButtons.Add(enterButton.gameObject);
        }

        private void HideChoicePanel()
        {
            if (_choicePanel != null)
            {
                _choicePanel.SetActive(false);
            }
        }

        private static string ArrowFor(GridCoord from, GridCoord to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            if (dx > 0)
            {
                return "\u2192";
            }

            if (dx < 0)
            {
                return "\u2190";
            }

            if (dy > 0)
            {
                return "\u2191";
            }

            return "\u2193";
        }

        private void CreateRewardPanel()
        {
            var panel = CreatePanel(_canvas.transform, "Reward Panel", new Color(0f, 0f, 0f, 0.92f));
            _rewardPanel = panel.gameObject;
            var rect = panel.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(220, 220, 130, 130);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = CreateText(panel.transform, "Reward Title", "Choose a reward", 42, TextAlignmentOptions.Center, Color.white);
            var titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 64f;

            _rewardPanel.SetActive(false);
        }

        private void RebuildRewardOptions()
        {
            foreach (var row in _rewardRows)
            {
                if (row != null)
                {
                    Destroy(row);
                }
            }

            _rewardRows.Clear();

            if (_session == null || _session.RewardSelection == null || _rewardPanel == null)
            {
                return;
            }

            for (var i = 0; i < _session.RewardSelection.Count; i++)
            {
                var option = _session.RewardSelection.GetOption(i);
                var rowImage = CreatePanel(_rewardPanel.transform, $"Option {i + 1}", new Color(0.12f, 0.14f, 0.18f, 1f));
                var row = rowImage.gameObject;
                var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.padding = new RectOffset(18, 18, 8, 8);
                rowLayout.spacing = 12f;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = false;

                var rowElement = row.AddComponent<LayoutElement>();
                rowElement.preferredHeight = 92f;
                rowElement.flexibleWidth = 1f;

                var label = CreateText(
                    row.transform,
                    "Cards",
                    option.a != null ? option.Label() : "No option",
                    28,
                    TextAlignmentOptions.Left,
                    Color.white);
                var labelLayout = label.gameObject.AddComponent<LayoutElement>();
                labelLayout.preferredHeight = 60f;
                labelLayout.flexibleWidth = 1f;

                var choice = i;
                var choose = CreateButton(row.transform, "Choose", () =>
                {
                    if (_session != null && _session.ConfirmReward(choice))
                    {
                        HideReward();
                        RefreshStatus();
                    }
                });
                var chooseLayout = choose.GetComponent<LayoutElement>();
                chooseLayout.preferredWidth = 140f;
                chooseLayout.preferredHeight = 40f;

                var reroll = CreateButton(row.transform, "Reroll", () =>
                {
                    if (_session != null)
                    {
                        _session.RerollReward(choice);
                        RebuildRewardOptions();
                    }
                });
                var rerollLayout = reroll.GetComponent<LayoutElement>();
                rerollLayout.preferredWidth = 140f;
                rerollLayout.preferredHeight = 40f;
                reroll.interactable = _session.RewardSelection.CanReroll(choice);

                _rewardRows.Add(row);
            }
        }

        private void ShowLoading()
        {
            if (_loadingPanel == null)
            {
                CreateLoadingPanel();
            }

            _loadingPanel.SetActive(true);
            if (_loadingRoutine == null)
            {
                _loadingRoutine = StartCoroutine(LoadRoutine());
            }
        }

        private void HideLoading()
        {
            if (_loadingPanel != null)
            {
                _loadingPanel.SetActive(false);
            }
        }

        private void CreateLoadingPanel()
        {
            var panel = CreatePanel(_canvas.transform, "Loading Panel", new Color(0f, 0f, 0f, 0.78f));
            _loadingPanel = panel.gameObject;
            var rect = panel.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = CreateText(panel.transform, "Loading Text", "Loading", 42, TextAlignmentOptions.Center, Color.white);
            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0f, 0f);
            textRect.sizeDelta = new Vector2(600f, 80f);

            var iconGo = new GameObject("Loading Icon", typeof(RectTransform));
            iconGo.transform.SetParent(panel.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = ShapeSpriteFactory.Square;
            icon.color = new Color(1f, 0.8f, 0.2f);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 90f);
            iconRect.sizeDelta = new Vector2(48f, 48f);
            _loadingIcon = iconRect;

            _loadingPanel.SetActive(false);
        }

        private IEnumerator LoadRoutine()
        {
            yield return new WaitForSecondsRealtime(1f);
            if (_session != null)
            {
                _session.CompleteTransition();
            }

            HideLoading();
            _loadingRoutine = null;
            RefreshAll();
        }

        private void RefreshAll()
        {
            _targeting?.CancelDrag();
            RefreshWorld();
            RefreshHand();
            RefreshDice();
            RefreshStatus();
            RefreshChoicePanel();
        }

        private void RefreshWorld()
        {
            if (_session == null || _worldRoot == null)
            {
                return;
            }

            foreach (Transform child in _worldRoot)
            {
                Destroy(child.gameObject);
            }

            for (var y = 0; y < _session.Grid.Height; y++)
            {
                for (var x = 0; x < _session.Grid.Width; x++)
                {
                    var coord = new GridCoord(x, y);
                    if (_session.IsLinearMap && (_session.PathGraph == null || !_session.PathGraph.IsPath(coord)))
                    {
                        continue;
                    }

                    var tile = _session.Grid.Get(coord);
                    var type = tile.EffectiveType;
                    var tileColor = _session.IsLinearMap && type == TileType.Floor
                        ? new Color(0.16f, 0.28f, 0.22f)
                        : TileColor(type);
                    CreateSprite($"Tile {x},{y}", ShapeSpriteFactory.Square, WorldOf(coord), tileColor, 0, tileSize * 0.95f);

                    if (type == TileType.Hole || type == TileType.Damaging || type == TileType.Wall)
                    {
                        var obstacleColor = type == TileType.Hole
                            ? new Color(0.1f, 0.2f, 0.5f)
                            : type == TileType.Damaging
                                ? new Color(0.95f, 0.45f, 0.1f)
                                : new Color(0.55f, 0.55f, 0.6f);
                        CreateSprite($"Obstacle {type} {x},{y}", ShapeSpriteFactory.Hexagon, WorldOf(coord), obstacleColor, 3, tileSize * 0.8f);
                        CreateLabel(type == TileType.Damaging ? "Spike" : type.ToString(), WorldOf(coord) + new Vector3(0f, tileSize * 0.62f, 0f), Color.white, 26);
                    }
                    else if (type == TileType.Exit || type == TileType.Treasure)
                    {
                        CreateLabel(type.ToString(), WorldOf(coord) + new Vector3(0f, tileSize * 0.62f, 0f), Color.white, 26);
                    }
                }
            }

            if (_session.MapData.exits != null)
            {
                for (var i = 0; i < _session.MapData.exits.Length; i++)
                {
                    var exit = _session.MapData.exits[i];
                    CreateSprite($"Exit {i + 1}", ShapeSpriteFactory.Square, WorldOf(exit.coord), new Color(0.2f, 0.9f, 0.3f), 2, tileSize * 0.85f);
                    CreateLabel($"Exit {i + 1}", WorldOf(exit.coord) + new Vector3(0f, tileSize * 0.62f, 0f), Color.white, 26);
                }
            }

            if (_session.MapData.branchTiles != null)
            {
                foreach (var branch in _session.MapData.branchTiles)
                {
                    CreateSprite($"Branch {branch.X},{branch.Y}", ShapeSpriteFactory.Hexagon, WorldOf(branch), new Color(0.95f, 0.55f, 0.1f), 3, tileSize * 0.65f);
                    CreateLabel("B", WorldOf(branch) + new Vector3(0f, tileSize * 0.62f, 0f), Color.white, 24);

                    if (_session.PathGraph != null)
                    {
                        foreach (var option in _session.PathGraph.GetForwardOptions(branch))
                        {
                            CreateLabel(ArrowFor(branch, option), WorldOf(option) + new Vector3(0f, -tileSize * 0.55f, 0f), new Color(1f, 0.8f, 0.3f), 26);
                        }
                    }
                }
            }

            if (_session.MapData.treasureSpawns != null)
            {
                foreach (var spawn in _session.MapData.treasureSpawns)
                {
                    if (_session.OpenedTreasure.Contains(spawn.coord))
                    {
                        continue;
                    }

                    CreateSprite("Treasure", ShapeSpriteFactory.Square, WorldOf(spawn.coord), new Color(0.9f, 0.75f, 0.2f), 2, tileSize * 0.8f);
                    var code = spawn.data != null && !string.IsNullOrEmpty(spawn.data.shortCode)
                        ? spawn.data.shortCode
                        : "T";
                    CreateLabel(code, WorldOf(spawn.coord) + new Vector3(0f, tileSize * 0.62f, 0f), Color.white, 26);
                }
            }

            foreach (var enemy in _session.Enemies)
            {
                if (!enemy.IsAlive || enemy.Data == null)
                {
                    continue;
                }

                if (!_session.IsInEnemyRange(enemy, _session.Player.Position))
                {
                    continue;
                }

                var rangeColor = enemy.Data.IsFast
                    ? new Color(1f, 0.2f, 0.2f, 0.22f)
                    : new Color(1f, 0.8f, 0.2f, 0.22f);

                foreach (var coord in _session.GetEnemyRangeTiles(enemy))
                {
                    CreateSprite(
                        $"Enemy Range {enemy.Id} {coord.X},{coord.Y}",
                        ShapeSpriteFactory.Square,
                        WorldOf(coord),
                        rangeColor,
                        1,
                        tileSize * 0.95f);
                }
            }

            CreateSprite("Player", ShapeSpriteFactory.Circle, WorldOf(_session.Player.Position), new Color(0.2f, 0.9f, 1f), 5, tileSize * 0.7f);
            CreateLabel("Player", WorldOf(_session.Player.Position) + new Vector3(0f, tileSize * 0.7f, 0f), Color.white, 28);

            foreach (var enemy in _session.Enemies)
            {
                if (!enemy.IsAlive || enemy.Data == null)
                {
                    continue;
                }

                var color = enemy.Data.bodyType == EnemyBodyType.Tank
                    ? new Color(0.85f, 0.25f, 0.85f)
                    : enemy.Data.bodyType == EnemyBodyType.Flying
                        ? new Color(0.25f, 0.55f, 1f)
                        : new Color(0.9f, 0.25f, 0.2f);

                if (enemy.Data.IsFast)
                {
                    color = Color.Lerp(color, Color.white, 0.25f);
                }

                CreateSprite($"Enemy {enemy.Id}", ShapeSpriteFactory.Triangle, WorldOf(enemy.Position), color, 4, tileSize * 0.7f);
                var speed = enemy.Data.IsFast ? "F" : "S";
                var attack = enemy.Data.IsMelee ? "M" : "R";
                var code = string.IsNullOrEmpty(enemy.Data.shortCode) ? enemy.Data.enemyName : enemy.Data.shortCode;
                CreateLabel($"{code}\n{speed}/{attack}", WorldOf(enemy.Position) + new Vector3(0f, tileSize * 0.75f, 0f), Color.white, 24);
            }
        }

        private void RefreshHand()
        {
            if (_handPanel == null)
            {
                return;
            }

            foreach (var cardObject in _cardObjects)
            {
                if (cardObject != null)
                {
                    Destroy(cardObject);
                }
            }

            _cardObjects.Clear();

            if (_session == null)
            {
                return;
            }

            foreach (var card in _session.PlayerHand.Cards)
            {
                var go = new GameObject($"Card {card.Id}", typeof(RectTransform));
                go.transform.SetParent(_handPanel, false);

                var image = go.AddComponent<Image>();
                image.raycastTarget = true;

                var layout = go.AddComponent<LayoutElement>();
                layout.preferredWidth = 150f;
                layout.preferredHeight = 90f;

                var label = CreateText(go.transform, "Label", string.Empty, 22, TextAlignmentOptions.Center, Color.white);
                var rect = label.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(4f, 4f);
                rect.offsetMax = new Vector2(-4f, -4f);

                var view = go.AddComponent<QaCardView>();
                view.Initialize(this, card, image, label);
                _cardObjects.Add(go);
            }
        }

        private void RefreshDice()
        {
            if (_diceText != null && _session != null)
            {
                _diceText.text =
                    $"Dice: {_session.Dice.Count}   Roll: {_session.CurrentRoll}   Free rerolls: {_session.FreeRerollsRemaining}\n" +
                    $"Hand: {_session.PlayerHand.Count}/{_session.PlayerHand.Cap}   Deck: {_session.DrawPile.Count}";
            }

            if (_rollButton != null)
            {
                _rollButton.interactable = _session != null && _session.Phase == TurnPhase.AwaitingRoll;
            }

            if (_rerollButton != null)
            {
                _rerollButton.interactable = _session != null &&
                                             _session.Phase == TurnPhase.AwaitingCard &&
                                             !_session.HasPlayableCard();
            }

            if (_takeExitButton != null)
            {
                _takeExitButton.interactable = _session != null &&
                                               !_session.IsLinearMap &&
                                               _session.IsOnExit &&
                                               _session.Phase != TurnPhase.Reward &&
                                               _session.Phase != TurnPhase.Loading &&
                                               _session.Phase != TurnPhase.LevelRetry;
            }
        }

        private void RefreshStatus()
        {
            if (_statusText != null && _session != null)
            {
                var torch = _session.Torch != null ? _session.Torch.Remaining : 0f;
                _statusText.text =
                    $"HP {_session.Player.Hp}/{_session.Player.MaxHp}   Gold {_session.Gold}   Turn {_session.TurnNumber}   " +
                    $"Level {_session.LevelNumber}   Phase {_session.Phase}   Torch {torch:0.0}s";
            }

            if (_messageText == null)
            {
                return;
            }

            if (_session == null)
            {
                _messageText.text = string.Empty;
            }
            else if (_session.IsRunOver)
            {
                _messageText.text = "RUN OVER";
            }
            else if (_session.IsLevelComplete)
            {
                _messageText.text = "LEVEL COMPLETE";
            }
            else if (_session.Phase == TurnPhase.LevelRetry)
            {
                _messageText.text = "LEVEL RETRY";
            }
            else if (_session.Phase == TurnPhase.Reward)
            {
                _messageText.text = "CHOOSE A REWARD";
            }
            else if (_session.Phase == TurnPhase.Loading)
            {
                _messageText.text = "LOADING";
            }
            else if (_session.Phase == TurnPhase.AwaitingPathChoice)
            {
                _messageText.text = "CHOOSE A PATH";
            }
            else if (_session.Phase == TurnPhase.AwaitingExitChoice)
            {
                _messageText.text = "EXIT: CONTINUE OR ENTER";
            }
            else
            {
                _messageText.text = string.Empty;
            }
        }

        public bool IsCardPlayable(CardInstance card)
        {
            if (_session == null || card == null || _session.Phase != TurnPhase.AwaitingCard)
            {
                return false;
            }

            foreach (var playable in _session.GetPlayableCards())
            {
                if (playable == card)
                {
                    return true;
                }
            }

            return false;
        }

        public void PlayCardAuto(CardInstance card)
        {
            if (_session == null || !_session.IsLinearMap)
            {
                return;
            }

            if (_session.PlayCardAuto(card).Succeeded)
            {
                RefreshAll();
            }
            else
            {
                RefreshHand();
                RefreshStatus();
            }
        }

        private void CreateTargeting()
        {
            var go = new GameObject("Targeting");
            go.transform.SetParent(transform, false);
            _targeting = go.AddComponent<QaTargetingController>();
            _targeting.Initialize(_camera, tileSize);
        }

        private void CreateCamera()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                var go = new GameObject("QA Camera");
                go.transform.SetParent(transform, false);
                go.tag = "MainCamera";
                _camera = go.AddComponent<Camera>();
            }

            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            _camera.transform.rotation = Quaternion.identity;
            UpdateCamera();
        }

        private void UpdateCamera()
        {
            var data = _session != null ? _session.MapData : mapData;
            if (_camera == null || data == null)
            {
                return;
            }

            if (_session != null && _session.IsLinearMap && _session.PathGraph != null &&
                _session.PathGraph.TryGetBounds(out var minX, out var maxX, out var minY, out var maxY))
            {
                _camera.transform.position = new Vector3(
                    (minX + maxX) * tileSize * 0.5f,
                    (minY + maxY) * tileSize * 0.5f + 0.5f,
                    -10f);
                _camera.orthographicSize = Mathf.Max(maxX - minX + 2, maxY - minY + 2) * 0.5f + 1.5f;
                return;
            }

            _camera.transform.position = new Vector3(
                (data.width - 1) * tileSize * 0.5f,
                (data.height - 1) * tileSize * 0.5f + 0.5f,
                -10f);
            _camera.orthographicSize = Mathf.Max(data.width, data.height) * 0.5f + 1.5f;
        }

        private void CreateUI()
        {
            var canvasGo = new GameObject("QA Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            if (EventSystem.current == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.transform.SetParent(transform, false);
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<InputSystemUIInputModule>();
            }

            _statusText = CreateText(canvasGo.transform, "Status", string.Empty, 24, TextAlignmentOptions.TopLeft, Color.white);
            var statusRect = _statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0f, 1f);
            statusRect.anchoredPosition = new Vector2(16f, -16f);
            statusRect.sizeDelta = new Vector2(-32f, 80f);

            _messageText = CreateText(canvasGo.transform, "Message", string.Empty, 56, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.3f));
            var messageRect = _messageText.rectTransform;
            messageRect.anchorMin = new Vector2(0.5f, 0.5f);
            messageRect.anchorMax = new Vector2(0.5f, 0.5f);
            messageRect.pivot = new Vector2(0.5f, 0.5f);
            messageRect.anchoredPosition = new Vector2(0f, 120f);
            messageRect.sizeDelta = new Vector2(900f, 160f);

            var bottom = CreatePanel(canvasGo.transform, "Bottom Panel", new Color(0f, 0f, 0f, 0.75f));
            var bottomRect = bottom.rectTransform;
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0f);
            bottomRect.pivot = new Vector2(0.5f, 0f);
            bottomRect.anchoredPosition = Vector2.zero;
            bottomRect.sizeDelta = new Vector2(0f, 220f);

            var hand = CreatePanel(bottom.transform, "Hand Panel", new Color(0f, 0f, 0f, 0f));
            _handPanel = hand.rectTransform;
            _handPanel.anchorMin = new Vector2(0f, 0f);
            _handPanel.anchorMax = new Vector2(0.68f, 1f);
            _handPanel.offsetMin = new Vector2(12f, 12f);
            _handPanel.offsetMax = new Vector2(-6f, -12f);

            var handLayout = hand.gameObject.AddComponent<HorizontalLayoutGroup>();
            handLayout.spacing = 10f;
            handLayout.padding = new RectOffset(0, 0, 0, 0);
            handLayout.childAlignment = TextAnchor.MiddleLeft;
            handLayout.childControlWidth = false;
            handLayout.childControlHeight = false;
            handLayout.childForceExpandWidth = false;
            handLayout.childForceExpandHeight = false;

            var dice = CreatePanel(bottom.transform, "Dice Panel", new Color(0.08f, 0.09f, 0.12f, 0.95f));
            var diceRect = dice.rectTransform;
            diceRect.anchorMin = new Vector2(0.68f, 0f);
            diceRect.anchorMax = new Vector2(1f, 1f);
            diceRect.offsetMin = new Vector2(6f, 12f);
            diceRect.offsetMax = new Vector2(-12f, -12f);

            var diceLayout = dice.gameObject.AddComponent<VerticalLayoutGroup>();
            diceLayout.spacing = 4f;
            diceLayout.padding = new RectOffset(6, 6, 6, 6);
            diceLayout.childAlignment = TextAnchor.UpperLeft;
            diceLayout.childControlWidth = true;
            diceLayout.childControlHeight = false;
            diceLayout.childForceExpandWidth = true;
            diceLayout.childForceExpandHeight = false;

            _diceText = CreateText(dice.transform, "Dice Text", string.Empty, 20, TextAlignmentOptions.TopLeft, Color.white);
            var diceTextLayout = _diceText.gameObject.AddComponent<LayoutElement>();
            diceTextLayout.preferredHeight = 56f;

            _rollButton = CreateButton(dice.transform, "Roll Die", () =>
            {
                _session?.RollCurrentDie();
                RefreshDice();
                RefreshHand();
            });

            _rerollButton = CreateButton(dice.transform, "Reroll / Skip", () =>
            {
                _session?.Reroll();
                RefreshDice();
                RefreshHand();
            });

            _restartButton = CreateButton(dice.transform, "Restart QA", CreateSession);
            _takeExitButton = CreateButton(dice.transform, "Take Exit", OnTakeExit);
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string content, int size, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (_tmpFont != null)
            {
                text.font = _tmpFont;
            }

            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.22f, 0.24f, 0.3f, 1f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 32f;

            var text = CreateText(go.transform, "Label", label, 22, TextAlignmentOptions.Center, Color.white);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return button;
        }

        private Image CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private GameObject CreateSprite(string name, Sprite sprite, Vector3 position, Color color, int sortingOrder, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_worldRoot, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        private void CreateLabel(string text, Vector3 position, Color color, int size)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(_worldRoot, false);
            go.transform.position = position;
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = _font;
            mesh.fontSize = size;
            mesh.characterSize = 0.1f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 10;
                if (_font != null)
                {
                    renderer.sharedMaterial = _font.material;
                }
            }
        }

        private Vector3 WorldOf(GridCoord coord)
        {
            return new Vector3(coord.X * tileSize, coord.Y * tileSize, 0f);
        }

        private static Color TileColor(TileType type)
        {
            switch (type)
            {
                case TileType.Exit:
                    return new Color(0.15f, 0.55f, 0.25f);
                case TileType.Treasure:
                    return new Color(0.7f, 0.55f, 0.12f);
                case TileType.Hole:
                    return new Color(0.06f, 0.08f, 0.18f);
                case TileType.Damaging:
                    return new Color(0.45f, 0.18f, 0.1f);
                case TileType.Wall:
                    return new Color(0.28f, 0.28f, 0.32f);
                case TileType.Sand:
                    return new Color(0.65f, 0.6f, 0.4f);
                default:
                    return new Color(0.18f, 0.19f, 0.23f);
            }
        }

        private static Font LoadFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }

            return font;
        }

        private static TMP_FontAsset LoadTmpFont()
        {
            var font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }

            return font;
        }
    }
}
