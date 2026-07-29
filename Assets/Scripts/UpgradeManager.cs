using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeLine
    {
        public string upgradeName;
        public GameObject[] tierObjects;
        public string[] tierNames;
        public int[] upgradeCosts;
        public int currentTier;
        public Button upgradeButton;
        public TMP_Text upgradeButtonText;
        public TMP_Text priceText;
    }

    [Header("Upgrade Lines")]
    [SerializeField] private UpgradeLine[] upgradeLines;

    private void Start()
    {
        RefreshAllUpgradeVisuals();
        UpdateHomeWorkMultiplier();
    }

    public void BuyUpgrade(int upgradeIndex)
    {
        if (upgradeLines == null ||
            upgradeIndex < 0 ||
            upgradeIndex >= upgradeLines.Length)
        {
            Debug.LogWarning($"[UPGRADE] No upgrade line found at index {upgradeIndex}.");
            return;
        }

        BuyUpgrade(upgradeLines[upgradeIndex]);
    }

    public void BuyUpgradeByName(string upgradeName)
    {
        if (upgradeLines == null) return;

        foreach (UpgradeLine upgradeLine in upgradeLines)
        {
            if (upgradeLine != null && upgradeLine.upgradeName == upgradeName)
            {
                BuyUpgrade(upgradeLine);
                return;
            }
        }

        Debug.LogWarning($"[UPGRADE] No upgrade line named {upgradeName}.");
    }

    private void BuyUpgrade(UpgradeLine upgradeLine)
    {
        if (upgradeLine == null) return;

        int nextTier = upgradeLine.currentTier + 1;

        if (upgradeLine.tierObjects == null ||
            nextTier >= upgradeLine.tierObjects.Length)
        {
            Debug.Log($"[UPGRADE] {upgradeLine.upgradeName} is already max tier.");
            return;
        }

        if (upgradeLine.upgradeCosts == null ||
            upgradeLine.currentTier >= upgradeLine.upgradeCosts.Length)
        {
            Debug.LogWarning($"[UPGRADE] Missing cost for {upgradeLine.upgradeName} tier {nextTier}.");
            return;
        }

        int upgradeCost = upgradeLine.upgradeCosts[upgradeLine.currentTier];

        if (GameManager.Instance == null ||
            !GameManager.Instance.TrySpendMoney(upgradeCost))
        {
            Debug.Log($"[UPGRADE] Failed to buy {upgradeLine.upgradeName} tier {nextTier}.");
            return;
        }

        upgradeLine.currentTier = nextTier;
        RefreshUpgradeLine(upgradeLine);
        UpdateHomeWorkMultiplier();

        Debug.Log($"[UPGRADE] Bought {upgradeLine.upgradeName} tier {upgradeLine.currentTier} for ${upgradeCost}.");
    }

    private void RefreshAllUpgradeVisuals()
    {
        if (upgradeLines == null) return;

        foreach (UpgradeLine upgradeLine in upgradeLines)
            RefreshUpgradeLine(upgradeLine);
    }

    private void RefreshUpgradeLine(UpgradeLine upgradeLine)
    {
        RefreshUpgradeVisuals(upgradeLine);
        RefreshUpgradeUI(upgradeLine);
    }

    private void RefreshUpgradeVisuals(UpgradeLine upgradeLine)
    {
        if (upgradeLine == null || upgradeLine.tierObjects == null) return;

        int activeTier = Mathf.Clamp(upgradeLine.currentTier, 0, upgradeLine.tierObjects.Length - 1);

        for (int i = 0; i < upgradeLine.tierObjects.Length; i++)
        {
            if (upgradeLine.tierObjects[i] != null)
                upgradeLine.tierObjects[i].SetActive(i == activeTier);
        }
    }

    private void RefreshUpgradeUI(UpgradeLine upgradeLine)
    {
        if (upgradeLine == null) return;

        int nextTier = upgradeLine.currentTier + 1;
        bool hasNextTier = upgradeLine.tierObjects != null && nextTier < upgradeLine.tierObjects.Length;
        bool hasNextCost = upgradeLine.upgradeCosts != null && upgradeLine.currentTier < upgradeLine.upgradeCosts.Length;

        if (!hasNextTier)
        {
            if (upgradeLine.upgradeButtonText != null)
                upgradeLine.upgradeButtonText.text = "Max";

            if (upgradeLine.priceText != null)
                upgradeLine.priceText.text = "";

            if (upgradeLine.upgradeButton != null)
                upgradeLine.upgradeButton.interactable = false;

            return;
        }

        if (upgradeLine.upgradeButton != null)
            upgradeLine.upgradeButton.interactable = hasNextCost;

        string nextTierName = GetTierName(upgradeLine, nextTier);

        if (upgradeLine.upgradeButtonText != null)
            upgradeLine.upgradeButtonText.text = nextTierName;

        if (upgradeLine.priceText != null)
            upgradeLine.priceText.text = hasNextCost ? $"${upgradeLine.upgradeCosts[upgradeLine.currentTier]}" : "No price";
    }

    private string GetTierName(UpgradeLine upgradeLine, int tierIndex)
    {
        if (upgradeLine.tierNames != null &&
            tierIndex >= 0 &&
            tierIndex < upgradeLine.tierNames.Length &&
            !string.IsNullOrWhiteSpace(upgradeLine.tierNames[tierIndex]))
        {
            return upgradeLine.tierNames[tierIndex];
        }

        return $"{upgradeLine.upgradeName} Tier {tierIndex}";
    }

    private void UpdateHomeWorkMultiplier()
    {
        if (GameManager.Instance == null || upgradeLines == null) return;

        int purchasedTiers = 0;
        int purchasableTiers = 0;

        foreach (UpgradeLine upgradeLine in upgradeLines)
        {
            if (upgradeLine == null || upgradeLine.tierObjects == null) continue;

            int maxPurchasableTier = Mathf.Max(0, upgradeLine.tierObjects.Length - 1);
            purchasedTiers += Mathf.Clamp(upgradeLine.currentTier, 0, maxPurchasableTier);
            purchasableTiers += maxPurchasableTier;
        }

        float progress = purchasableTiers > 0
            ? (float)purchasedTiers / purchasableTiers
            : 0f;

        GameManager.Instance.SetHouseUpgradeProgress(progress);
    }
}
