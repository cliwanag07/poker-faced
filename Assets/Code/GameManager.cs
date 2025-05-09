using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour {
    [SerializeField] private TexasHoldemUIManager texasHoldemUIManager;
    [SerializeField] private TexasHoldemManager texasHoldemManager;
    [SerializeField] private FaceCamSquareCrop faceCamSquareCrop;
    [SerializeField] private MrChipsEmojiController emojiController;
    [SerializeField] private AICaller aICaller;

    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private TextMeshProUGUI loadingText;
    
    private const int PLAYER_INDEX = 0;
    private const int COMPUTER_INDEX = 1;
    private bool showComputerHand = false;
    private bool roundEnd = false;
    private bool serverConnected = false;
    private bool cameraDetected = false;
    
    [SerializeField] private float minAIResponseDelay = 3f; // minimum delay in seconds
    [SerializeField] private float maxAIResponseDelay = 11f; // maximum delay in seconds

    private float AIResponse;

    private void Start() {
        texasHoldemUIManager.OnPlayerAction += HandlePlayerAction;
        
        texasHoldemManager.OnAwaitingNextAction += HandleAwaitingNextAction;
        texasHoldemManager.OnUpdateUI += UpdateUI;
        texasHoldemManager.OnPlayerWin += HandlePlayerWin;
        aICaller.OnApiResponseReceived += HandleApiResponse;
        
        StartCoroutine(InitializeGame());
    }
    
    private IEnumerator InitializeGame() {
        loadingScreen.SetActive(true);
        loadingText.text = "Waiting for server response...";
        
        // Check server connection with timeout
        yield return StartCoroutine(CheckServerConnection());
        
        if (!serverConnected) {
            loadingText.text = "Server connection failed, see README.txt";
            yield break;
        }
        
        // Check camera availability
        yield return StartCoroutine(CheckCamera());
        
        if (!cameraDetected) {
            loadingText.text = "Camera not detected, connect camera and restart";
            yield break;
        }
        
        // Both server and camera are ready
        loadingScreen.SetActive(false);
        texasHoldemManager.CreateRoom();
        texasHoldemManager.StartNewRound();
    }
    
    private IEnumerator CheckServerConnection() 
    {
        float timeout = 30f;
        float elapsedTime = 0f;
        bool connectionSuccess = false;
    
        loadingText.text = "Waiting for server response...";
    
        while (elapsedTime < timeout && !connectionSuccess) 
        {
            yield return aICaller.PingServer((success) => {
                connectionSuccess = success;
            });
        
            if (!connectionSuccess) 
            {
                elapsedTime += 1f;
                loadingText.text = $"Waiting for server response... {Mathf.RoundToInt(timeout - elapsedTime)}s remaining";
                yield return new WaitForSeconds(1f);
            }
        }
    
        serverConnected = connectionSuccess;
    
        if (!serverConnected)
        {
            loadingText.text = "Server connection failed, see README.txt";
        }
    }
    
    private IEnumerator CheckCamera() {
        // Wait for camera initialization if needed
        if (faceCamSquareCrop != null) {
            float timeout = 5f;
            float elapsedTime = 0f;
            
            while (elapsedTime < timeout && !faceCamSquareCrop.IsInitialized()) {
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            cameraDetected = faceCamSquareCrop.IsInitialized();
        } else {
            cameraDetected = false;
        }
    }

    private void Update() {
        showComputerHand = roundEnd = texasHoldemManager.Phase == Phase.EndRound;

        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (texasHoldemManager.Phase != Phase.EndRound) return;
        if (texasHoldemManager.Players[PLAYER_INDEX].GetStack() <= 0 ||
            texasHoldemManager.Players[COMPUTER_INDEX].GetStack() <= 0) {
            texasHoldemManager.ResetRoom();
            UpdateUI();
        } else {
            texasHoldemManager.ResetRound();
            UpdateUI();
        }

        // UpdateUI();
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
#if UNITY_EDITOR
            Debug.Log("Waiting for action from Player");
#endif
            emojiController.SetEmotion(Emotion.Idle);
        }
        else {
#if UNITY_EDITOR
            Debug.Log("Waiting for action from Computer");
#endif
            emojiController.SetEmotion(Emotion.Thinking);
            
            var cardInfo = GetAICardInfo();
            var cards = cardInfo.cardValNumbers;
            Debug.Log(string.Join(", ", cardInfo.cardValNumbers));
            var suits = cardInfo.cardSuits;
            Debug.Log(string.Join(", ", cardInfo.cardSuits));
            var ratio = cardInfo.computerChipsToUserRatio;
            
            StartCoroutine(HandleAIDelay(cards, suits, ratio));
        }
    }
    
    private IEnumerator HandleAIDelay(List<int> cards, List<string> suits, double ratio) {
        // wait for a random delay between min and max time
        float delay = UnityEngine.Random.Range(minAIResponseDelay, maxAIResponseDelay);
        yield return new WaitForSeconds(delay);

        // proceed to get AI response after the delay
        aICaller.GetAIResponse(cards, suits, ratio, GetWebCamImage());
    }
    
    private void HandleApiResponse(float response) {
#if UNITY_EDITOR
        Debug.Log(response);
#endif

        var computer = texasHoldemManager.Players[COMPUTER_INDEX];
        var player = texasHoldemManager.Players[PLAYER_INDEX];
        var pot = texasHoldemManager.Pot;

        var userLastAction = player.GetAction();
        int compStack = computer.GetStack();
        int compBet = computer.GetCurrentBet();
        int playerBet = player.GetCurrentBet();
        int playerStack = player.GetStack();

        float rand = UnityEngine.Random.value;

        // stack awareness
        bool playerIsShort = playerStack < compStack * 0.4f;
        bool compIsShort = compStack < playerStack * 0.4f;
        bool playerPotCommitted = playerBet >= playerStack * 0.9f;

        // bluffing logic
        bool bluffRaise = (response < 0.3f && rand < 0.06f && !playerPotCommitted) ||
                          (response < 0.6f && playerIsShort && rand < 0.2f);  // bully short player

        bool bluffCall = response < 0.1f && rand < 0.15f && !playerPotCommitted;

        // hand strength
        bool isStrong = response > 0.75f || (response > 0.6f && rand > 0.8f);
        bool isGood = response > 0.5f || (response > 0.4f && rand > 0.7f);
        bool isMediocre = response > 0.25f;

        Action DecideRaise() => Action.Bet;
        Action DecideCallOrCheck() =>
            userLastAction == Action.Bet ? Action.Call : Action.Check;
        Action DecideFoldOrCheck() =>
            userLastAction == Action.Bet ? Action.Fold : Action.Check;

        bool canRaise = compStack > pot;
        bool playerAllIn = player.GetIsAllIn();

        // if player is all-in AI can only call or fold
        if (playerAllIn) {
            bool shouldCall = isGood || isMediocre || bluffCall;
            if (shouldCall) {
                texasHoldemManager.HandlePlayerAction(Action.Call);
            } else {
                texasHoldemManager.HandlePlayerAction(Action.Fold);
            }
            return;
        }

        // regular decision logic
        switch (userLastAction) {
            case Action.Bet:
                if ((isStrong && canRaise) || bluffRaise) {
                    texasHoldemManager.HandlePlayerAction(DecideRaise(), Math.Min(pot, compStack));
                } else if (isGood || bluffCall) {
                    texasHoldemManager.HandlePlayerAction(DecideCallOrCheck());
                } else if (isMediocre && compBet < compStack * 0.25f) {
                    texasHoldemManager.HandlePlayerAction(DecideCallOrCheck());
                } else {
                    texasHoldemManager.HandlePlayerAction(DecideFoldOrCheck());
                }
                break;

            case Action.Check:
                if ((response > 0.65f || bluffRaise) && canRaise) {
                    texasHoldemManager.HandlePlayerAction(DecideRaise(), Math.Min(pot, compStack));
                } else {
                    texasHoldemManager.HandlePlayerAction(DecideCallOrCheck());
                }
                break;

            default:
                if (texasHoldemManager.Phase != Phase.Preflop) {
                    if ((isStrong && canRaise) || bluffRaise) {
                        texasHoldemManager.HandlePlayerAction(DecideRaise(), Math.Min(pot, compStack));
                    } else if (isMediocre || bluffCall) {
                        texasHoldemManager.HandlePlayerAction(DecideCallOrCheck());
                    } else {
                        texasHoldemManager.HandlePlayerAction(DecideFoldOrCheck());
                    }
                } else {
                    if (computer.IsSmallBlind) {
                        if ((isStrong && canRaise) || bluffRaise) {
                            texasHoldemManager.HandlePlayerAction(DecideRaise(), Math.Min(pot, compStack));
                        } else if (isGood || isMediocre || bluffCall) {
                            texasHoldemManager.HandlePlayerAction(Action.Call);
                        } else {
                            texasHoldemManager.HandlePlayerAction(Action.Fold);
                        }
                    } else {
                        if ((isStrong && canRaise) || bluffRaise) {
                            texasHoldemManager.HandlePlayerAction(DecideRaise(), Math.Min(pot, compStack));
                        } else {
                            texasHoldemManager.HandlePlayerAction(DecideCallOrCheck());
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
            isRoundEnd = roundEnd,
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
        public  bool isRoundEnd { get; set; }
    }

    public class AICardInfo {
        public List<string> cardValString = new();
        public List<int> cardValNumbers = new();
        public List<string> cardSuits = new();
        public double computerChipsToUserRatio = 0.0;
    }

}
