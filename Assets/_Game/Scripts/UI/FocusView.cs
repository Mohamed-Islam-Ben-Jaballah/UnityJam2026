using UnityEngine;
using UnityEngine.UI;

namespace DiceDrive.UI
{
    /// <summary>Shows Focus as pips: one Image per point (placed in the scene), coloured filled or empty.</summary>
    public sealed class FocusView : MonoBehaviour
    {
        [SerializeField] private Image[] pips;
        [SerializeField] private Color filled = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color empty = new Color(1f, 1f, 1f, 0.2f);

        public void SetFocus(int value)
        {
            for (var i = 0; i < pips.Length; i++)
            {
                if (pips[i] != null) pips[i].color = i < value ? filled : empty;
            }
        }
    }
}
