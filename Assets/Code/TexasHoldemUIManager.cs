using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TexasHoldemUIManager : MonoBehaviour {
    [SerializeField] private GameObject playerButtons;
    [SerializeField] private Button betButton;
    [SerializeField] private TMP_InputField betValue;
    [SerializeField] private Button callButton;
    [SerializeField] private Button checkButton;
    [SerializeField] private Button foldButton;
    
    [SerializeField] private GameObject debugButtons;
    [SerializeField] private Button debugBetButton;
    [SerializeField] private TMP_InputField debugBetValue;
    [SerializeField] private Button debugCallButton;
    [SerializeField] private Button debugCheckButton;
    [SerializeField] private Button debugFoldButton;
    
    [SerializeField] private TextMeshProUGUI potValueText;
    [SerializeField] private TextMeshProUGUI currentActionText;
    [SerializeField] private TextMeshProUGUI playerStackText;
    [SerializeField] private TextMeshProUGUI computerStackText;
    [SerializeField] private TextMeshProUGUI playerCurrentBetText;
    [SerializeField] private TextMeshProUGUI computerCurrentBetText;

    [SerializeField] private List<SpriteRenderer> communityCards;
    [SerializeField] private List<SpriteRenderer> playerCards;
    [SerializeField] private List<SpriteRenderer> computerCards;
    
    [SerializeField] private TextMeshProUGUI log;
    
    private Sprite cardBack;

    public event Action<Action, int> OnPlayerAction;

    private void Awake() {
        cardBack = Resources.Load<Sprite>("Cards/Card Back");
        playerButtons.SetActive(false);
        debugButtons.SetActive(false);
    }
    
    private void Start() {
        betButton.onClick.AddListener(() => HandleAction(Action.Bet, Convert.ToInt32(betValue.text)));
        callButton.onClick.AddListener(() => HandleAction(Action.Call));
        checkButton.onClick.AddListener(() => HandleAction(Action.Check));
        foldButton.onClick.AddListener(() => HandleAction(Action.Fold));
        
        #if UNITY_EDITOR
        debugBetButton.onClick.AddListener(() => HandleAction(Action.Bet, Convert.ToInt32(debugBetValue.text)));
        debugCallButton.onClick.AddListener(() => HandleAction(Action.Call));
        debugCheckButton.onClick.AddListener(() => HandleAction(Action.Check));
        debugFoldButton.onClick.AddListener(() => HandleAction(Action.Fold));
        #endif
    }

    public void SetButtons(bool playerButtonsActive, bool debugButtonsActive=false) {
        playerButtons.SetActive(playerButtonsActive);
        debugButtons.SetActive(debugButtonsActive);
    }

    private void HandleAction(Action action, int raiseAmount = 0) {
        OnPlayerAction?.Invoke(action, raiseAmount);
    }

    public void SetCurrentAction(string actionOn) {
        currentActionText.text = "CURRENT ACTION: \n" + actionOn;
    }

    public void UpdateUI(GameManager.UIUpdateInfo uiUpdateInfo) {
        potValueText.text = $"POT: {uiUpdateInfo.pot}";
        playerStackText.text = $"STACK: {uiUpdateInfo.playerStack}";
        computerStackText.text = $"STACK: {uiUpdateInfo.computerStack}";
        playerCurrentBetText.text = $"BET: {uiUpdateInfo.playerCurrentBet}";
        computerCurrentBetText.text = $"BET: {uiUpdateInfo.computerCurrentBet}";
        
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
        
        log.text = uiUpdateInfo.log;
    }

}
