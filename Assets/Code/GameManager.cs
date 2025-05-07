using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    [SerializeField] private TexasHoldemUIManager texasHoldemUIManager;
    [SerializeField] private TexasHoldemManager texasHoldemManager;
    [SerializeField] private FaceCamSquareCrop faceCamSquareCrop;
    [SerializeField] private MrChipsEmojiController emojiController;
    
    private const int PLAYER_INDEX = 0;
    private const int COMPUTER_INDEX = 1;
    private bool showComputerHand = false;

    private void Start() {
        texasHoldemUIManager.OnPlayerAction += HandlePlayerAction;
        
        texasHoldemManager.OnAwaitingNextAction += HandleAwaitingNextAction;
        texasHoldemManager.OnUpdateUI += UpdateUI;
        texasHoldemManager.OnPlayerWin += HandlePlayerWin;
        
        texasHoldemManager.CreateRoom();
        texasHoldemManager.StartNewRound();
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
            emojiController.SetEmotion(Emotion.Thinking);
            string prompt = texasHoldemManager.GetPrompt();
            /*
             * WAITING FOR AI CALL SHOULD BE HANDLED HERE NOT ANYWHERE ELSE, IN FACT YOU PROBABLY DONT NEED TO
             * TOUCH ANYTHING ELSE ANYWHERE IF IM BEING COMPLETELY HONEST CUZ MY CODE IS JUST THAT AMAZING
             * 
             * FOR EXAMPLE:
             * call to AI with prompt
             * AI will call method here based on response, either
             * texasHoldEmManager.HandlePlayerAction(action);
             * OR
             * texasHoldEmManager.HandlePlayerAction(action, raiseAmount);
             * everything else should be handled from there
             * if you run into any issues please contact me, Chris, I made most of this code <3
             */
        }
    }
    
    // format to whatever you need it to be for the AI to read
    private byte[] GetWebCamImage() {
        Texture2D croppedFaceImage = faceCamSquareCrop.GetCroppedSquareTexture();
        return croppedFaceImage.EncodeToPNG();
        // return croppedFaceImage.EncodeToJPG();
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
}
