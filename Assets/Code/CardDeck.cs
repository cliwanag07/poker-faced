using System;
using System.Collections.Generic;
using UnityEngine;

public class CardDeck {
    private List<Card> cards;

    public CardDeck() {
        ResetDeck();
    }

    public Card GetAndRemoveRandomCard() {
        var card = cards[UnityEngine.Random.Range(0, cards.Count)];
        cards.Remove(card);
        return card;
    }

    public Card GetRandomCard() {
        return cards[UnityEngine.Random.Range(0, cards.Count)];
    }

    public void ResetDeck() {
        cards.Clear();
        foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit))) {
            foreach (CardValue value in Enum.GetValues(typeof(CardValue))) {
                cards.Add(new Card(suit, value));
            }
        }
    }
}

public class Card {
    public CardSuit Suit { get; private set; }
    public CardValue Value { get; private set; }
    public string ResourcePath { get; private set; }

    public Card(CardSuit suit, CardValue value) {
        Suit = suit;
        Value = value;
        ResourcePath = $"{suit} {value.ToSymbol()}";
    }

    public string GetSymbol() => Value.ToSymbol();
    
    public Sprite Sprite => Resources.Load<Sprite>(ResourcePath);
    
    // custom format for PokerHandEvaluator
    public string[] ToCustomFormat() {
        int valueAsInt = Value switch {
            CardValue.Jack => 11,
            CardValue.Queen => 12,
            CardValue.King => 13,
            CardValue.Ace => 14,
            _ => (int)Value + 1 // enum starts at 0
        };

        string suitString = Suit switch {
            CardSuit.Spades => "Spade",
            CardSuit.Hearts => "Heart",
            CardSuit.Diamonds => "Diamond",
            CardSuit.Clubs => "Club",
            _ => "Unknown"
        };

        return new string[] { valueAsInt.ToString(), suitString };
    }

}

public enum CardSuit {
    Spades, Hearts, Diamonds, Clubs
}

public enum CardValue {
    Ace, Two, Three, Four, Five, Six, Seven,
    Eight, Nine, Ten, Jack, Queen, King
}

public static class CardValueExtensions {
    public static string ToSymbol(this CardValue value) {
        return value switch {
            CardValue.Ace => "A",
            CardValue.Two => "2",
            CardValue.Three => "3",
            CardValue.Four => "4",
            CardValue.Five => "5",
            CardValue.Six => "6",
            CardValue.Seven => "7",
            CardValue.Eight => "8",
            CardValue.Nine => "9",
            CardValue.Ten => "10",
            CardValue.Jack => "J",
            CardValue.Queen => "Q",
            CardValue.King => "K",
            _ => "?"
        };
    }
}