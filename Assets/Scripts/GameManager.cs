using UnityEngine;

namespace Breakout
{
    /// <summary>总控：持有 GameState，连接 UI 与游戏事件。</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = new GameState(3);

        /// <summary>本地模拟的支付服务（下单/回调/发货）。</summary>
        public PaymentService Payments { get; private set; } = new PaymentService();

        [SerializeField] private UIManager ui;
        [SerializeField] private int brickScore = 100;

        /// <summary>购买一条命的价格（金币）。</summary>
        public int LifeCost { get; private set; } = 20;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ui?.Refresh(State);
        }

        /// <summary>运行时注入 HUD 引用（Bootstrap 全代码搭建场景时使用）。</summary>
        public void BindUi(UIManager hud)
        {
            ui = hud;
            ui?.Refresh(State);
        }

        public void OnBrickDestroyed()
        {
            State.AddScore(brickScore);
            ui?.Refresh(State);
        }

        public void OnBallLost()
        {
            bool alive = State.LoseLife();
            ui?.Refresh(State);
            if (!alive) OnGameOver();
        }

        public void BuyLife(int cost)
        {
            if (State.TryPurchase(cost))
            {
                State.AddLife();
                ui?.Toast("购买成功，生命 +1");
            }
            else
            {
                ui?.Toast("金币不足，先去砸砖块吧");
            }
            ui?.Refresh(State);
        }

        private void OnGameOver()
        {
            Debug.Log("Game Over");
            ui?.Toast("游戏结束，按 R 重开");
        }
    }
}
