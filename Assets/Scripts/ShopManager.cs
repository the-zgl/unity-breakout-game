using UnityEngine;

namespace Breakout
{
    /// <summary>模拟商店 / 支付入口（本地模拟，不含真实支付 SDK）。</summary>
    public class ShopManager : MonoBehaviour
    {
        [SerializeField] private int lifeCost = 20;

        public int LifeCost => lifeCost;

        /// <summary>购买一条命（模拟支付，走 GameState.TryPurchase）。</summary>
        public void BuyExtraLife()
        {
            GameManager.Instance?.BuyLife(lifeCost);
        }
    }
}
