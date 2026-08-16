using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DoubleOrNothingManager : MonoBehaviour
{
    public enum CardSuit
    {
        Clubs,
        Diamonds,
        Hearts,
        Spades,
        Joker
    }

    public enum CardRank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14,
        Joker = 99
    }

    public enum ComboRank
    {
        None = 0,
        TwoPair = 1,
        Trips = 2,
        Straight = 3,
        Flush = 4,
        FullHouse = 5,
        Quads = 6,
        StraightFlush = 7,
        FiveOfAKind = 8,
        RoyalFlush = 9
    }

    public struct PlayingCard
    {
        public CardRank rank;
        public CardSuit suit;

        public bool IsJoker => rank == CardRank.Joker || suit == CardSuit.Joker;
        public string RankText => IsJoker ? "Joker" : GetRankText(rank);
        public string SuitText => IsJoker ? "" : suit.ToString();
        public string DisplayName => IsJoker ? "Joker" : $"{RankText} of {SuitText}";

        public PlayingCard(CardRank cardRank, CardSuit cardSuit)
        {
            rank = cardRank;
            suit = cardSuit;
        }
    }

    [System.Serializable]
    public class ComboMultiplier
    {
        public ComboRank combo;
        public int multiplier;
    }

    private const int BetAmount = 10;

    [Header("Pages")]
    [SerializeField] private GameObject firstPage;
    [SerializeField] private GameObject phase1;
    [SerializeField] private GameObject combi;
    [SerializeField] private GameObject phase2;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text cashText;

    [Header("Phase 1")]
    [SerializeField] private PlayingCardSlotUI[] phase1Cards;
    [Range(0f, 1f)]
    [SerializeField] private float jokerChance = 0.04f;
    [SerializeField] private int maxJokersInHand = 1;
    [SerializeField] private TMP_Text phase1InstructionsText;
    [SerializeField] private TMP_Text combiText;
    [SerializeField] private Button rerollButton;
    [SerializeField] private float rerollRevealDuration = 3f;

    [Header("Phase 2")]
    [SerializeField] private PlayingCardSlotUI openCard;
    [SerializeField] private PlayingCardSlotUI[] cardPile;
    [SerializeField] private PlayingCardSlotUI randomCard;
    [SerializeField] private GameObject phase2InstructionsGroup;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private Button highButton;
    [SerializeField] private Button lowButton;
    [Range(0f, 1f)]
    [SerializeField] private float highLowCorrectChance = 0.2f;
    [SerializeField] private float highLowRevealDuration = 3f;
    [SerializeField] private int maxDoubleAttempts = 4;

    [Header("Results")]
    [SerializeField] private GameObject winResultGroup;
    [SerializeField] private GameObject loseResultGroup;
    [SerializeField] private TMP_Text payoutText;
    [SerializeField] private TMP_Text loseMessageText;
    [SerializeField] private GameObject[] notEnoughCashHiddenObjects;
    [SerializeField] private float notEnoughCashDisplayDuration = 2f;

    [Header("Card Visuals")]
    [SerializeField] private Sprite cardBackSprite;
    [SerializeField] private Sprite[] namedCardSprites;

    [Header("Multipliers")]
    [SerializeField] private ComboMultiplier[] comboMultipliers =
    {
        new ComboMultiplier { combo = ComboRank.TwoPair, multiplier = 2 },
        new ComboMultiplier { combo = ComboRank.Trips, multiplier = 2 },
        new ComboMultiplier { combo = ComboRank.Straight, multiplier = 4 },
        new ComboMultiplier { combo = ComboRank.Flush, multiplier = 7 },
        new ComboMultiplier { combo = ComboRank.FullHouse, multiplier = 8 },
        new ComboMultiplier { combo = ComboRank.Quads, multiplier = 15 },
        new ComboMultiplier { combo = ComboRank.StraightFlush, multiplier = 30 },
        new ComboMultiplier { combo = ComboRank.FiveOfAKind, multiplier = 70 },
        new ComboMultiplier { combo = ComboRank.RoyalFlush, multiplier = 100 }
    };

    [Header("Gambling Cost")]
    [SerializeField] private bool useStatAction = false;
    [SerializeField] private string gamblingActionName = "Gamble";
    [SerializeField] private float happinessGain = 25f;
    [SerializeField] private bool advancesTime = true;

    [Header("Office Rules")]
    [SerializeField] private bool returnHomeAtEndOfOfficeDay = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onRoundFinished;

    private readonly List<PlayingCard> hand = new List<PlayingCard>();
    private readonly List<PlayingCard> highLowHistory = new List<PlayingCard>();
    private readonly bool[] keptCards = new bool[5];
    private Coroutine rerollRevealRoutine;
    private Coroutine highLowRevealRoutine;
    private Coroutine notEnoughCashRoutine;
    private PlayingCard currentHighLowCard;
    private PlayingCard revealedHighLowCard;
    private string originalLoseMessage;
    private int currentBet;
    private int currentWinnings;
    private int currentComboMultiplier;
    private int doubleAttempts;
    private int startingDayPhase;
    private ComboRank currentCombo;
    private bool canToggleKeptCards;
    private bool awaitingDoubleDecision;
    private bool highLowStarted;
    private bool shouldReturnHomeAfterRound;
    private bool roundActive;

    private void Awake()
    {
        if (loseMessageText != null)
            originalLoseMessage = loseMessageText.text;

        if (phase1Cards == null) return;

        for (int i = 0; i < phase1Cards.Length; i++)
        {
            if (phase1Cards[i] != null)
                phase1Cards[i].Setup(this, i);
        }
    }

    private void OnEnable()
    {
        OpenBetPanel();
    }

    private void OnDisable()
    {
        if (rerollRevealRoutine != null)
        {
            StopCoroutine(rerollRevealRoutine);
            rerollRevealRoutine = null;
        }

        StopHighLowRevealRoutine();
        StopNotEnoughCashRoutine();
    }

    public void StartFixedBetRound()
    {
        StartRoundWithBet(BetAmount);
    }

    public void StartRoundWithBet(int betAmount)
    {
        StopRerollRevealRoutine();
        StopHighLowRevealRoutine();
        StopNotEnoughCashRoutine();

        if (GameManager.Instance != null && !GameManager.Instance.TrySpendMoney(betAmount))
        {
            Debug.Log("[DOUBLE OR NOTHING] Not enough money.");
            UpdateCashText();
            ReturnToFirstPage();
            ShowNotEnoughCashWarning();
            return;
        }

        if (GameManager.Instance != null)
            startingDayPhase = GameManager.Instance.CurrentDayPhase;

        currentBet = betAmount;
        currentWinnings = 0;
        currentComboMultiplier = 0;
        currentCombo = ComboRank.None;
        doubleAttempts = 0;
        awaitingDoubleDecision = false;
        highLowStarted = false;
        shouldReturnHomeAfterRound = false;
        roundActive = true;
        canToggleKeptCards = true;
        DealInitialHand();
        UpdateCashText();
        ShowDrawPanel();
    }

    public void ToggleKeepCard(int cardIndex)
    {
        if (!canToggleKeptCards || cardIndex < 0 || cardIndex >= keptCards.Length) return;

        keptCards[cardIndex] = !keptCards[cardIndex];
        RefreshCardSlots();
        UpdateCurrentComboDisplay();
    }

    public void RerollUnkeptCards()
    {
        if (!roundActive || !canToggleKeptCards) return;

        canToggleKeptCards = false;
        DrawReplacementCards();
        RefreshCardSlots();
        UpdateCurrentComboDisplay();

        if (rerollButton != null)
            rerollButton.interactable = false;

        if (phase1InstructionsText != null)
            phase1InstructionsText.text = "Checking hand...";

        if (rerollRevealRoutine != null)
            StopCoroutine(rerollRevealRoutine);

        rerollRevealRoutine = StartCoroutine(EvaluateAfterRerollReveal());
    }

    public void CashOut()
    {
        if (!roundActive) return;

        EndRound(currentWinnings, "Cashed out.");
    }

    public void StartDoubleOrNothing()
    {
        if (!roundActive || currentWinnings <= 0) return;

        StopHighLowRevealRoutine();
        doubleAttempts = 0;
        awaitingDoubleDecision = false;
        highLowStarted = true;
        currentHighLowCard = DrawRandomNormalCard(null);
        highLowHistory.Clear();
        ShowHighLowPanel();
    }

    public void ContinueDoubleOrNothing()
    {
        if (roundActive && currentWinnings > 0 && !highLowStarted)
        {
            StartDoubleOrNothing();
            return;
        }

        if (!roundActive || !awaitingDoubleDecision || currentWinnings <= 0) return;

        highLowHistory.Add(currentHighLowCard);
        currentHighLowCard = revealedHighLowCard;
        awaitingDoubleDecision = false;

        if (resultPanel != null)
            resultPanel.SetActive(false);

        RefreshHighLowPanel();
    }

    public void GuessHigher()
    {
        ResolveHighLowGuess(true);
    }

    public void GuessLower()
    {
        ResolveHighLowGuess(false);
    }

    public void ReturnToFirstPage()
    {
        OpenBetPanel();
    }

    private void OpenBetPanel()
    {
        StopRerollRevealRoutine();
        StopHighLowRevealRoutine();
        StopNotEnoughCashRoutine();
        roundActive = false;
        canToggleKeptCards = false;
        hand.Clear();
        highLowHistory.Clear();
        awaitingDoubleDecision = false;
        highLowStarted = false;
        SetPanels(true, false, false, false, false);
        SetComboTexts(false);
        SetResultGroups(false, false);

        if (randomCard != null)
            randomCard.ShowBack(GetCardBackSprite());

        RefreshHighLowPile();
        UpdateCashText();
    }

    private void StopRerollRevealRoutine()
    {
        if (rerollRevealRoutine == null) return;

        StopCoroutine(rerollRevealRoutine);
        rerollRevealRoutine = null;
    }

    private void StopHighLowRevealRoutine()
    {
        if (highLowRevealRoutine == null) return;

        StopCoroutine(highLowRevealRoutine);
        highLowRevealRoutine = null;
    }

    private void DealInitialHand()
    {
        hand.Clear();

        for (int i = 0; i < keptCards.Length; i++)
            keptCards[i] = false;

        while (hand.Count < 5)
            hand.Add(DrawRandomCard(hand));
    }

    private void DrawReplacementCards()
    {
        for (int i = 0; i < hand.Count; i++)
        {
            if (keptCards[i]) continue;

            hand[i] = DrawRandomCard(hand, i);
        }
    }

    private PlayingCard DrawRandomCard(List<PlayingCard> existingCards, int replacingIndex = -1)
    {
        int currentJokers = CountJokers(existingCards, replacingIndex);

        if (currentJokers < maxJokersInHand && Random.value < jokerChance)
            return new PlayingCard(CardRank.Joker, Random.value < 0.5f ? CardSuit.Clubs : CardSuit.Spades);

        return DrawRandomNormalCard(existingCards, replacingIndex);
    }

    private PlayingCard DrawRandomNormalCard(List<PlayingCard> existingCards, int replacingIndex = -1)
    {
        List<PlayingCard> availableCards = BuildNormalDeck();

        if (existingCards != null)
        {
            for (int i = availableCards.Count - 1; i >= 0; i--)
            {
                if (ContainsExactCard(existingCards, availableCards[i], replacingIndex))
                    availableCards.RemoveAt(i);
            }
        }

        if (availableCards.Count <= 0)
            return new PlayingCard(CardRank.Ace, CardSuit.Spades);

        return availableCards[Random.Range(0, availableCards.Count)];
    }

    private void EvaluateFinalHand()
    {
        currentCombo = GetBestCombo(hand);
        currentComboMultiplier = GetComboMultiplier(currentCombo);

        if (currentCombo == ComboRank.None || currentComboMultiplier <= 0)
        {
            EndRound(0, "Too bad...");
            return;
        }

        currentWinnings = currentBet * currentComboMultiplier;
        ShowComboPanel();
        ShowWinResult();
    }

    private IEnumerator EvaluateAfterRerollReveal()
    {
        yield return new WaitForSeconds(rerollRevealDuration);

        rerollRevealRoutine = null;
        EvaluateFinalHand();
    }

    private void ResolveHighLowGuess(bool guessedHigher)
    {
        if (!roundActive || currentWinnings <= 0 || awaitingDoubleDecision) return;

        StopHighLowRevealRoutine();
        PlayingCard revealedCard = DrawRiggedHighLowCard(currentHighLowCard, guessedHigher, out bool isCorrect);
        revealedHighLowCard = revealedCard;

        if (randomCard != null)
            randomCard.Show(revealedCard, false, false, GetCardSprite(revealedCard));

        SetHighLowButtonsInteractable(false);
        highLowRevealRoutine = StartCoroutine(ResolveHighLowAfterReveal(isCorrect));
    }

    private IEnumerator ResolveHighLowAfterReveal(bool isCorrect)
    {
        yield return new WaitForSeconds(highLowRevealDuration);

        highLowRevealRoutine = null;

        if (!isCorrect)
        {
            EndRound(-currentWinnings, "Too bad...");
            yield break;
        }

        currentWinnings *= 2;
        doubleAttempts++;

        if (doubleAttempts >= maxDoubleAttempts)
        {
            EndRound(currentWinnings, "Maximum doubles reached.");
            yield break;
        }

        awaitingDoubleDecision = true;
        if (rewardText != null)
            rewardText.text = $"Reward: ${currentWinnings}";

        ShowWinResult();
    }

    private PlayingCard DrawDifferentRankCard(PlayingCard currentCard)
    {
        PlayingCard revealedCard = DrawRandomNormalCard(null);
        int safety = 0;

        while (revealedCard.rank == currentCard.rank && safety < 100)
        {
            revealedCard = DrawRandomNormalCard(null);
            safety++;
        }

        return revealedCard;
    }

    private PlayingCard DrawRiggedHighLowCard(PlayingCard currentCard, bool guessedHigher, out bool isCorrect)
    {
        List<PlayingCard> correctCards = BuildHighLowDeck(currentCard, guessedHigher);
        List<PlayingCard> wrongCards = BuildHighLowDeck(currentCard, !guessedHigher);
        bool shouldBeCorrect = Random.value < highLowCorrectChance;

        if (shouldBeCorrect && correctCards.Count > 0)
        {
            isCorrect = true;
            return correctCards[Random.Range(0, correctCards.Count)];
        }

        if (wrongCards.Count > 0)
        {
            isCorrect = false;
            return wrongCards[Random.Range(0, wrongCards.Count)];
        }

        if (correctCards.Count > 0)
        {
            isCorrect = true;
            return correctCards[Random.Range(0, correctCards.Count)];
        }

        isCorrect = false;
        return DrawDifferentRankCard(currentCard);
    }

    private List<PlayingCard> BuildHighLowDeck(PlayingCard currentCard, bool wantsHigher)
    {
        List<PlayingCard> availableCards = BuildNormalDeck();
        int currentRank = (int)currentCard.rank;

        for (int i = availableCards.Count - 1; i >= 0; i--)
        {
            int cardRank = (int)availableCards[i].rank;
            bool isValidCard = wantsHigher ? cardRank > currentRank : cardRank < currentRank;

            if (!isValidCard)
                availableCards.RemoveAt(i);
        }

        return availableCards;
    }

    private void EndRound(int payout, string message)
    {
        if (!roundActive) return;

        roundActive = false;
        canToggleKeptCards = false;
        awaitingDoubleDecision = false;

        if (payout != 0)
            GameManager.Instance?.ChangeMoney(payout);

        ApplyGamblingConsequences();
        UpdateCashText();
        ShowEndResult(payout);
        onRoundFinished?.Invoke();

        if (shouldReturnHomeAfterRound)
            InteractionUIManager.Instance?.ReturnHomeFromOfficeWithFade();
    }

    private void ApplyGamblingConsequences()
    {
        if (GameManager.Instance == null) return;

        int phaseCost = GameManager.Instance.GetActionPhaseCost();

        if (useStatAction)
            GameManager.Instance.ApplyActionStatsByName(gamblingActionName);
        else
            GameManager.Instance.ChangeHappiness(happinessGain);

        if (advancesTime && GameManager.Instance.CurrentDayPhase == startingDayPhase)
            GameManager.Instance.AdvanceTimePhases(phaseCost);

        shouldReturnHomeAfterRound = returnHomeAtEndOfOfficeDay &&
            GameManager.Instance.ShouldReturnHomeFromOffice;
    }

    private void ShowDrawPanel()
    {
        SetPanels(false, true, true, false, false);
        SetComboTexts(true);

        if (phase1InstructionsText != null)
            phase1InstructionsText.text = "Pick cards to keep, then reroll the rest.";

        if (rerollButton != null)
            rerollButton.interactable = true;

        RefreshCardSlots();
        UpdateCurrentComboDisplay();
    }

    private void ShowComboPanel()
    {
        SetPanels(false, true, true, false, false);
        SetComboTexts(true);

        if (combiText != null)
            combiText.text = $"{GetComboDisplayName(currentCombo)} x{currentComboMultiplier}";

        if (phase1InstructionsText != null)
            phase1InstructionsText.text = "Cash out or try to double it.";

        if (rerollButton != null)
            rerollButton.interactable = false;
    }

    private void ShowWinResult()
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        SetResultGroups(true, false);

        if (payoutText != null)
            payoutText.text = $"current payout: ${currentWinnings}";
    }

    private void UpdateCurrentComboDisplay()
    {
        if (combiText != null)
            combiText.text = GetCurrentHandDisplayName();
    }

    private void ShowHighLowPanel()
    {
        SetPanels(false, false, false, true, false);

        if (randomCard != null)
            randomCard.ShowBack(GetCardBackSprite());

        RefreshHighLowPanel();
    }

    private void RefreshHighLowPanel()
    {
        if (openCard != null)
            openCard.Show(currentHighLowCard, false, false, GetCardSprite(currentHighLowCard));

        RefreshHighLowPile();

        if (phase2InstructionsGroup != null)
            phase2InstructionsGroup.SetActive(true);

        if (rewardText != null)
            rewardText.text = $"Reward: ${currentWinnings}";

        if (randomCard != null && !awaitingDoubleDecision)
            randomCard.ShowBack(GetCardBackSprite());

        SetHighLowButtonsInteractable(!awaitingDoubleDecision && roundActive);
    }

    private void ShowEndResult(int payout)
    {
        RestoreNotEnoughCashObjects();

        if (payout > 0)
        {
            OpenBetPanel();
            return;
        }

        if (resultPanel != null)
            resultPanel.SetActive(true);

        SetResultGroups(false, true);
    }

    private void ShowNotEnoughCashWarning()
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        SetResultGroups(false, true);
        SetNotEnoughCashObjectsHidden(true);

        if (loseMessageText != null)
            loseMessageText.text = "Not Enough Cash";

        notEnoughCashRoutine = StartCoroutine(HideNotEnoughCashWarningAfterDelay());
    }

    private IEnumerator HideNotEnoughCashWarningAfterDelay()
    {
        yield return new WaitForSeconds(notEnoughCashDisplayDuration);

        notEnoughCashRoutine = null;
        RestoreNotEnoughCashObjects();

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void StopNotEnoughCashRoutine()
    {
        if (notEnoughCashRoutine != null)
        {
            StopCoroutine(notEnoughCashRoutine);
            notEnoughCashRoutine = null;
        }

        RestoreNotEnoughCashObjects();
    }

    private void RestoreNotEnoughCashObjects()
    {
        SetNotEnoughCashObjectsHidden(false);

        if (loseMessageText != null && !string.IsNullOrEmpty(originalLoseMessage))
            loseMessageText.text = originalLoseMessage;
    }

    private void SetNotEnoughCashObjectsHidden(bool isHidden)
    {
        if (notEnoughCashHiddenObjects == null) return;

        foreach (GameObject hiddenObject in notEnoughCashHiddenObjects)
        {
            if (hiddenObject != null)
                hiddenObject.SetActive(!isHidden);
        }
    }

    private void RefreshHighLowPile()
    {
        if (cardPile == null) return;

        for (int i = 0; i < cardPile.Length; i++)
        {
            PlayingCardSlotUI pileSlot = cardPile[i];

            if (pileSlot == null) continue;

            if (i >= highLowHistory.Count)
            {
                pileSlot.Hide();
                continue;
            }

            PlayingCard historyCard = highLowHistory[i];
            pileSlot.Show(historyCard, false, false, GetCardSprite(historyCard));
        }
    }

    private void SetHighLowButtonsInteractable(bool isInteractable)
    {
        if (highButton != null)
            highButton.interactable = isInteractable;

        if (lowButton != null)
            lowButton.interactable = isInteractable;
    }

    private void RefreshCardSlots()
    {
        if (phase1Cards == null) return;

        for (int i = 0; i < phase1Cards.Length; i++)
        {
            if (phase1Cards[i] == null) continue;

            if (i >= hand.Count)
            {
                phase1Cards[i].Hide();
                continue;
            }

            phase1Cards[i].Show(hand[i], keptCards[i], canToggleKeptCards, GetCardSprite(hand[i]));
        }
    }

    private void SetPanels(bool showBet, bool showDraw, bool showCombo, bool showHighLow, bool showResult)
    {
        if (firstPage != null)
            firstPage.SetActive(showBet);

        if (phase1 != null)
            phase1.SetActive(showDraw);

        if (combi != null)
            combi.SetActive(showCombo);

        if (phase2 != null)
            phase2.SetActive(showHighLow);

        if (resultPanel != null)
            resultPanel.SetActive(showResult);
    }

    private void SetComboTexts(bool isActive)
    {
        if (combiText != null)
            combiText.gameObject.SetActive(isActive);
    }

    private void SetResultGroups(bool showWin, bool showLose)
    {
        if (winResultGroup != null)
            winResultGroup.SetActive(showWin);

        if (loseResultGroup != null)
            loseResultGroup.SetActive(showLose);
    }

    private void UpdateCashText()
    {
        if (cashText == null || GameManager.Instance == null) return;

        float money = GameManager.Instance.Money;
        cashText.text = Mathf.Approximately(money % 1f, 0f) ? $"{money:0}" : $"{money:0.00}";
    }

    private Sprite GetCardSprite(PlayingCard card)
    {
        if (card.IsJoker)
            return GetNamedCardSprite(card);

        return GetNamedCardSprite(card);
    }

    private Sprite GetNamedCardSprite(PlayingCard card)
    {
        if (namedCardSprites == null) return null;

        string expectedName = card.IsJoker
            ? card.suit == CardSuit.Clubs ? "Joker_Blue" : "Joker_Red"
            : $"{GetSpriteSuitName(card.suit)}_{GetSpriteRankName(card.rank)}";

        foreach (Sprite namedCardSprite in namedCardSprites)
        {
            if (namedCardSprite == null) continue;

            if (namedCardSprite.name == expectedName)
                return namedCardSprite;
        }

        return null;
    }

    private Sprite GetCardBackSprite()
    {
        return cardBackSprite;
    }

    private ComboRank GetBestCombo(List<PlayingCard> cards)
    {
        int jokerCount = CountJokers(cards);

        if (jokerCount <= 0)
            return EvaluateConcreteHand(cards);

        ComboRank bestCombo = ComboRank.None;
        List<PlayingCard> workingCards = new List<PlayingCard>(cards);
        ReplaceJokersAndEvaluate(workingCards, 0, ref bestCombo);
        return bestCombo;
    }

    private string GetCurrentHandDisplayName()
    {
        ComboRank rewardCombo = GetBestCombo(hand);

        if (rewardCombo != ComboRank.None)
            return GetComboDisplayName(rewardCombo);

        return HasNaturalOnePair(hand) ? "One Pair" : "No Pair";
    }

    private bool HasNaturalOnePair(List<PlayingCard> cards)
    {
        Dictionary<CardRank, int> rankCounts = new Dictionary<CardRank, int>();
        int jokerCount = 0;

        foreach (PlayingCard card in cards)
        {
            if (card.IsJoker)
            {
                jokerCount++;
                continue;
            }

            if (!rankCounts.ContainsKey(card.rank))
                rankCounts.Add(card.rank, 0);

            rankCounts[card.rank]++;
        }

        foreach (KeyValuePair<CardRank, int> rankCount in rankCounts)
        {
            if (rankCount.Value >= 2)
                return true;
        }

        return jokerCount > 0 && rankCounts.Count > 0;
    }

    private void ReplaceJokersAndEvaluate(List<PlayingCard> cards, int startIndex, ref ComboRank bestCombo)
    {
        int jokerIndex = -1;

        for (int i = startIndex; i < cards.Count; i++)
        {
            if (cards[i].IsJoker)
            {
                jokerIndex = i;
                break;
            }
        }

        if (jokerIndex < 0)
        {
            ComboRank combo = EvaluateConcreteHand(cards);

            if (combo > bestCombo)
                bestCombo = combo;

            return;
        }

        foreach (CardRank rank in GetNormalRanks())
        {
            foreach (CardSuit suit in GetNormalSuits())
            {
                cards[jokerIndex] = new PlayingCard(rank, suit);
                ReplaceJokersAndEvaluate(cards, jokerIndex + 1, ref bestCombo);

                if (bestCombo == ComboRank.RoyalFlush)
                    return;
            }
        }
    }

    private ComboRank EvaluateConcreteHand(List<PlayingCard> cards)
    {
        Dictionary<CardRank, int> rankCounts = new Dictionary<CardRank, int>();
        bool isFlush = true;
        CardSuit firstSuit = cards[0].suit;

        foreach (PlayingCard card in cards)
        {
            if (!rankCounts.ContainsKey(card.rank))
                rankCounts.Add(card.rank, 0);

            rankCounts[card.rank]++;

            if (card.suit != firstSuit)
                isFlush = false;
        }

        bool isStraight = IsStraight(rankCounts);
        bool isRoyal = isFlush && HasRoyalRanks(rankCounts);

        if (isRoyal)
            return ComboRank.RoyalFlush;

        if (rankCounts.ContainsValue(5))
            return ComboRank.FiveOfAKind;

        if (isFlush && isStraight)
            return ComboRank.StraightFlush;

        if (rankCounts.ContainsValue(4))
            return ComboRank.Quads;

        if (rankCounts.ContainsValue(3) && rankCounts.ContainsValue(2))
            return ComboRank.FullHouse;

        if (isFlush)
            return ComboRank.Flush;

        if (isStraight)
            return ComboRank.Straight;

        if (rankCounts.ContainsValue(3))
            return ComboRank.Trips;

        int pairCount = 0;

        foreach (KeyValuePair<CardRank, int> rankCount in rankCounts)
        {
            if (rankCount.Value == 2)
                pairCount++;
        }

        return pairCount >= 2 ? ComboRank.TwoPair : ComboRank.None;
    }

    private bool IsStraight(Dictionary<CardRank, int> rankCounts)
    {
        if (rankCounts.Count != 5) return false;

        List<int> ranks = new List<int>();

        foreach (CardRank rank in rankCounts.Keys)
            ranks.Add((int)rank);

        ranks.Sort();

        bool normalStraight = true;

        for (int i = 1; i < ranks.Count; i++)
        {
            if (ranks[i] != ranks[i - 1] + 1)
            {
                normalStraight = false;
                break;
            }
        }

        if (normalStraight)
            return true;

        return ranks[0] == 2 &&
            ranks[1] == 3 &&
            ranks[2] == 4 &&
            ranks[3] == 5 &&
            ranks[4] == 14;
    }

    private bool HasRoyalRanks(Dictionary<CardRank, int> rankCounts)
    {
        return rankCounts.ContainsKey(CardRank.Ten) &&
            rankCounts.ContainsKey(CardRank.Jack) &&
            rankCounts.ContainsKey(CardRank.Queen) &&
            rankCounts.ContainsKey(CardRank.King) &&
            rankCounts.ContainsKey(CardRank.Ace);
    }

    private int GetComboMultiplier(ComboRank combo)
    {
        if (comboMultipliers == null) return 0;

        foreach (ComboMultiplier comboMultiplier in comboMultipliers)
        {
            if (comboMultiplier != null && comboMultiplier.combo == combo)
                return comboMultiplier.multiplier;
        }

        return 0;
    }

    private int CountJokers(List<PlayingCard> cards, int ignoredIndex = -1)
    {
        if (cards == null) return 0;

        int jokerCount = 0;

        for (int i = 0; i < cards.Count; i++)
        {
            if (i == ignoredIndex) continue;

            if (cards[i].IsJoker)
                jokerCount++;
        }

        return jokerCount;
    }

    private bool ContainsExactCard(List<PlayingCard> cards, PlayingCard card, int ignoredIndex)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (i == ignoredIndex || cards[i].IsJoker) continue;

            if (cards[i].rank == card.rank && cards[i].suit == card.suit)
                return true;
        }

        return false;
    }

    private List<PlayingCard> BuildNormalDeck()
    {
        List<PlayingCard> deck = new List<PlayingCard>();

        foreach (CardSuit suit in GetNormalSuits())
        {
            foreach (CardRank rank in GetNormalRanks())
                deck.Add(new PlayingCard(rank, suit));
        }

        return deck;
    }

    private static CardRank[] GetNormalRanks()
    {
        return new[]
        {
            CardRank.Two,
            CardRank.Three,
            CardRank.Four,
            CardRank.Five,
            CardRank.Six,
            CardRank.Seven,
            CardRank.Eight,
            CardRank.Nine,
            CardRank.Ten,
            CardRank.Jack,
            CardRank.Queen,
            CardRank.King,
            CardRank.Ace
        };
    }

    private static CardSuit[] GetNormalSuits()
    {
        return new[]
        {
            CardSuit.Clubs,
            CardSuit.Diamonds,
            CardSuit.Hearts,
            CardSuit.Spades
        };
    }

    private static string GetComboDisplayName(ComboRank combo)
    {
        switch (combo)
        {
            case ComboRank.TwoPair:
                return "Two Pair";
            case ComboRank.Trips:
                return "Trips";
            case ComboRank.Straight:
                return "Straight";
            case ComboRank.Flush:
                return "Flush";
            case ComboRank.FullHouse:
                return "Full House";
            case ComboRank.Quads:
                return "Quads";
            case ComboRank.StraightFlush:
                return "Straight Flush";
            case ComboRank.FiveOfAKind:
                return "Five of a Kind";
            case ComboRank.RoyalFlush:
                return "Royal Flush";
            default:
                return "No Combo";
        }
    }

    private static string GetRankText(CardRank rank)
    {
        switch (rank)
        {
            case CardRank.Jack:
                return "J";
            case CardRank.Queen:
                return "Q";
            case CardRank.King:
                return "K";
            case CardRank.Ace:
                return "A";
            default:
                return ((int)rank).ToString();
        }
    }

    private static string GetSpriteSuitName(CardSuit suit)
    {
        switch (suit)
        {
            case CardSuit.Clubs:
                return "Club";
            case CardSuit.Diamonds:
                return "Diamond";
            case CardSuit.Hearts:
                return "Heart";
            case CardSuit.Spades:
                return "Spade";
            default:
                return "Joker";
        }
    }

    private static string GetSpriteRankName(CardRank rank)
    {
        switch (rank)
        {
            case CardRank.Ace:
                return "1";
            case CardRank.Jack:
                return "J";
            case CardRank.Queen:
                return "Q";
            case CardRank.King:
                return "K";
            default:
                return ((int)rank).ToString();
        }
    }
}
