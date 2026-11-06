using System;
using System.Collections.Generic;
using DiceDrive.Cards;
using DiceDrive.Core;
using DiceDrive.Entities;
using DiceDrive.Grid;
using DiceDrive.Turn;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DiceDrive.QA
{
    public sealed class QaBootstrap : MonoBehaviour
    {
        public RunConfig runConfig;
        public MapData mapData;
        public int seed = 12345;
        public float tileSize = 1f;
        public float dragLineStartOffset = 24f;

        private GameSession _session;
        private Camera _camera;
        private Transform _worldRoot;
        private Canvas _canvas;
        private Text _statusText;
        private Text _messageText;
        private Text _diceText;
        private Button _rollButton;
        private Button _rerollButton;
        private Button _restartButton;
        private RectTransform _handPanel;
        private Font _font;
        private QaTargetingController _targeting;
        private readonly List<GameObject> _cardObjects = new List<GameObject>();

        public GameSession Session => _session;
        public QaTargetingController Targeting => _targeting;

        private void Start()
        {
            if (runConfig == null || mapData == null)
            {
                Debug.LogError("QA bootstrap needs a RunConfig and a MapData asset.");
                enabled = false;
                return;
            }

            _font = LoadFont();
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
            _session.StartLevel();
            _targeting?.SetSession(_session);
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
        }

        private void OnPhaseChanged(TurnPhase phase)
        {
            if (phase != TurnPhase.AwaitingCard)
            {
                _targeting?.CancelDrag();
            }

            RefreshHand();
            RefreshDice();
            RefreshStatus();
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

        private void RefreshAll()
        {
            _targeting?.CancelDrag();
            RefreshWorld();
            RefreshHand();
            RefreshDice();
            RefreshStatus();
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
                    var tile = _session.Grid.Get(coord);
                    var type = tile.EffectiveType;
                    CreateSprite($"Tile {x},{y}", ShapeSpriteFactory.Square, WorldOf(coord), TileColor(type), 0, tileSize * 0.95f);

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
                CreateLabel($"{enemy.Data.enemyName}\n{speed}/{attack}", WorldOf(enemy.Position) + new Vector3(0f, tileSize * 0.75f, 0f), Color.white, 24);
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

                var label = CreateText(go.transform, "Label", string.Empty, 16, TextAnchor.MiddleCenter, Color.white);
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
                    $"Dice: {_session.Dice.Count}   Roll: {_session.CurrentRoll}   Free reroll: {_session.FreeRerollAvailable}\n" +
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
        }

        private void RefreshStatus()
        {
            if (_statusText != null && _session != null)
            {
                var torch = _session.Torch != null ? _session.Torch.Remaining : 0f;
                _statusText.text =
                    $"HP {_session.Player.Hp}/{_session.Player.MaxHp}   Gold {_session.Gold}   Turn {_session.TurnNumber}   " +
                    $"Phase {_session.Phase}   Torch {torch:0.0}s";
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
            _camera.transform.position = new Vector3(
                (mapData.width - 1) * tileSize * 0.5f,
                (mapData.height - 1) * tileSize * 0.5f + 0.5f,
                -10f);
            _camera.transform.rotation = Quaternion.identity;
            _camera.orthographicSize = Mathf.Max(mapData.width, mapData.height) * 0.5f + 1.5f;
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

            _statusText = CreateText(canvasGo.transform, "Status", string.Empty, 20, TextAnchor.UpperLeft, Color.white);
            var statusRect = _statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0f, 1f);
            statusRect.anchoredPosition = new Vector2(16f, -16f);
            statusRect.sizeDelta = new Vector2(-32f, 80f);

            _messageText = CreateText(canvasGo.transform, "Message", string.Empty, 48, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.3f));
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

            _diceText = CreateText(dice.transform, "Dice Text", string.Empty, 16, TextAnchor.UpperLeft, Color.white);
            var diceTextLayout = _diceText.gameObject.AddComponent<LayoutElement>();
            diceTextLayout.preferredHeight = 42f;

            _rollButton = CreateButton(dice.transform, "Roll Die", () =>
            {
                _session?.RollCurrentDie();
                RefreshDice();
                RefreshHand();
            });

            _rerollButton = CreateButton(dice.transform, "Reroll", () =>
            {
                _session?.Reroll();
                RefreshDice();
                RefreshHand();
            });

            _restartButton = CreateButton(dice.transform, "Restart QA", CreateSession);
        }

        private Text CreateText(Transform parent, string name, string content, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.text = content;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
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

            var text = CreateText(go.transform, "Label", label, 18, TextAnchor.MiddleCenter, Color.white);
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
    }
}
