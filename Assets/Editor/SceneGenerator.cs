using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Breakout.EditorTools
{
    /// <summary>
    /// 一键生成可运行场景（菜单：Breakout → 生成主场景）。
    ///
    /// 仓库里没有手写 .unity 文件，场景由 Unity 自己生成，保证格式永远合法；
    /// 场景里只有一个挂着 Bootstrap 的空物体，其余对象全部由代码在运行时创建。
    /// </summary>
    public static class SceneGenerator
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Breakout/生成主场景并设为启动场景")]
        public static void GenerateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrap = new GameObject("Bootstrap");
            bootstrap.AddComponent<Bootstrap>();

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            Debug.Log($"[Breakout] 已生成场景 {ScenePath}，并加入 Build Settings。按 Play 即可开跑。");
            EditorUtility.DisplayDialog(
                "Breakout",
                "主场景已生成：\n" + ScenePath + "\n\n已加入 Build Settings，直接按 Play 就能玩。",
                "好的");
        }

        [MenuItem("Breakout/运行 EditMode 单元测试")]
        public static void RunEditModeTests()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
        }
    }
}
