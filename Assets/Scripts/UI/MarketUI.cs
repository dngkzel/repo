using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FootballGame.Core;
using FootballGame.Economy;
using FootballGame.Player;
using FootballGame.Audio;

namespace FootballGame.UI
{
    public class MarketUI : MonoBehaviour
    {
        [Header("Header")]
        public TextMeshProUGUI txtTokenBalance;
        public TextMeshProUGUI txtRefreshCost;
        public Button btnRefresh;
        public Button btnBack;

        [Header("Filter")]
        public TMP_Dropdown ddTier;
        public TMP_Dropdown ddPosition;

        [Header("Player List")]
        public Transform listContainer;
        public GameObject playerCardPrefab;
        public ScrollRect listScroll;

        [Header("Buy Confirm Panel")]
        public GameObject confirmPanel;
        public TextMeshProUGUI txtConfirmName;
        public TextMeshProUGUI txtConfirmStats;
        public TextMeshProUGUI txtConfirmPrice;
        public Button btnConfirmBuy;
        public Button btnCancelBuy;

        private MarketSystem _market;
        private List<MarketPlayer> _displayedPlayers = new List<MarketPlayer>();
        private MarketPlayer _pendingBuy;

        private void Start()
        {
            _market = MarketSystem.Instance;

            btnRefresh?.onClick.AddListener(OnRefresh);
            btnBack?.onClick.AddListener(OnBack);
            btnConfirmBuy?.onClick.AddListener(OnConfirmBuy);
            btnCancelBuy?.onClick.AddListener(() => confirmPanel?.SetActive(false));
            ddTier?.onValueChanged.AddListener(_ => RefreshList());
            ddPosition?.onValueChanged.AddListener(_ => RefreshList());

            confirmPanel?.SetActive(false);

            if (_market != null)
            {
                _market.OnMarketRefreshed += OnMarketRefreshed;
                _market.OnPlayerPurchased += OnPlayerPurchased;
                _market.OnPurchaseFailed  += OnPurchaseFailed;
                _market.Initialize();
            }

            if (GameManager.Instance?.TokenSystem != null)
                GameManager.Instance.TokenSystem.OnTokenBalanceChanged += UpdateBalanceDisplay;

            UpdateBalanceDisplay(GameManager.Instance?.CurrentTokenBalance ?? 0);
            UpdateRefreshCostDisplay();
        }

        private void OnDestroy()
        {
            if (_market != null)
            {
                _market.OnMarketRefreshed -= OnMarketRefreshed;
                _market.OnPlayerPurchased -= OnPlayerPurchased;
                _market.OnPurchaseFailed  -= OnPurchaseFailed;
            }
            if (GameManager.Instance?.TokenSystem != null)
                GameManager.Instance.TokenSystem.OnTokenBalanceChanged -= UpdateBalanceDisplay;
        }

        private void OnMarketRefreshed(List<MarketPlayer> _) => RefreshList();
        private void OnPlayerPurchased(MarketPlayer _) { AudioManager.Instance?.PlaySFX(SFX.CoinCollect); confirmPanel?.SetActive(false); RefreshList(); }
        private void OnPurchaseFailed(string msg) { if (txtConfirmPrice) txtConfirmPrice.text = msg; AudioManager.Instance?.PlaySFX(SFX.Boo); }

        private void UpdateBalanceDisplay(int bal)
        {
            if (txtTokenBalance) txtTokenBalance.text = bal.ToString("N0");
        }

        private void UpdateRefreshCostDisplay()
        {
            if (txtRefreshCost == null) return;
            float factor = PremiumSystem.Instance?.GetMarketRefreshDiscount() ?? 1f;
            int cost = Mathf.RoundToInt(MarketSystem.MarketRefreshCost * factor);
            txtRefreshCost.text = cost.ToString();
        }

        private async void OnRefresh()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            if (_market != null) await _market.RefreshMarket(true);
        }

        private void RefreshList()
        {
            if (listContainer == null) return;
            foreach (Transform c in listContainer) Destroy(c.gameObject);

            _displayedPlayers = _market?.GetMarketPlayers() ?? new List<MarketPlayer>();

            if (ddTier != null && ddTier.value > 0)
            {
                var tier = (PlayerTier)(ddTier.value - 1);
                _displayedPlayers = _displayedPlayers.FindAll(mp => mp.player?.GetTier() == tier);
            }

            if (ddPosition != null && ddPosition.value > 0)
            {
                string pos = ddPosition.options[ddPosition.value].text;
                _displayedPlayers = _displayedPlayers.FindAll(mp => mp.player?.Position == pos);
            }

            foreach (var mp in _displayedPlayers)
                CreateCard(mp);
        }

        private void CreateCard(MarketPlayer mp)
        {
            if (playerCardPrefab == null) return;
            var go = Instantiate(playerCardPrefab, listContainer);
            var txts = go.GetComponentsInChildren<TextMeshProUGUI>();
            if (txts.Length > 0) txts[0].text = mp.player?.Name ?? "";
            if (txts.Length > 1) txts[1].text = $"{mp.player?.Position} | OVR {mp.player?.Overall}";
            if (txts.Length > 2) txts[2].text = $"PAC {mp.player?.Speed}  SHO {mp.player?.Shooting}  PAS {mp.player?.Passing}";
            if (txts.Length > 3) txts[3].text = $"DRI {mp.player?.Dribbling}  DEF {mp.player?.Defending}  PHY {mp.player?.Physical}";
            if (txts.Length > 4) txts[4].text = $"{mp.price} T";

            var btn = go.GetComponentInChildren<Button>();
            if (btn) btn.onClick.AddListener(() => OpenConfirm(mp));
        }

        private void OpenConfirm(MarketPlayer mp)
        {
            _pendingBuy = mp;
            if (confirmPanel) confirmPanel.SetActive(true);
            if (txtConfirmName) txtConfirmName.text = mp.player?.Name ?? "";
            if (txtConfirmStats)
                txtConfirmStats.text =
                    $"{mp.player?.Position} | OVR {mp.player?.Overall}\n" +
                    $"PAC {mp.player?.Speed}  SHO {mp.player?.Shooting}  PAS {mp.player?.Passing}\n" +
                    $"DRI {mp.player?.Dribbling}  DEF {mp.player?.Defending}  PHY {mp.player?.Physical}";
            if (txtConfirmPrice) txtConfirmPrice.text = $"{mp.price} Tokens";
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
        }

        private async void OnConfirmBuy()
        {
            if (_pendingBuy == null || _market == null) return;
            if (btnConfirmBuy) btnConfirmBuy.interactable = false;
            await _market.BuyMarketPlayer(_pendingBuy);
            if (btnConfirmBuy) btnConfirmBuy.interactable = true;
        }

        private void OnBack()
        {
            AudioManager.Instance?.PlaySFX(SFX.ButtonClick);
            GameSceneManager.Instance?.LoadScene(SceneName.MainMenu);
        }
    }
}
