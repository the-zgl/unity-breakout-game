using NUnit.Framework;

namespace Breakout.Tests
{
    /// <summary>
    /// GameState 单元测试：纯 C# 逻辑，不需要启动场景。
    /// 用例设计上覆盖「正常值 / 边界值 / 非法值」三类，这是测试开发的基本功。
    /// </summary>
    public class GameStateTests
    {
        // ------------------------------------------------------------ 计分与金币

        [Test]
        public void AddScore_IncrementsScoreAndRewardsCoins()
        {
            // Arrange
            var state = new GameState();
            // Act
            state.AddScore(100);
            // Assert
            Assert.AreEqual(100, state.Score);
            Assert.AreEqual(10, state.Coins);
        }

        [Test]
        public void AddScore_IgnoresNonPositive()
        {
            var state = new GameState();
            state.AddScore(0);
            state.AddScore(-5);
            Assert.AreEqual(0, state.Score);
            Assert.AreEqual(0, state.Coins);
        }

        [Test]
        public void AddScore_CoinsAccumulateAcrossCalls()
        {
            // 回归用例：金币按「本局累计得分」换算，而不是每次加分单独整除。
            // 曾经的 bug：AddScore(9) 后再 AddScore(1)，两次数值都 <10，永远拿不到金币。
            var state = new GameState();
            state.AddScore(9);
            Assert.AreEqual(0, state.Coins, "不足 10 分不给金币");

            state.AddScore(1);
            Assert.AreEqual(1, state.Coins, "累计到 10 分应给 1 金币");
        }

        [Test]
        public void AddScore_CoinsDoNotCarryOverAfterReset()
        {
            // 边界：Reset 之后是全新一局，金币不能带上局的余额
            var state = new GameState();
            state.AddScore(95); // 9 金币，余 5 分
            state.Reset();
            state.AddScore(5);  // 本局只有 5 分

            Assert.AreEqual(0, state.Coins, "上一局的零头不该带到下一局");
        }

        [Test]
        public void AddScore_TracksBestScore()
        {
            var state = new GameState();
            state.AddScore(300);
            state.Reset();
            state.AddScore(100);
            Assert.AreEqual(300, state.BestScore, "历史最高分不应被 Reset 清掉");
        }

        // ---------------------------------------------------------------- 生命

        [Test]
        public void LoseLife_DecrementsLivesAndReportsAlive()
        {
            var state = new GameState(3);
            bool alive = state.LoseLife();
            Assert.IsTrue(alive);
            Assert.AreEqual(2, state.Lives);
        }

        [Test]
        public void LoseLife_BecomesGameOverAtZero()
        {
            var state = new GameState(1);
            bool alive = state.LoseLife();
            Assert.IsFalse(alive);
            Assert.IsTrue(state.IsGameOver);
        }

        [Test]
        public void LoseLife_AlreadyDead_StaysAtZero()
        {
            var state = new GameState(0);
            state.LoseLife();
            Assert.AreEqual(0, state.Lives, "生命不应变成负数");
            Assert.IsTrue(state.IsGameOver);
        }

        // ------------------------------------------------------------ 模拟购买

        [Test]
        public void TryPurchase_SucceedsWithEnoughCoins()
        {
            var state = new GameState();
            state.AddScore(100); // 得 10 金币
            bool ok = state.TryPurchase(10);
            Assert.IsTrue(ok);
            Assert.AreEqual(0, state.Coins);
        }

        [Test]
        public void TryPurchase_FailsWithoutEnoughCoins()
        {
            var state = new GameState();
            bool ok = state.TryPurchase(10);
            Assert.IsFalse(ok);
            Assert.AreEqual(0, state.Coins);
        }

        [Test]
        public void TryPurchase_ExactlyEnoughCoins_Succeeds()
        {
            // 边界：刚好够钱
            var state = new GameState();
            state.AddScore(200); // 20 金币
            Assert.IsTrue(state.TryPurchase(20));
            Assert.AreEqual(0, state.Coins);
        }

        [Test]
        public void TryPurchase_NegativeCost_IsRejected()
        {
            var state = new GameState();
            state.AddScore(100);
            Assert.IsFalse(state.TryPurchase(-1), "负数价格必须被拒绝，否则会被刷金币");
            Assert.AreEqual(10, state.Coins, "失败时金币不能被改动");
        }

        [Test]
        public void Reset_ClearsScoreAndCoinsButKeepsLivesSetting()
        {
            var state = new GameState();
            state.AddScore(500);
            state.LoseLife();
            state.Reset(3);

            Assert.AreEqual(0, state.Score);
            Assert.AreEqual(0, state.Coins);
            Assert.AreEqual(3, state.Lives);
        }
    }
}
