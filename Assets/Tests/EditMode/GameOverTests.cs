using NUnit.Framework;
using UnityEngine;

namespace Breakout.Tests
{
    /// <summary>
    /// 游戏结束（Game Over）的回归测试。
    ///
    /// 曾经的真实 bug：命数归零后，小球仍会被 ResetBall() 重新发射，
    /// 且砸砖块仍在加分 —— 表现为「显示 0 命但游戏还在继续」。
    /// 根因是 OnBallLost 无条件发球、OnBrickDestroyed 从不检查游戏状态。
    /// 这几条用例用于守住该回归。
    /// </summary>
    public class GameOverTests
    {
        /// <summary>构造一个最小可运行环境（GameManager + Ball）。</summary>
        private static (GameObject gm, GameObject ball) BuildMinimalScene()
        {
            var gmObj = new GameObject("GameManager");
            var gm = gmObj.AddComponent<GameManager>();

            var ballObj = new GameObject("Ball");
            var rb = ballObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            ballObj.AddComponent<CircleCollider2D>();
            var ball = ballObj.AddComponent<Ball>();
            return (gmObj, ballObj);
        }

        [Test]
        public void LivesReachZero_MarksGameOver()
        {
            var state = new GameState(1);
            Assert.IsFalse(state.IsGameOver, "初始不应为游戏结束");

            bool alive = state.LoseLife();
            Assert.IsFalse(alive, "最后一条命失去后应返回 not alive");
            Assert.IsTrue(state.IsGameOver, "生命归零后应标记游戏结束");
        }

        [Test]
        public void AfterGameOver_ScoreDoesNotIncrease()
        {
            var (gmObj, ballObj) = BuildMinimalScene();
            try
            {
                var gm = gmObj.GetComponent<GameManager>();

                // 先正常得分
                gm.OnBrickDestroyed();
                Assert.AreEqual(100, gm.State.Score, "砸砖应加分");

                // 把命耗到 0
                gm.OnBallLost();
                gm.OnBallLost();
                gm.OnBallLost();
                Assert.IsTrue(gm.State.IsGameOver, "三次丢命后应游戏结束");

                int scoreAtGameOver = gm.State.Score;

                // 游戏结束后再砸砖，分数不应变化
                gm.OnBrickDestroyed();
                Assert.AreEqual(scoreAtGameOver, gm.State.Score, "游戏结束后不应继续计分");
            }
            finally
            {
                Object.DestroyImmediate(gmObj);
                Object.DestroyImmediate(ballObj);
            }
        }

        [Test]
        public void AfterGameOver_LivesDoNotGoBelowZero()
        {
            var (gmObj, ballObj) = BuildMinimalScene();
            try
            {
                var gm = gmObj.GetComponent<GameManager>();

                // 故意多丢几次命
                for (int i = 0; i < 5; i++) gm.OnBallLost();

                Assert.AreEqual(0, gm.State.Lives, "生命不应变成负数");
                Assert.IsTrue(gm.State.IsGameOver);
            }
            finally
            {
                Object.DestroyImmediate(gmObj);
                Object.DestroyImmediate(ballObj);
            }
        }

        [Test]
        public void AfterGameOver_BallStopsMoving()
        {
            var (gmObj, ballObj) = BuildMinimalScene();
            try
            {
                var gm = gmObj.GetComponent<GameManager>();
                var ball = ballObj.GetComponent<Ball>();
                var rb = ballObj.GetComponent<Rigidbody2D>();

                // 让球动起来
                ball.ResetBall();
                Assert.Greater(rb.velocity.magnitude, 0f, "发球后小球应有速度");

                // 游戏结束
                for (int i = 0; i < 3; i++) gm.OnBallLost();
                Assert.IsTrue(gm.State.IsGameOver);

                // 模拟球再次掉到底部（此时应停球而不是重发）
                ball.StopBall();
                Assert.AreEqual(0f, rb.velocity.magnitude, "游戏结束后小球应停止");
            }
            finally
            {
                Object.DestroyImmediate(gmObj);
                Object.DestroyImmediate(ballObj);
            }
        }

        [Test]
        public void Reset_RestoresPlayableState()
        {
            var state = new GameState(3);
            state.AddScore(500);
            for (int i = 0; i < 3; i++) state.LoseLife();
            Assert.IsTrue(state.IsGameOver);

            state.Reset(3);
            Assert.IsFalse(state.IsGameOver, "重开后应恢复可玩状态");
            Assert.AreEqual(3, state.Lives);
            Assert.AreEqual(0, state.Score);
        }

        [Test]
        public void AfterGameOver_CannotBuyLife()
        {
            var (gmObj, ballObj) = BuildMinimalScene();
            try
            {
                var gm = gmObj.GetComponent<GameManager>();

                // 攒够金币再结束游戏
                // 每块砖 100 分，每 10 分 1 金币；买命要 20 金币，所以砸 2 块砖
                gm.OnBrickDestroyed();
                gm.OnBrickDestroyed();
                for (int i = 0; i < 3; i++) gm.OnBallLost();
                Assert.IsTrue(gm.State.IsGameOver, "前置：游戏已结束");
                Assert.GreaterOrEqual(gm.State.Coins, 20, "前置：金币应足够买命");

                int coinsBefore = gm.State.Coins;
                int livesBefore = gm.State.Lives;

                // 游戏结束后购买应被拒绝
                gm.BuyLife(20);

                Assert.AreEqual(livesBefore, gm.State.Lives, "游戏结束后不应能购买生命");
                Assert.AreEqual(coinsBefore, gm.State.Coins, "购买失败不应扣金币");
            }
            finally
            {
                Object.DestroyImmediate(gmObj);
                Object.DestroyImmediate(ballObj);
            }
        }
    }
}
