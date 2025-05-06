using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TexasHoldemUIManager : MonoBehaviour {
    [SerializeField] private Button foldButton;
    [SerializeField] private Button checkButton;
    [SerializeField] private Button betButton;
    [SerializeField] private TMP_InputField betValue;
    
    [SerializeField] private Button debugFoldButton;
    [SerializeField] private Button debugCheckButton;
    [SerializeField] private Button debugBetButton;
    [SerializeField] private TMP_InputField debugBetValue;
    
    [SerializeField] private TextMeshProUGUI potValueText;
    [SerializeField] private TextMeshProUGUI playerStackText;
    [SerializeField] private TextMeshProUGUI computerStackText;

    [SerializeField] private List<SpriteRenderer> communityCards;
    [SerializeField] private List<SpriteRenderer> playerCards;
    [SerializeField] private List<SpriteRenderer> computerCards;
    
    private Sprite cardBack;

    public event Action<Action, int> OnPlayerAction;

    private void Awake() {
        cardBack = Resources.Load<Sprite>("Cards/Card Back");
    }
    
    private void Start() {
        foldButton.onClick.AddListener(() => HandleAction(Action.Fold));
        checkButton.onClick.AddListener(() => HandleAction(Action.Check));
        betButton.onClick.AddListener(() => HandleAction(Action.Bet, Convert.ToInt32(betValue.text)));
    }

    private void HandleAction(Action action, int raiseAmount = 0) {
        OnPlayerAction?.Invoke(action, raiseAmount);
    }

    public void UpdateUI(GameManager.UIUpdateInfo uiUpdateInfo) {
        potValueText.text = $"POT: {uiUpdateInfo.pot}";
        playerStackText.text = $"STACK: {uiUpdateInfo.playerStack}";
        computerStackText.text = $"STACK: {uiUpdateInfo.computerStack}";
        
        for (int i = 0; i < communityCards.Count; i++)
            communityCards[i].sprite = i >= uiUpdateInfo.communityCards.Count || uiUpdateInfo.communityCards[i] == null
                ? cardBack
                : uiUpdateInfo.communityCards[i].Sprite;

        for (int i = 0; i < playerCards.Count; i++)
            playerCards[i].sprite = i >= uiUpdateInfo.playerHand.Count || uiUpdateInfo.playerHand[i] == null
                ? cardBack
                : uiUpdateInfo.playerHand[i].Sprite;

        if (uiUpdateInfo.revealComputerHand) {
            for (int i = 0; i < computerCards.Count; i++)
                computerCards[i].sprite = i >= uiUpdateInfo.computerHand.Count || uiUpdateInfo.computerHand[i] == null
                    ? cardBack
                    : uiUpdateInfo.computerHand[i].Sprite;
        }
    }

}
