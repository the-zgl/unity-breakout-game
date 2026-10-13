using NUnit.Framework;
using UnityEngine;

namespace Breakout.Tests
{
    /// <summary>
    /// 场景装配冒烟测试：验证 Bootstrap 能在代码层把整个场景搭起来。
    ///
    /// 这类测试的价值：项目里最容易坏的不是算法，而是「引用没接上」。
    /// 手工搭场景没法自动回归，把装配写成代码之后，就能用一条测试守住
    /// 「挡板/小球/砖块/出界区/HUD 是不是都在」。
    /// </summary>
    public class BootstrapSmokeTests
    {
        [Test]
        public void Bootstrap_CreatesPlayableScene()
        {
            var root = new GameObject("Bootstrap");
            try
            {
                var bootstrap = root.AddComponent<Bootstrap>();
                // 直接调用装配方法，而不是 SendMessage("Start")：
                // Unity 不允许在编辑模式下手动触发生命周期回调。
                bootstrap.AssembleScene();

                Assert.IsNotNull(GameObject.Find("GameManager"), "应有 GameManager");
                Assert.IsNotNull(GameObject.Find("Paddle"), "应有挡板");
                Assert.IsNotNull(GameObject.Find("Ball"), "应有小球");
                Assert.IsNotNull(GameObject.Find("BottomZone"), "应有底部出界触发区");
                Assert.IsNotNull(GameObject.Find("HUD"), "应有 HUD 画布");
                Assert.IsNotNull(GameObject.Find("Ball").GetComponent<Ball>(), "小球应挂 Ball 组件");
                Assert.IsNotNull(GameObject.Find("Paddle").GetComponent<PaddleController>(), "挡板应挂控制器");
            }
            finally
            {
                Cleanup(root);
            }
        }

        [Test]
        public void Bootstrap_SpawnsExpectedNumberOfBricks()
        {
            var root = new GameObject("Bootstrap");
            try
            {
                var bootstrap = root.AddComponent<Bootstrap>();
                bootstrap.AssembleScene();

                // 5 行 × 11 列
                Assert.AreEqual(55, Object.FindObjectsOfType<Brick>().Length);
            }
            finally
            {
                Cleanup(root);
            }
        }

        /// <summary>清理装配出来的对象，避免用例之间互相干扰。</summary>
        private static void Cleanup(GameObject root)
        {
            foreach (var brick in Object.FindObjectsOfType<Brick>())
                Object.DestroyImmediate(brick.gameObject);

            foreach (var name in new[] { "GameManager", "Paddle", "Ball", "BottomZone",
                                         "HUD", "EventSystem", "WallLeft", "WallRight", "WallTop",
                                         "Main Camera" })
            {
                var go = GameObject.Find(name);
                if (go != null) Object.DestroyImmediate(go);
            }

            if (root != null) Object.DestroyImmediate(root);
        }
    }
}
