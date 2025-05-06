using System.Collections.Generic;
using UnityEngine;

public class TexasHoldemManager : MonoBehaviour {
    private List<Card> communityCards;
    private List<Player> players;

    private const int STARTING_MONEY = 1000;
    
    private int pot;
    private int smallBlind;
    
    
    public TexasHoldemManager() {
        
    }

    public void CreateRoom(int players, int smallBlind, int startingCash = STARTING_MONEY) {
        if (smallBlind >= startingCash / 2) {
            Debug.LogError("small blind cannot be larger than half of starting cash");
            return;
        }

        this.smallBlind = smallBlind;
        for (int i = 0; i < players; i++) {
            this.players.Add(new Player(startingCash));
        }
    }

    public void StartNewRound() {
        
    }

    public void ResetRoom() {
        communityCards.Clear();
        foreach (Player player in this.players) {
            player.GetHand().Clear();
        }
    }
}

public class Player {
    private List<Card> hand;
    private int stack;
    private int currentBet;

    public Player(int startingCash) {
        stack = startingCash;
    }

    public List<Card> GetHand() {
        return hand;
    }

    public int GetStack() {
        return stack;
    }

    public int GetCurrentBet() {
        return currentBet;
    }

    public int Bet(int value) {
        if (value > stack) return 0;

        currentBet = value;
        stack -= value;
        return value;
    }
}

public enum Action {
    Check, Bet, Fold
}

public enum Phase {
    Preflop, Flop, Turn, River
}