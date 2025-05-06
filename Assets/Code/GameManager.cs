using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    [SerializeField] private TexasHoldemUIManager texasHoldemUIManager;
    [SerializeField] private TexasHoldemManager texasHoldemManager;
    
    private const int PLAYER_INDEX = 0;
    private const int COMPUTER_INDEX = 1;

    private void Start() {
        texasHoldemUIManager.OnPlayerAction += HandlePlayerAction;
        texasHoldemManager.OnAwaitingNextAction += HandleAwaitingNextAction;
        
        texasHoldemManager.CreateRoom();
        texasHoldemManager.StartNewRound();
        UpdateUI();
    }
    
    private void HandlePlayerAction(Action action, int raiseAmount = 0) {
        texasHoldemManager.HandlePlayerAction(action, raiseAmount);
    }

    private void HandleAwaitingNextAction(int currentPlayerIndex) {
        UpdateUI();
        if (currentPlayerIndex == PLAYER_INDEX) {
            Debug.Log("Waiting for action from Player");
        }
        else {
            Debug.Log("Waiting for action from Computer");
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
    
    private void SetButtons() {
        
    }
    
    private void UpdateUI() {
        texasHoldemUIManager.UpdateUI(new UIUpdateInfo() {
            pot = texasHoldemManager.Pot,
            playerStack = texasHoldemManager.Players[PLAYER_INDEX].GetStack(),
            computerStack = texasHoldemManager.Players[COMPUTER_INDEX].GetStack(),
            revealComputerHand = true, // update later
            playerHand = texasHoldemManager.Players[PLAYER_INDEX].GetHand(),
            computerHand = texasHoldemManager.Players[COMPUTER_INDEX].GetHand(),
            communityCards = texasHoldemManager.CommunityCards,
        });
    }

    public class UIUpdateInfo {
        public int pot { get; set; }
        public int playerStack { get; set; }
        public int computerStack { get; set; }
        public bool revealComputerHand { get; set; }
        public List<Card> playerHand { get; set; }
        public List<Card> computerHand { get; set; }
        public List<Card> communityCards { get; set; }
    }
}
