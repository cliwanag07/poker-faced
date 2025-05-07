using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour {
    [SerializeField] private TexasHoldemUIManager texasHoldemUIManager;
    [SerializeField] private TexasHoldemManager texasHoldemManager;
    [SerializeField] private FaceCamSquareCrop faceCamSquareCrop;
    [SerializeField] private MrChipsEmojiController emojiController;
    [SerializeField] private AICaller aICaller;
    
    private const int PLAYER_INDEX = 0;
    private const int COMPUTER_INDEX = 1;
    private bool showComputerHand = false;

    private float AIResponse;

    private void Start() {
        texasHoldemUIManager.OnPlayerAction += HandlePlayerAction;
        
        texasHoldemManager.OnAwaitingNextAction += HandleAwaitingNextAction;
        texasHoldemManager.OnUpdateUI += UpdateUI;
        texasHoldemManager.OnPlayerWin += HandlePlayerWin;
        
        texasHoldemManager.CreateRoom();
        texasHoldemManager.StartNewRound();

        aICaller.OnApiResponseReceived += HandleApiResponse;
        UpdateUI();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            if (texasHoldemManager.Phase == Phase.EndRound)
                texasHoldemManager.ResetRound();
        }

        showComputerHand = texasHoldemManager.Phase == Phase.EndRound;
    }

    private void HandlePlayerWin(int playerIndex) {
        Debug.Log(playerIndex);
        emojiController.SetEmotion(playerIndex == PLAYER_INDEX ? Emotion.Sad : Emotion.Happy);
    }

    private Player GetUser() {
        return texasHoldemManager.Players[PLAYER_INDEX];
    }

    private Player GetComputer() {
        return texasHoldemManager.Players[COMPUTER_INDEX];
    }
    
    private void HandlePlayerAction(Action action, int raiseAmount = 0) {
        texasHoldemManager.HandlePlayerAction(action, raiseAmount);
    }

    private void HandleAwaitingNextAction(int currentPlayerIndex) {
        UpdateUI();
        if (texasHoldemManager.Phase == Phase.EndRound) return;
        SetButtons();
        if (currentPlayerIndex == PLAYER_INDEX) {
            Debug.Log("Waiting for action from Player");
            emojiController.SetEmotion(Emotion.Idle);
        }
        else {
            Debug.Log("Waiting for action from Computer");
            var cardInfo = GetAICardInfo();
            var cards = cardInfo.cardValNumbers;
            Debug.Log(string.Join(", ", cardInfo.cardValNumbers));
            var suits = cardInfo.cardSuits;
            Debug.Log(string.Join(", ", cardInfo.cardSuits));
            var ratio = cardInfo.computerChipsToUserRatio;
            
            aICaller.GetAIResponse(cards, suits, ratio, GetWebCamImage());
        }
    }

    private void HandleApiResponse(float response) {
        Debug.Log(response);

        var userLastAction = texasHoldemManager.Players[PLAYER_INDEX].GetAction();
        switch (userLastAction) {
            case Action.Bet:
                switch (response) {
                    case > 0.85f: // Raise
                        texasHoldemManager.HandlePlayerAction(Action.Bet, texasHoldemManager.Pot);
                        break;
                    case > 0.5f: // Call
                        texasHoldemManager.HandlePlayerAction(Action.Call);
                        break;
                    case > 0.25f: // Call if bet < 25% of stack
                        texasHoldemManager.HandlePlayerAction(
                            GetComputer().GetCurrentBet() < texasHoldemManager.Pot * .25 ? Action.Call : Action.Fold);
                        break;
                    default: // Fold
                        texasHoldemManager.HandlePlayerAction(Action.Fold);
                        break;
                }
                break;
            case Action.Check:
                switch (response) {
                    case > 0.65f: // Raise
                        texasHoldemManager.HandlePlayerAction(Action.Bet, texasHoldemManager.Pot);
                        break;
                    default: // Check
                        texasHoldemManager.HandlePlayerAction(Action.Check);
                        break;
                }
                break;
            default:
                if (texasHoldemManager.Phase != Phase.Preflop) { // NOT IN PREFLOP
                    switch (response) {
                        case > 0.85f: // Raise
                            texasHoldemManager.HandlePlayerAction(Action.Bet, texasHoldemManager.Pot);
                            break;
                        case > 0.25f: // Check
                            texasHoldemManager.HandlePlayerAction(Action.Check);
                            break;
                        default: // Fold
                            texasHoldemManager.HandlePlayerAction(Action.Fold);
                            break;
                    }
                } else { // IN PREFLOP
                    if (GetComputer().IsSmallBlind) { // YOU ARE FIRST TO MOVE
                        switch (response) { 
                            case > 0.85f: // Raise
                                texasHoldemManager.HandlePlayerAction(Action.Bet, texasHoldemManager.Pot);
                                break;
                            case > 0.15f: // Call
                                texasHoldemManager.HandlePlayerAction(Action.Call);
                                break;
                            default: // Fold
                                texasHoldemManager.HandlePlayerAction(Action.Fold);
                                break;
                        }
                    } else { // YOU ARE SECOND TO MOVE
                        switch (response) {
                            case > 0.85f: // Raise
                                texasHoldemManager.HandlePlayerAction(Action.Bet, texasHoldemManager.Pot);
                                break;
                            default: // Check
                                texasHoldemManager.HandlePlayerAction(Action.Check);
                                break;
                        }
                    }
                }
                break;
        }
    }
    
    // format to whatever you need it to be for the AI to read
    private byte[] GetWebCamImage() {
        Texture2D croppedFaceImage = faceCamSquareCrop.GetCroppedSquareTexture();
        // return croppedFaceImage.EncodeToPNG();
        return croppedFaceImage.EncodeToJPG();
    }

    private AICardInfo GetAICardInfo() {
        var cardInfo = new AICardInfo();

        // get player hand (2 cards)
        for (int i = 0; i < 2; ++i) {
            var card = texasHoldemManager.Players[COMPUTER_INDEX].GetHand()[i];
            cardInfo.cardValNumbers.Add(card.Value.ToInt());
            cardInfo.cardValString.Add(card.Value.ToString());
            cardInfo.cardSuits.Add(card.Suit.ToSymbol());
        }
        
        // get community cards (up to 5 cards)
        for (int i = 0; i < 5; ++i) {
            if (i < texasHoldemManager.CommunityCards.Count && texasHoldemManager.CommunityCards[i] != null) {
                var card = texasHoldemManager.CommunityCards[i];
                cardInfo.cardValNumbers.Add(card.Value.ToInt());
                cardInfo.cardValString.Add(card.Value.ToString());
                cardInfo.cardSuits.Add(card.Suit.ToSymbol());
            } else {
                // append placeholder if no available community card
                cardInfo.cardValNumbers.Add(0);
                cardInfo.cardValString.Add("NONE");
                cardInfo.cardSuits.Add("NONE");
            }
        }
        
        cardInfo.computerChipsToUserRatio =
            Math.Round(
                (double)texasHoldemManager.Players[COMPUTER_INDEX].GetStack() /
                texasHoldemManager.Players[PLAYER_INDEX].GetStack(), 3);

        return cardInfo;
    }

    
    private void SetButtons() {
        #if UNITY_EDITOR
        if (texasHoldemManager.GetCurrentPlayerIndex() == PLAYER_INDEX) 
            texasHoldemUIManager.SetButtons(true, false);
        else 
            texasHoldemUIManager.SetButtons(false, true);
        #else
        if (texasHoldemManager.GetCurrentPlayerIndex() == PLAYER_INDEX) 
            texasHoldemUIManager.SetButtons(true);
        else 
            texasHoldemUIManager.SetButtons(false);
        #endif
    }
    
    private void UpdateUI() {
        texasHoldemUIManager.UpdateUI(new UIUpdateInfo{
            pot = texasHoldemManager.Pot,
            playerStack = texasHoldemManager.Players[PLAYER_INDEX].GetStack(),
            computerStack = texasHoldemManager.Players[COMPUTER_INDEX].GetStack(),
            playerCurrentBet = texasHoldemManager.Players[PLAYER_INDEX].GetCurrentBet(),
            computerCurrentBet = texasHoldemManager.Players[COMPUTER_INDEX].GetCurrentBet(),
            #if UNITY_EDITOR
            revealComputerHand = true,
            #else
            revealComputerHand = showComputerHand,
            #endif
            playerHand = texasHoldemManager.Players[PLAYER_INDEX].GetHand(),
            computerHand = texasHoldemManager.Players[COMPUTER_INDEX].GetHand(),
            communityCards = texasHoldemManager.CommunityCards,
            log = texasHoldemManager.Log,
        });
        texasHoldemUIManager.SetCurrentAction(texasHoldemManager.GetCurrentPlayerIndex() == PLAYER_INDEX ? "YOU" : "COMPUTER");
    }

    public class UIUpdateInfo {
        public int pot { get; set; }
        public int playerStack { get; set; }
        public int computerStack { get; set; }
        public int playerCurrentBet { get; set; }
        public int computerCurrentBet { get; set; }
        public bool revealComputerHand { get; set; }
        public List<Card> playerHand { get; set; }
        public List<Card> computerHand { get; set; }
        public List<Card> communityCards { get; set; }
        public string log { get; set; }
    }

    public class AICardInfo {
        public List<string> cardValString = new();
        public List<int> cardValNumbers = new();
        public List<string> cardSuits = new();
        public double computerChipsToUserRatio = 0.0;
    }

}
