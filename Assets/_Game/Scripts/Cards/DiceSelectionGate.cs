using DiceDrive.UI;
using UnityEngine;

namespace DiceDrive.Cards
{
    /// <summary>
    /// Allows card selection only after the die has landed: locked at start and while rolling,
    /// unlocked on <see cref="DiceView.Landed"/>.
    /// </summary>
    public sealed class DiceSelectionGate : MonoBehaviour
    {
        [SerializeField] private CardSelectionService service;
        [SerializeField] private DiceView dice;

        private void OnEnable()
        {
            if (service == null) service = GetComponent<CardSelectionService>();
            if (service == null) service = FindAnyObjectByType<CardSelectionService>();
            if (dice == null) dice = FindAnyObjectByType<DiceView>(FindObjectsInactive.Include); // prefab used in a new scene
            if (service == null || dice == null) return;

            service.Selection.SelectionEnabled = false;
            dice.RollStarted += Lock;
            dice.Landed += Unlock;
        }

        private void OnDisable()
        {
            if (dice == null) return;
            dice.RollStarted -= Lock;
            dice.Landed -= Unlock;
        }

        private void Lock() => service.Selection.SelectionEnabled = false;
        private void Unlock(int result) => service.Selection.SelectionEnabled = true;
    }
}
