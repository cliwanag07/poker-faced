using System;
using System.Collections.Generic;
using UnityEngine;

public static class HandValue {
    public static int GetHighestHandValue(List<Card> hand, List<Card> communityCards) {
        if (hand.Count != 2)
            throw new ArgumentException("Exactly 2 cards are required for hand.");
        
        if (communityCards.Count is < 0 or > 5)
            throw new ArgumentException("Between 0 and 5 cards are required for community cards.");

        return 0;
    }
}
