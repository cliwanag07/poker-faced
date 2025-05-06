using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manager for Heads up version of Texas Holdem (1v1)
/// </summary>
public class TexasHoldemManager : MonoBehaviour {
    private CardDeck cardDeck;
    private List<Card> communityCards;
    private List<Player> players;

    private const int STARTING_MONEY = 1000;

    private Phase phase;
    private int pot;
    private int smallBlind;
    private int startingPlayer = 0;
    private int currentPlayer = 0;
    private int currentBet;
    
    private bool awaitingPlayerAction = false;

    public event Action<int> OnAwaitingNextAction;

    private void Awake() {
        communityCards = new List<Card>();
        players = new List<Player>();
        cardDeck = new CardDeck();
    }
    
    public List<Card> CommunityCards => communityCards;

    public List<Player> Players => players;
    public int Pot => pot;

    public void CreateRoom(int smallBlind = STARTING_MONEY / 100, int startingCash = STARTING_MONEY) {
        ResetRoom();

        if (smallBlind >= startingCash / 2) {
            Debug.LogError("Small blind cannot be larger than half of starting cash");
            return;
        }

        this.smallBlind = smallBlind;
        players.Add(new Player(startingCash));
        players.Add(new Player(startingCash));
        startingPlayer = 0;
    }

    public void StartNewRound() {
        communityCards.Clear();

        foreach (Player player in players) {
            player.ResetForRound();
        }

        // Alternate starting player
        startingPlayer = startingPlayer == 1 ? 0 : 1;
        currentPlayer = startingPlayer;

        // Assign blind roles
        players[startingPlayer].IsSmallBlind = true;
        players[1 - startingPlayer].IsBigBlind = true;

        phase = Phase.Preflop;
        cardDeck.ResetDeck();
        currentBet = 0;

        DealPlayers();
        CollectBlinds();

        AwaitAction();
    }

    private void DealPlayers() {
        foreach (var player in players) {
            for (int i = 0; i < 2; i++) {
                player.GetHand().Add(cardDeck.GetAndRemoveRandomCard());
            }
        }
    }

    private void CollectBlinds() {
        Player sb = players.Find(p => p.IsSmallBlind);
        Player bb = players.Find(p => p.IsBigBlind);

        int sbBet = sb.GetStack() >= smallBlind ? smallBlind : sb.GetStack();
        Bet(sbBet, sb);

        int bbBet = bb.GetStack() >= smallBlind * 2 ? smallBlind * 2 : bb.GetStack();
        Bet(bbBet, bb);
    }

    private void Bet(int betAmount, Player player) {
        int actualBet = player.Bet(betAmount);
        pot += actualBet;

        if (actualBet > currentBet) {
            currentBet = actualBet;
        }

        Debug.Log($"{player} bet {actualBet}. Stack: {player.GetStack()}, All-In: {player.GetIsAllIn()}");
    }
    
    public void AwaitAction() {
        awaitingPlayerAction = true;
        OnAwaitingNextAction?.Invoke(GetCurrentPlayerIndex());
    }
    
    public void HandlePlayerAction(Action action, int raiseAmount = 0) {
        if (!awaitingPlayerAction) return;

        Player player = GetCurrentPlayer();
        Player opponent = GetOtherPlayer();

        player.SetAction(action);
        awaitingPlayerAction = false;

        switch (action) {
            case Action.Fold:
                EndRound(opponent); // other player wins
                break;

            case Action.Check:
                if (player.GetCurrentBet() == opponent.GetCurrentBet()) {
                    ProceedOrNextPlayer();
                } else {
                    Debug.LogWarning("Cannot check: unmatched bets");
                    AwaitAction();
                }
                break;

            case Action.Bet:
                if (raiseAmount > 0 && raiseAmount <= player.GetStack()) {
                    Bet(raiseAmount, player);
                    SetNextPlayer();
                    AwaitAction();
                } else {
                    Debug.LogWarning("Invalid raise amount");
                    AwaitAction();
                }
                break;

            default:
                Debug.LogWarning("Unhandled action type");
                AwaitAction();
                break;
        }
    }

    private void ProceedOrNextPlayer() {
        if (currentPlayer == startingPlayer) {
            SetNextPlayer();
            AwaitAction();
        } else {
            if (BothPlayersHaveMatchedBets()) {
                AdvancePhase();
            } else {
                SetNextPlayer();
                AwaitAction();
            }
        }
    }

    private bool BothPlayersHaveMatchedBets() {
        return players[0].GetCurrentBet() == players[1].GetCurrentBet();
    }

    public void AdvancePhase() {
        // Reset round state for the new phase
        foreach (var player in players) {
            player.ResetCurrentBet();
            player.SetAction(Action.None);
        }

        currentBet = 0;
        currentPlayer = startingPlayer;
        
        // Transition between phases (Flop -> Turn -> River)
        switch (phase) {
            case Phase.Preflop:
                phase = Phase.Flop;
                DealFlop();
                break;
            case Phase.Flop:
                phase = Phase.Turn;
                DealTurn();
                break;
            case Phase.Turn:
                phase = Phase.River;
                DealRiver();
                break;
            case Phase.River:
                EvaluateHands();
                EndRoundBasedOnHands();
                break;
            default:
                Debug.LogError("Unknown phase");
                break;
        }
        
        AwaitAction();
    }
    
    private void DealFlop() {
        Debug.Log("Dealing Flop...");
        communityCards.Add(cardDeck.GetAndRemoveRandomCard());
        communityCards.Add(cardDeck.GetAndRemoveRandomCard());
        communityCards.Add(cardDeck.GetAndRemoveRandomCard());
    }

    private void DealTurn() {
        Debug.Log("Dealing Turn...");
        communityCards.Add(cardDeck.GetAndRemoveRandomCard());
    }

    private void DealRiver() {
        Debug.Log("Dealing River...");
        communityCards.Add(cardDeck.GetAndRemoveRandomCard());
    }

    private void EndRound(Player winner) {
        winner.ReceiveWinnings(pot);
        Debug.Log($"Player {players.IndexOf(winner)} wins the pot of {pot}");
        ResetRound();
    }
    
    private void EvaluateHands() {
        Debug.Log("Evaluating hands...");

        // Evaluate the best hand for both players
        foreach (var player in players) {
            player.EvaluateHand(communityCards);
        }
    }

    private void EndRoundBasedOnHands() {
        Player winner = DetermineWinner();
        winner.ReceiveWinnings(pot);
        Debug.Log($"Player {players.IndexOf(winner)} wins the pot of {pot}");
        ResetRound();
    }
    
    private Player DetermineWinner() {
        // Assuming you have a method to determine the winning player by hand ranking
        double player1Rank = players[0].GetHandRank();
        double player2Rank = players[1].GetHandRank();

        if (player1Rank > player2Rank) {
            return players[0];
        } else if (player2Rank > player1Rank) {
            return players[1];
        } else {
            // Handle tie logic (for example, split the pot)
            Debug.Log("It's a tie!");
            return players[0]; // or players[1], depending on your tie-handling logic
        }
    }

    private void ResetRound() {
        pot = 0;
        foreach (Player p in players) {
            p.SetAction(Action.None);
            p.SetIsAllIn(false);
            p.ResetCurrentBet();
        }
        StartNewRound();
    }

    private void SetNextPlayer() {
        currentPlayer = 1 - currentPlayer;
    }

    private Player GetCurrentPlayer() => players[currentPlayer];
    private Player GetOtherPlayer() => players[1 - currentPlayer];
    
    private int GetCurrentPlayerIndex() => currentPlayer;

    public void ResetRoom() {
        players.Clear();
        pot = 0;
    }
}


public class Player {
    private List<Card> hand = new List<Card>();
    private int stack;
    private int currentBet;
    private bool allIn;
    private Action action;
    private double handRank;

    public bool IsSmallBlind { get; set; }
    public bool IsBigBlind { get; set; }

    public Player(int startingCash) {
        stack = startingCash;
    }

    public List<Card> GetHand() => hand;
    public int GetStack() => stack;
    public int GetCurrentBet() => currentBet;
    public bool GetIsAllIn() => allIn;
    public Action GetAction() => action;

    public void SetAction(Action action) => this.action = action;
    public void SetIsAllIn(bool value) => allIn = value;

    public int Bet(int value) {
        int actualBet = Mathf.Min(value, stack);
        currentBet = actualBet;
        stack -= actualBet;
        allIn = stack == 0;
        return actualBet;
    }

    public void ResetForRound() {
        hand.Clear();
        currentBet = 0;
        allIn = false;
        action = Action.None;
        IsSmallBlind = false;
        IsBigBlind = false;
    }
    
    public void ReceiveWinnings(int amount) {
        stack += amount;
    }

    public void ResetCurrentBet() {
        currentBet = 0;
    }
    
    public void EvaluateHand(List<Card> communityCards) {
        // Combine the player's hand with the community cards
        List<Card> allCards = new List<Card>(hand);
        allCards.AddRange(communityCards);

        // Evaluate the best 5-card hand here (simplified; you should implement the poker hand evaluation logic)
        handRank = EvaluatePokerHand(allCards);
    }

    public double GetHandRank() {
        return handRank;
    }

    private double EvaluatePokerHand(List<Card> cards) {
        List<string[]> cardsOnTable = cards.Select(card => card.ToCustomFormat()).ToList();

        return HandValue.EvaluateRankByHighestCards(cardsOnTable);
    }
}


public enum Action {
    None, Check, Bet, Fold
}

public enum Phase {
    Preflop, Flop, Turn, River
}