using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Economy;
using FootballGame.Player;
using FootballGame.Core;
using FootballGame.Audio;

namespace FootballGame.UI
{
    public class MarketUI : MonoBehaviour
    {
        [Header("Token Display")]
        [SerializeField] private TMP_Text _tokenBalanceText;
        [SerializeField] private Button _buyTokensButton;
        [SerializeField] private GameObject _shopPanel;

        [Header("Filter Tabs — Bronze/Silver/Gold/Platinum/Legend")]
        [SerializeField] private Button[] _tierTabButtons;
        [SerializeField] private Image[] _tierTabIndicators;
        [SerializeField] private Color _activeTabColor = Color.yellow;
        [SerializeField] private Color _inactiveTabColor = Color.white;

        [Header("Player List")]
        [SerializeField] private Transform _playerListParent;
        [SerializeField] private GameObject _playerCardPrefab;

        [Header("Player Detail Panel")]
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private TMP_Text _playerPositionText;
        [SerializeField] private TMP_Text _playerOverallText;
        [SerializeField] private TMP_Text _playerNationalityText;
        [SerializeField] private TMP_Text _playerPriceText;
        [SerializeField] private Button _purchaseButton;
        [SerializeField] private TMP_Text _purchaseButtonText;
        [SerializeField] private Slider _speedBar, _shootingBar, _passingBar, _defenseBar, _physicalBar;

        [Header("IAP Shop")]
        [SerializeField] private Button[] _iapButtons;
        [SerializeField] private TMP_Text[] _iapPriceTexts;
        [SerializeField] private TMP_Text[] _iapTokenAmountTexts;
        [SerializeField] private Button _premiumMonthlyBtn;
        [SerializeField] private Button _premiumYearlyBtn;
        [SerializeField] private TMP_Text _premiumMonthlyPrice;
        [SerializeField] private TMP_Text _premiumYearlyPrice;

        private PlayerTier _currentTier = PlayerTier.Bronze;
        private MarketPlayer _selectedMarketPlayer;
        private List<GameObject> _playerCards = new List<GameObject>();

        private readonly PlayerTier[] _tiers = { PlayerTier.Bronze, PlayerTier.Silver, PlayerTier.Gold, PlayerTier.Platinum, PlayerTier.Legend };

        private readonly string[] _iapProductIds =
        {
            IAPManager.TOKENS_100, IAPManager.TOKENS_500,
            IAPManager.TOKENS_1200, IAPManager.TOKENS_2500, IAPManager.TOKENS_6000
        };

        private void Start()
        {
            SetupTierTabs();
            SetupIAPButtons();
            RefreshTokenDisplay();

            if (TokenSystem.Instance != null)
                TokenSystem.Instance.OnBalanceChanged += _ => RefreshTokenDisplay();

            if (IAPManager.Instance != null)
                IAPManager.Instance.OnPurchaseSuccess += _ => RefreshTokenDisplay();

            _buyTokensButton?.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlaySFX(SoundEffect.ButtonClick);
                _shopPanel?.SetActive(true);
            });

            LoadTier(PlayerTier.Bronze);
        }

        private void SetupTierTabs()
        {
            for (int i = 0; i < _tierTabButtons.Length && i < _tiers.Length; i++)
            {
                int idx = i;
                _tierTabButtons[i]?.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySFX(SoundEffect.ButtonClick);
                    LoadTier(_tiers[idx]);
                });
            }
        }

        private void SetupIAPButtons()
        {
            for (int i = 0; i < _iapButtons.Length && i < _iapProductIds.Length; i++)
            {
                string productId = _iapProductIds[i];
                if (_iapPriceTexts != null && i < _iapPriceTexts.Length)
                    _iapPriceTexts[i].text = IAPManager.Instance?.GetProductPrice(productId) ?? "...";

                if (_iapTokenAmountTexts != null && i < _iapTokenAmountTexts.Length)
                {
                    int t = IAPManager.TokenAmounts.TryGetValue(productId, out int tk) ? tk : 0;
                    _iapTokenAmountTexts[i].text = $"{t} Tokens";
                }

                _iapButtons[i]?.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySFX(SoundEffect.ButtonClick);
                    IAPManager.Instance?.BuyTokens(productId);
                });
            }

            _premiumMonthlyBtn?.onClick.AddListener(() => IAPManager.Instance?.BuyPremium(false));
            _premiumYearlyBtn?.onClick.AddListener(() => IAPManager.Instance?.BuyPremium(true));
            if (_premiumMonthlyPrice != null) _premiumMonthlyPrice.text = IAPManager.Instance?.GetProductPrice(IAPManager.PREMIUM_MONTHLY) ?? "...";
            if (_premiumYearlyPrice != null) _premiumYearlyPrice.text = IAPManager.Instance?.GetProductPrice(IAPManager.PREMIUM_YEARLY) ?? "...";
        }

        private void LoadTier(PlayerTier tier)
        {
            _currentTier = tier;
            UpdateTierTabs();
            ClearPlayerCards();

            var players = MarketSystem.Instance?.FilterByTier(tier) ?? new List<MarketPlayer>();
            foreach (var mp in players)
            {
                var card = Instantiate(_playerCardPrefab, _playerListParent);
                SetupPlayerCard(card, mp);
                _playerCards.Add(card);
            }
            _detailPanel?.SetActive(false);
        }

        private void SetupPlayerCard(GameObject card, MarketPlayer mp)
        {
            var p = mp.player;
            card.transform.Find("NameText")?.GetComponent<TMP_Text>()?.SetText(p?.Name ?? "");
            card.transform.Find("PositionText")?.GetComponent<TMP_Text>()?.SetText(p?.Position ?? "");
            card.transform.Find("OverallText")?.GetComponent<TMP_Text>()?.SetText($"{p?.Overall}");
            card.transform.Find("PriceText")?.GetComponent<TMP_Text>()?.SetText($"{mp.price} 🪙");
            card.transform.Find("NationalityText")?.GetComponent<TMP_Text>()?.SetText(p?.Nationality ?? "");

            card.GetComponent<Button>()?.onClick.AddListener(() => ShowPlayerDetail(mp));
        }

        private void ShowPlayerDetail(MarketPlayer mp)
        {
            AudioManager.Instance?.PlaySFX(SoundEffect.MenuOpen);
            _selectedMarketPlayer = mp;
            _detailPanel?.SetActive(true);

            var p = mp.player;
            if (_playerNameText != null) _playerNameText.text = p?.Name ?? "";
            if (_playerPositionText != null) _playerPositionText.text = p?.Position ?? "";
            if (_playerOverallText != null) _playerOverallText.text = $"{p?.Overall}";
            if (_playerNationalityText != null) _playerNationalityText.text = p?.Nationality ?? "";
            if (_playerPriceText != null) _playerPriceText.text = $"{mp.price} 🪙";

            if (p != null)
            {
                SetStatBar(_speedBar, p.Speed);
                SetStatBar(_shootingBar, p.Shooting);
                SetStatBar(_passingBar, p.Passing);
                SetStatBar(_defenseBar, p.Defending);
                SetStatBar(_physicalBar, p.Physical);
            }

            int balance = TokenSystem.Instance?.Balance ?? 0;
            bool canAfford = balance >= mp.price;
            if (_purchaseButton != null) _purchaseButton.interactable = canAfford && !mp.IsExpired;
            if (_purchaseButtonText != null)
                _purchaseButtonText.text = !canAfford
                    ? LocalizationManager.Instance?.Get("not_enough_tokens") ?? "Not Enough Tokens"
                    : mp.IsExpired ? "Expired" : LocalizationManager.Instance?.Get("buy") ?? "BUY";

            _purchaseButton?.onClick.RemoveAllListeners();
            _purchaseButton?.onClick.AddListener(OnPurchaseClicked);
        }

        private async void OnPurchaseClicked()
        {
            if (_selectedMarketPlayer == null) return;
            AudioManager.Instance?.PlaySFX(SoundEffect.ButtonClick);
            await MarketSystem.Instance?.BuyMarketPlayer(_selectedMarketPlayer);
            _detailPanel?.SetActive(false);
            RefreshTokenDisplay();
            AudioManager.Instance?.PlaySFX(SoundEffect.PackOpen);
        }

        private void RefreshTokenDisplay()
        {
            int balance = TokenSystem.Instance?.Balance ?? 0;
            if (_tokenBalanceText != null) _tokenBalanceText.text = $"{balance:N0} 🪙";
        }

        private void UpdateTierTabs()
        {
            for (int i = 0; i < _tiers.Length; i++)
            {
                bool active = _tiers[i] == _currentTier;
                if (_tierTabIndicators != null && i < _tierTabIndicators.Length && _tierTabIndicators[i] != null)
                    _tierTabIndicators[i].color = active ? _activeTabColor : _inactiveTabColor;
            }
        }

        private void SetStatBar(Slider bar, int value) { if (bar != null) bar.value = value / 100f; }

        private void ClearPlayerCards()
        {
            foreach (var c in _playerCards) if (c != null) Destroy(c);
            _playerCards.Clear();
        }
    }
}
