using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Breakout.Tests
{
    /// <summary>
    /// 端到端冒烟：加载真实场景（Assets/Scenes/Main.unity）并运行若干物理帧，
    /// 验证「打开工程 → 按 Play 就能玩」这条链路真的成立。
    ///
    /// 与 BootstrapSmokeTests 的区别：那个直接调 AssembleScene()，
    /// 这个走完整的场景加载流程（LoadScene），更接近玩家按 Play 的实际情况。
    /// </summary>
    public class SceneEndToEndTests
    {
        [UnityTest]
        public IEnumerator MainScene_LoadsAndRunsGameplay()
        {
            // Arrange & Act：加载仓库里生成的场景
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main", UnityEngine.SceneManagement.LoadSceneMode.Single);

            // 等场景完成加载与 Awake/Start
            yield return null;
            yield return null;

            // Assert 1：Bootstrap 把主要对象都建出来了
            Assert.IsNotNull(GameObject.Find("GameManager"), "场景加载后应存在 GameManager");
            Assert.IsNotNull(GameObject.Find("Paddle"), "应存在挡板");
            Assert.IsNotNull(GameObject.Find("Ball"), "应存在小球");
            Assert.IsNotNull(GameObject.Find("HUD"), "应存在 HUD");
            Assert.AreEqual(55, Object.FindObjectsOfType<Brick>().Length, "砖块数量应为 5×11=55");

            // Assert 2：小球真的在动（跑物理帧）
            var ball = GameObject.Find("Ball");
            var rb = ball.GetComponent<Rigidbody2D>();
            Vector2 before = rb.position;
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Vector2 after = rb.position;

            Assert.AreNotEqual(before, after, "小球应在物理帧中移动");
            Assert.AreEqual(8f, rb.velocity.magnitude, 0.3f, "小球速度大小应保持恒定");

            // Assert 3：GameState 初始值正确
            var gm = GameObject.Find("GameManager").GetComponent<GameManager>();
            Assert.AreEqual(3, gm.State.Lives, "初始生命应为 3");
            Assert.AreEqual(0, gm.State.Score, "初始分数应为 0");
        }
    }
}
