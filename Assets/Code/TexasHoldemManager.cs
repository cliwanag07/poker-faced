using System;
using System.Collections;
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

    private float delay = 1.0f;

    private string log;
    
    private bool awaitingPlayerAction = false;
    
    private const string PLAYER1_COLOR = "blue"; // Blue
    private const string PLAYER2_COLOR = "red"; // Red
    private const string WARNING_COLOR = "yellow"; // Yellow
    private const string SYSTEM_COLOR = "#CCCCCC";  // Gray

    public event Action<int> OnAwaitingNextAction;
    public event System.Action OnUpdateUI;

    private void Awake() {
        communityCards = new List<Card>();
        players = new List<Player>();
        cardDeck = new CardDeck();
    }
    
    public List<Card> CommunityCards => communityCards;

    public List<Player> Players => players;
    public int Pot => pot;
    public string Log => log;
    public Phase Phase => phase;

    public void CreateRoom(int smallBlind = STARTING_MONEY / 100, int startingCash = STARTING_MONEY) {
        ResetRoom();

        if (smallBlind >= startingCash / 2) {
            Debug.LogError("Small blind cannot be larger than half of starting cash");
            return;
        }

        this.smallBlind = smallBlind;
        players.Add(new Player(startingCash));
        players.Add(new Player(startingCash));
        startingPlayer = 1;
    }

    public void StartNewRound() {
        AppendToLog($"<color={SYSTEM_COLOR}><b>-- New Round Started --</b></color>");
        
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
    
    private void AppendToLog(string message) {
        log += $"{message}\n";
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

        string color = players.IndexOf(player) == 0 ? PLAYER1_COLOR : PLAYER2_COLOR;
        AppendToLog(player.GetIsAllIn()
            ? $"<color={color}>Player {players.IndexOf(player) + 1}</color>: Goes all in with {actualBet}"
            : $"<color={color}>Player {players.IndexOf(player) + 1}</color>: Bet {actualBet}");
    }
    
    public void AwaitAction() {
        awaitingPlayerAction = true;
        OnAwaitingNextAction?.Invoke(GetCurrentPlayerIndex());
    }
    
    public void HandlePlayerAction(Action action, int raiseAmount = 0) {
        if (phase == Phase.EndRound) return;
        if (!awaitingPlayerAction) return;

        Player player = GetCurrentPlayer();
        Player opponent = GetOtherPlayer();
        int index = players.IndexOf(player);
        string color = index == 0 ? PLAYER1_COLOR : PLAYER2_COLOR;
        string label = $"<color={color}>Player {index + 1}</color>";

        player.SetAction(action);
        awaitingPlayerAction = false;

        switch (action) {
            case Action.Fold:
                AppendToLog(label + ": Folds");
                EndRound(opponent);
                break;

            case Action.Check:
                if (player.GetCurrentBet() == opponent.GetCurrentBet()) {
                    AppendToLog(label + ": Checks");
                    ProceedOrNextPlayer();
                } else if (AnyPlayerAllIn()) {
                    Debug.LogWarning("Can only fold or call when opponent is all in");
                    AwaitAction();
                } else {
                    Debug.LogWarning("Cannot check: unmatched bets");
                    AppendToLog($"<color={WARNING_COLOR}>Cannot check when bets unmatched</color>");
                    AwaitAction();
                }
                break;
            
            case Action.Call:
                int callAmount = opponent.GetCurrentBet() - player.GetCurrentBet();
                if (callAmount > 0) {
                    var actualCall = player.Bet(callAmount);
                    if (opponent.GetIsAllIn()) {
                        StartCoroutine(AutoAdvanceToShowdown());
                    } else if (player.GetIsAllIn()) {
                        AppendToLog(label + $": Goes all-in with {actualCall}");
                        // refund side pot if applicable
                        if (opponent.GetCurrentBet() - actualCall > 0) {
                            opponent.AddToStack(opponent.GetCurrentBet() - actualCall);
                            opponent.AddToBetAmount(-(opponent.GetCurrentBet() - actualCall));
                        }
                        pot += actualCall;
                        StartCoroutine(AutoAdvanceToShowdown());
                    } else {
                        pot += actualCall;
                        AppendToLog(label + $": Calls with {actualCall}");
                        if (phase == Phase.Preflop) ProceedOrNextPlayer();
                        else AdvancePhase();
                    }
                } else {
                    Debug.LogWarning("Invalid call amount");
                    AppendToLog($"<color={WARNING_COLOR}>Invalid call amount</color>");
                    AwaitAction();
                }
                break;

            case Action.Bet:
                if (AnyPlayerAllIn()) {
                    Debug.LogWarning("Can only fold or call when opponent is all in");
                    AppendToLog($"<color={WARNING_COLOR}>Can only fold or call when opponent is all in</color>");
                    AwaitAction();
                } else if (raiseAmount > 0 && raiseAmount <= player.GetStack()) {
                    Bet(raiseAmount, player);
                    // if (player.GetIsAllIn()) {
                    //     StartCoroutine(AutoAdvanceToShowdown());
                    // }
                    // else {
                        SetNextPlayer();
                        AwaitAction();
                    // }
                } else {
                    Debug.LogWarning("Invalid bet amount");
                    AppendToLog($"<color={WARNING_COLOR}>Invalid bet amount</color>");
                    AwaitAction();
                }
                break;

            default:
                Debug.LogError("Unhandled action type");
                AwaitAction();
                break;
        }

    }
    // prompt for the AI to use
    public string GetPrompt() {
        string smallBlindAmount = $"{smallBlind} chips";
        string bigBlindAmount = $"{smallBlind * 2} chips";
        string startingStack = $"{STARTING_MONEY} chips";
        string playerPosition = currentPlayer == 0 ? "SB" : "BB";
        string playerHand = string.Join(" and ", players[currentPlayer].GetHand().Select(card => card.ToString()));
        string opponentActions = log; 
        string communityCardsString = string.Join(", ", communityCards.Select(card => card.ToString()));
        string potSize = $"{pot} chips";

        return $"You are a specialist in playing 6-handed No Limit Texas Holdem. The following will be a game scenario and you need to make the optimal decision.\n\n" +
               $"Here is a game summary:\n\n" +
               $"The small blind is {smallBlindAmount} and the big blind is {bigBlindAmount}. Everyone started with {startingStack}.\n" +
               $"The player positions involved in this game are UTG, HJ, CO, BTN, SB, BB.\n" +
               $"In this hand, your position is {playerPosition}, and your holding is [{playerHand}].\n" +
               $"{opponentActions}\n" +
               $"The community cards are {communityCardsString}.\n\n" +
               $"To remind you, the current pot size is {potSize}, and your holding is [{playerHand}].\n\n" +
               $"Decide on an action based on the strength of your hand on this board, your position, and actions before you. Do not explain your answer.\n" +
               $"Your optimal action is:";
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
    
    private IEnumerator AutoAdvanceToShowdown() {
        yield return new WaitForSeconds(delay); // delay for pacing

        while (phase != Phase.River) {
            AdvancePhase();
            yield return new WaitForSeconds(delay);
        }

        // Final phase is River, trigger hand evaluation
        EvaluateHands();
        EndRoundBasedOnHands();
    }
    
    private bool AnyPlayerAllIn() {
        return players.Any(p => p.GetIsAllIn());
    }


    private bool BothPlayersHaveMatchedBets() {
        return players[0].GetCurrentBet() == players[1].GetCurrentBet();
    }

    private void AdvancePhase()
    {
        // Reset round state for the new phase
        foreach (var player in players)
        {
            player.ResetCurrentBet();
            player.SetAction(Action.None);
        }

        currentBet = 0;
        currentPlayer = startingPlayer;

        switch (phase)
        {
            case Phase.Preflop:
                phase = Phase.Flop;
                AppendToLog($"<color={SYSTEM_COLOR}><b>Dealing Flop</b></color>");
                StartCoroutine(DealFlop());
                break;
            case Phase.Flop:
                phase = Phase.Turn;
                AppendToLog($"<color={SYSTEM_COLOR}><b>Dealing Turn</b></color>");
                StartCoroutine(DealTurn());
                break;
            case Phase.Turn:
                phase = Phase.River;
                AppendToLog($"<color={SYSTEM_COLOR}><b>Dealing River</b></color>");
                StartCoroutine(DealRiver());
                break;
            case Phase.River:
                EvaluateHands();
                EndRoundBasedOnHands();
                break;
            default:
                Debug.LogError("Unknown phase");
                break;
        }
    }
    
    private IEnumerator DealFlop()
    {
        Debug.Log("Dealing Flop...");
        yield return new WaitForSeconds(delay);
        communityCards.Add(cardDeck.GetAndRemoveRandomCard());
        OnUpdateUI?.Invoke();
        yield return new WaitForSeconds(delay);
        communityCards.Add(cardDeck.GetAndRemoveRandomCard());
        OnUpdateUI?.Invoke();
        yield return new WaitForSeconds(delay);
        communityCards.Add(cardDeck.GetAndRemoveRandomCard());

        AwaitAction();
    }

    private IEnumerator DealTurn()
    {
        Debug.Log("Dealing Turn...");
        yield return new WaitForSeconds(delay);
        communityCards.Add(cardDeck.GetAndRemoveRandomCard());

        AwaitAction();
    }

    private IEnumerator DealRiver()
    {
        Debug.Log("Dealing River...");
        yield return new WaitForSeconds(delay);
        communityCards.Add(cardDeck.GetAndRemoveRandomCard());

        AwaitAction();
    }


    private void EndRound(Player winner) {
        winner.ReceiveWinnings(pot);
        
        int winnerIndex = players.IndexOf(winner);
        string winnerColor = winnerIndex == 0 ? PLAYER1_COLOR : PLAYER2_COLOR;
        AppendToLog($"<color={winnerColor}><b>Player {winnerIndex + 1} wins the pot of {pot}</b></color>");
        AppendToLog($"<color={SYSTEM_COLOR}>-- Round Ended --</color>");
        
        // ResetRound();
        phase = Phase.EndRound;
        AwaitAction();
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
        
        int winnerIndex = players.IndexOf(winner);
        string winnerColor = winnerIndex == 0 ? PLAYER1_COLOR : PLAYER2_COLOR;
        AppendToLog($"<color={winnerColor}><b>Player {winnerIndex + 1} wins with {players[winnerIndex].GetHandRank().RankName}</b></color>");
        
        var cardGroups = players[winnerIndex].GetHandRank().Cards; // string[][]
        string cardLine = $"<color={winnerColor}><b>";
        foreach (var group in cardGroups)
        {
            cardLine += "[";
            cardLine += string.Join(" ", group); // join inner array
            cardLine += "] ";
        }
        cardLine = cardLine.TrimEnd() + "</b></color>";
        AppendToLog(cardLine);

        AppendToLog($"<color={winnerColor}><b>Player {winnerIndex + 1} wins the pot of {pot}</b></color>");
        AppendToLog($"<color={SYSTEM_COLOR}>-- Round Ended --</color>");
        
        // ResetRound();
        phase = Phase.EndRound;
        AwaitAction();
    }
    
    private Player DetermineWinner() {
        // Assuming you have a method to determine the winning player by hand ranking
        double player1Rank = players[0].GetHandRank().Rank;
        double player2Rank = players[1].GetHandRank().Rank;

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

    public void ResetRound() {
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
    
    public int GetCurrentPlayerIndex() => currentPlayer;

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
    private HandValue.HandRank handRank;

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
        currentBet += actualBet;
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

    public void AddToStack(int amount) {
        stack += amount;
    }

    public void AddToBetAmount(int amount) {
        currentBet += amount;
    }
    
    public void EvaluateHand(List<Card> communityCards) {
        List<Card> allCards = new List<Card>(hand);
        allCards.AddRange(communityCards);
        
        handRank = EvaluatePokerHand(allCards);
    }

    public HandValue.HandRank GetHandRank() {
        return handRank;
    }

    private HandValue.HandRank EvaluatePokerHand(List<Card> cards) {
        List<string[]> cardsOnTable = cards.Select(card => card.ToCustomFormat()).ToList();

        return HandValue.GetPlayerHandRank(cardsOnTable);
    }
}


public enum Action {
    None, Check, Bet, Fold, Call
}

public enum Phase {
    Preflop, Flop, Turn, River, EndRound
}