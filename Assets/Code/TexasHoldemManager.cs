using System.Collections.Generic;
using UnityEngine;

public class TexasHoldemManager : MonoBehaviour {
    private List<Card> communityCards;
    private List<Card> playerHand;
    private List<Card> computerHand;

    private readonly int STARTING_MONEY = 1000;
    
    private int pot;
    private int playerBet;
    private int computerBet;
    private int playerBalance;
    private int computerBalance;
    
    public TexasHoldemManager() {
        communityCards = new List<Card>();
        playerHand = new List<Card>();
        computerHand = new List<Card>();
        playerBalance = STARTING_MONEY;
        computerBalance = STARTING_MONEY;
    }

    public void ResetRound() {
        communityCards.Clear();
        playerHand.Clear();
        computerHand.Clear();
        playerBalance = STARTING_MONEY;
        computerBalance = STARTING_MONEY;
    }
}

public enum Action {
    Check, Bet, Fold
}

public enum Phase {
    Preflop, Flop, Turn, River
}