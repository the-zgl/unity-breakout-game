using UnityEngine;
using UnityEngine.UI;

namespace Breakout
{
    /// <summary>HUD：把 GameState 显示到分数 / 生命 / 金币文本上，并提供提示条。</summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private Text scoreText;
        [SerializeField] private Text livesText;
        [SerializeField] private Text coinsText;
        [SerializeField] private Text toastText;

        /// <summary>运行时注入引用（Bootstrap 全代码搭建场景时使用）。</summary>
        public void Configure(Text score, Text lives, Text coins, Text toast)
        {
            scoreText = score;
            livesText = lives;
            coinsText = coins;
            toastText = toast;
        }

        public void Refresh(GameState state)
        {
            if (state == null) return;
            if (scoreText != null) scoreText.text = "分数 " + state.Score;
            if (livesText != null) livesText.text = "生命 " + state.Lives;
            if (coinsText != null) coinsText.text = "金币 " + state.Coins;
        }

        /// <summary>在屏幕下方显示一条短提示（购买结果、游戏结束等）。</summary>
        public void Toast(string message)
        {
            if (toastText != null) toastText.text = message;
            Debug.Log("[Toast] " + message);
        }
    }
}
