using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Breakout.Tests
{
    /// <summary>
    /// PlayMode 用例示例（需要真实跑帧，所以放在 PlayMode 程序集里）。
    ///
    /// 目前留成骨架 + 一条能在无设备环境跑通的最小断言，
    /// 后续可以在这里补「球掉到底部扣命」「砖块被击碎加分」这类需要物理帧的用例。
    /// </summary>
    public class PlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator Framework_IsAlive()
        {
            yield return null;
            Assert.IsTrue(Application.isPlaying, "PlayMode 测试应该运行在播放模式");
        }

        [UnityTest]
        public IEnumerator Ball_KeepsConstantSpeed_AcrossFrames()
        {
            // Arrange：搭一个最小物理环境（小球 + 左右墙）
            var ballGo = new GameObject("Ball");
            ballGo.transform.position = Vector3.zero;

            var rb = ballGo.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;

            var collider = ballGo.AddComponent<CircleCollider2D>();
            collider.sharedMaterial = new PhysicsMaterial2D("Bouncy") { bounciness = 1f, friction = 0f };

            ballGo.AddComponent<Ball>();

            // Act：跑 30 帧
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();

            // Assert：速度大小应保持恒定（Ball.FixedUpdate 里做了 normalize）
            Assert.Greater(rb.velocity.magnitude, 0f, "小球应该动起来了");
            Assert.AreEqual(8f, rb.velocity.magnitude, 0.2f, "小球速度大小应保持恒定");

            Object.Destroy(ballGo);
        }
    }
}
