using DiceDrive.UI;
using UnityEngine;

namespace DiceDrive.DiceSystem
{
    /// <summary>
    /// Temporary roller for the UI scene: rolls the assigned die when the die view is clicked.
    /// In gameplay, replace it with a binding that calls GameSession.RollCurrentDie() and
    /// forwards GameSession.DiceRolled to <see cref="DiceView.PlayRoll"/>.
    /// </summary>
    public sealed class PlaceholderDiceRoller : MonoBehaviour
    {
        [SerializeField] private DiceView view;
        [SerializeField] private DiceData die;

        private readonly System.Random random = new();

        private void OnEnable()
        {
            if (view == null) view = FindAnyObjectByType<DiceView>(FindObjectsInactive.Include); // prefab used in a new scene
            if (view != null) view.RollRequested += Roll;
        }

        private void OnDisable()
        {
            if (view != null) view.RollRequested -= Roll;
        }

        private void Roll()
        {
            int result = new Die(die.sides).Roll(random);
            Debug.Log($"[Dice] Rolled d{die.sides}: {result}", this);
            view.PlayRoll(result, die.sides);
        }
    }
}
