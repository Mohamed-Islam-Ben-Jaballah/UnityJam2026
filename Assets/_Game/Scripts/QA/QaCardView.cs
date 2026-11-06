using DiceDrive.Cards;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceDrive.QA
{
    public sealed class QaCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private QaBootstrap _bootstrap;
        private CardInstance _card;
        private Image _background;
        private Text _label;
        private bool _dragging;

        public CardInstance Card => _card;

        public void Initialize(QaBootstrap bootstrap, CardInstance card, Image background, Text label)
        {
            _bootstrap = bootstrap;
            _card = card;
            _background = background;
            _label = label;
            Refresh();
        }

        public void Refresh()
        {
            if (_card == null || _card.Data == null)
            {
                return;
            }

            var typeName = _card.Data.cardType != null ? _card.Data.cardType.displayName : "?";
            var lucky = _card.Data.cardType != null && _card.Data.cardType.typeId != CardTypeId.Normal
                ? $" ({_card.Data.cardType.luckyNumber})"
                : string.Empty;

            if (_label != null)
            {
                _label.text = $"{_card.Data.cardName}\n{typeName}{lucky}";
            }

            if (_background != null)
            {
                _background.color = _bootstrap != null && _bootstrap.IsCardPlayable(_card)
                    ? new Color(0.2f, 0.45f, 0.25f, 0.95f)
                    : new Color(0.25f, 0.25f, 0.28f, 0.95f);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_bootstrap == null || !_bootstrap.IsCardPlayable(_card))
            {
                return;
            }

            _dragging = true;
            var rect = (RectTransform)transform;
            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var start = RectTransformUtility.WorldToScreenPoint(camera, rect.position);
            start += new Vector2(0f, -_bootstrap.dragLineStartOffset);
            _bootstrap.Targeting.BeginDrag(_card, start);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragging)
            {
                _bootstrap.Targeting.UpdateHover(eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging)
            {
                return;
            }

            _dragging = false;
            _bootstrap.Targeting.EndDrag(eventData.position);
        }
    }
}
