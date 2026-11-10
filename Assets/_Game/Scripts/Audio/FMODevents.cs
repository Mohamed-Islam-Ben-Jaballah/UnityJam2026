using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
    [field: Header("Card SFX")]
    [field: SerializeField] public EventReference drawCard { get; private set; }
    [field: SerializeField] public EventReference drawCardEnd { get; private set; }
    
    [field: SerializeField] public EventReference placeholderPlayCard { get; private set; }     // [PLACEHOLDER]

    [field: Header("Dice SFX")]
    [field: SerializeField] public EventReference diceRoll { get; private set; }

    [field: Header("Chimes SFX")]
    [field: SerializeField] public EventReference chineseCharm { get; private set; }
    [field: SerializeField] public EventReference devilCharm { get; private set; }
    [field: SerializeField] public EventReference elfCharm { get; private set; }

    [field: Header("Might Be Useful SFX")]
    [field: SerializeField] public EventReference illegalMove { get; private set; }
    [field: SerializeField] public EventReference shuffleDeck { get; private set; }

    [field: Header("UI SFX")]
    [field: SerializeField] public EventReference placeholderButtonClick { get; private set; }  // [PLACEHOLDER]

    [field: Header("Music")]
    [field: SerializeField] public EventReference placeholderMusic { get; private set; }        // [PLACEHOLDER]

    public static FMODEvents instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            return;
        }
        instance = this;
    }
}