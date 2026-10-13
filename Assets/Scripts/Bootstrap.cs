using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Breakout
{
    /// <summary>
    /// 运行时装配器：用一个脚本把整个游戏场景搭出来。
    ///
    /// 为什么这么做：
    /// 手写 .unity 场景文件（YAML + GUID）极易出错，而且难以做代码评审。
    /// 把场景装配写成 C# 之后，克隆仓库 → 打开工程 → 按 Play 就能跑，
    /// 不需要在 Inspector 里手工拖拽任何引用，也方便后续做自动化校验。
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        private const int BrickRows = 5;
        private const int BrickCols = 11;
        private const float BrickSpacingX = 1.24f;
        private const float BrickSpacingY = 0.62f;
        private const float BrickTopY = 3.6f;

        private GameManager game;
        private Font uiFont;

        private void Start()
        {
            AssembleScene();
        }

        /// <summary>
        /// 装配整个场景（相机、砖墙、挡板、小球、墙、出界区、HUD、购买按钮）。
        /// 拆成独立方法而不是写在 Start 里，是为了让自动化测试能直接调用它，
        /// 不必依赖 Unity 的生命周期回调。
        /// </summary>
        public void AssembleScene()
        {
            SetupCamera();
            Physics2D.gravity = Vector2.zero;

            game = new GameObject("GameManager").AddComponent<GameManager>();

            UIManager ui = BuildHud();
            game.BindUi(ui);          // 把 HUD 接进 GameManager，否则砸砖/扣命后界面不会刷新
            BuildArena();
            BuildPaddle();
            BuildBall();
            BuildWalls();

            ui.Refresh(game.State);
            ui.Toast("A/D 或 ←/→ 移动挡板，砸光砖块试试");
        }

        private void Update()
        {
            // 一键重开，方便反复回归验证
            if (Input.GetKeyDown(KeyCode.R) && game != null)
            {
                game.State.Reset();
                Ball.Instance?.ResetBall();
                Debug.Log("[Bootstrap] 重新开始");
            }
        }

        // ---------------------------------------------------------------- 场景

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(0f, 0.5f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.09f, 0.14f);
        }

        private void BuildArena()
        {
            for (int row = 0; row < BrickRows; row++)
            {
                for (int col = 0; col < BrickCols; col++)
                {
                    float x = (col - (BrickCols - 1) * 0.5f) * BrickSpacingX;
                    float y = BrickTopY - row * BrickSpacingY;

                    var brick = new GameObject($"Brick_{row}_{col}");
                    brick.transform.position = new Vector3(x, y, 0f);
                    brick.transform.localScale = new Vector3(1.1f, 0.45f, 1f);

                    var sr = brick.AddComponent<SpriteRenderer>();
                    sr.sprite = ShapeFactory.Square();
                    sr.color = row % 2 == 0
                        ? new Color(0.36f, 0.62f, 0.98f)
                        : new Color(0.45f, 0.85f, 0.72f);

                    brick.AddComponent<BoxCollider2D>();
                    brick.AddComponent<Brick>();
                }
            }
        }

        private void BuildPaddle()
        {
            var paddle = new GameObject("Paddle");
            paddle.transform.position = new Vector3(0f, -5f, 0f);
            paddle.transform.localScale = new Vector3(2.2f, 0.42f, 1f);

            var sr = paddle.AddComponent<SpriteRenderer>();
            sr.sprite = ShapeFactory.Square();
            sr.color = new Color(0.97f, 0.86f, 0.36f);

            paddle.AddComponent<BoxCollider2D>();
            paddle.AddComponent<PaddleController>();
        }

        private void BuildBall()
        {
            var ball = new GameObject("Ball");
            ball.transform.position = new Vector3(0f, -3.5f, 0f);
            ball.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            var sr = ball.AddComponent<SpriteRenderer>();
            sr.sprite = ShapeFactory.Circle();
            sr.color = Color.white;

            var rb = ball.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = ball.AddComponent<CircleCollider2D>();
            // 完全弹性、无摩擦，保证球速稳定、弹跳可预测（用例断言才不会随机失败）
            collider.sharedMaterial = new PhysicsMaterial2D("Bouncy") { bounciness = 1f, friction = 0f };

            ball.AddComponent<Ball>();
        }

        private void BuildWalls()
        {
            // 左 / 右 / 上三面墙（静态碰撞体）
            CreateWall("WallLeft", new Vector3(-7.3f, 0.5f, 0f), new Vector2(0.5f, 13f));
            CreateWall("WallRight", new Vector3(7.3f, 0.5f, 0f), new Vector2(0.5f, 13f));
            CreateWall("WallTop", new Vector3(0f, 7f, 0f), new Vector2(15f, 0.5f));

            // 底部出界触发区
            var bottom = new GameObject("BottomZone");
            bottom.transform.position = new Vector3(0f, -6.6f, 0f);
            var box = bottom.AddComponent<BoxCollider2D>();
            box.size = new Vector2(15f, 0.6f);
            box.isTrigger = true;
            bottom.AddComponent<BottomZone>();
        }

        private void CreateWall(string name, Vector3 position, Vector2 size)
        {
            var wall = new GameObject(name);
            wall.transform.position = position;
            var box = wall.AddComponent<BoxCollider2D>();
            box.size = size;
        }

        // ------------------------------------------------------------------ UI

        private UIManager BuildHud()
        {
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            UIManager ui = canvasGo.AddComponent<UIManager>();
            ui.Configure(
                MakeText(canvasGo.transform, "ScoreText", "分数 0",
                    new Vector2(0f, 1f), new Vector2(24f, -24f), TextAnchor.UpperLeft),
                MakeText(canvasGo.transform, "LivesText", "生命 3",
                    new Vector2(0.5f, 1f), new Vector2(0f, -24f), TextAnchor.UpperCenter),
                MakeText(canvasGo.transform, "CoinsText", "金币 0",
                    new Vector2(1f, 1f), new Vector2(-24f, -24f), TextAnchor.UpperRight),
                MakeText(canvasGo.transform, "ToastText", "",
                    new Vector2(0.5f, 0f), new Vector2(0f, 40f), TextAnchor.LowerCenter));

            BuildBuyButton(canvasGo.transform);
            return ui;
        }

        private Text MakeText(Transform parent, string name, string content, Vector2 anchor, Vector2 offset, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(420f, 60f);

            var text = go.AddComponent<Text>();
            text.font = UIFont;
            text.fontSize = 30;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = content;
            return text;
        }

        private void BuildBuyButton(Transform parent)
        {
            var go = new GameObject("BuyLifeButton", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-24f, 24f);
            rect.sizeDelta = new Vector2(280f, 64f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.24f, 0.42f, 0.72f);

            var shop = go.AddComponent<ShopManager>();
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(shop.BuyExtraLife);

            var label = MakeText(go.transform, "Label", "购买生命 (20 金币)",
                new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
            label.fontSize = 22;
        }

        /// <summary>UI 字体：优先用内置字体，失败则退回系统字体，保证不出现空引用。</summary>
        private Font UIFont
        {
            get
            {
                if (uiFont != null) return uiFont;
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Arial", 30);
                return uiFont;
            }
        }
    }
}
