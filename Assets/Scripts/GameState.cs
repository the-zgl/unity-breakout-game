using System;

namespace Breakout
{
    /// <summary>
    /// 纯 C# 游戏状态逻辑（不依赖 MonoBehaviour，便于单元测试）。
    /// 计分、生命、金币、模拟购买都集中在这里。
    ///
    /// 设计意图：把「可测试的业务规则」从 MonoBehaviour 里抽出来，
    /// 单元测试就能直接 new 出来跑，不需要启动 Unity 场景。
    /// </summary>
    public class GameState
    {
        public int Score { get; private set; }
        public int Lives { get; private set; }
        public int Coins { get; private set; }

        /// <summary>本局累计得分，用来正确计算「每 10 分 1 金币」。</summary>
        private int totalPointsThisRun;

        /// <summary>最高分（可作为 PlayPrefs 持久化前的内存态）。</summary>
        public int BestScore { get; private set; }

        public GameState(int lives = 3)
        {
            if (lives < 0) throw new ArgumentOutOfRangeException(nameof(lives));
            Lives = lives;
            BestScore = 0;
        }

        /// <summary>
        /// 加分；每累计 10 分奖励 1 金币。
        ///
        /// 注意这里是按「累计得分」换算，而不是按单次加分整除：
        /// 先加 9 分再加 1 分，也应该拿到 1 金币。
        /// （这个 bug 就是用 tools/logic-smoke 的自检脚本暴露出来的。）
        /// </summary>
        public void AddScore(int points)
        {
            if (points <= 0) return;
            Score += points;
            totalPointsThisRun += points;
            Coins = totalPointsThisRun / 10;
            if (Score > BestScore) BestScore = Score;
        }

        /// <summary>失去一条命；返回是否还活着。</summary>
        public bool LoseLife()
        {
            if (Lives > 0) Lives--;
            return Lives > 0;
        }

        public void AddLife()
        {
            Lives++;
        }

        public bool IsGameOver => Lives <= 0;

        /// <summary>模拟购买：金币足够才扣款并返回 true（失败时金币不变，事务性）。</summary>
        public bool TryPurchase(int cost)
        {
            if (cost < 0 || Coins < cost) return false;
            Coins -= cost;
            return true;
        }

        /// <summary>开始新一局：分数与金币清零，生命恢复。</summary>
        public void Reset(int lives = 3)
        {
            Score = 0;
            Coins = 0;
            totalPointsThisRun = 0;
            Lives = lives;
        }
    }
}
