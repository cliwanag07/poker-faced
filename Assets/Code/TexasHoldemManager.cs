using System.Collections.Generic;
using UnityEngine;

public class TexasHoldemManager : MonoBehaviour {
    private List<Card> communityCards;
    private List<Player> players;

    private const int STARTING_MONEY = 1000;
    
    private int pot;
    private int playerBet;
    private int computerBet;
    private int playerBalance;
    private int computerBalance;
    
    public TexasHoldemManager() {
        
    }

    public void CreateRoom(int players, int startingCash = STARTING_MONEY) {
        for (int i = 0; i < players; i++) {
            this.players.Add(new Player(startingCash));
        }
    }

    public void StartNewRound() {
        
    }

    public void ResetRoom() {
        communityCards.Clear();
        playerBalance = STARTING_MONEY;
        computerBalance = STARTING_MONEY;
    }
}

public class Player {
    private List<Card> hand;
    private int cash;

    public Player(int startingCash) {
        cash = startingCash;
    }

    public int Bet(int value) {
        if (value > cash) return 0;

        cash -= value;
        return value;
    }
}

public enum Action {
    Check, Bet, Fold
}

public enum Phase {
    Preflop, Flop, Turn, River
}