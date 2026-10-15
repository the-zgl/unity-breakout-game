using UnityEngine;

namespace Breakout
{
    /// <summary>
    /// 小球：恒定速度运动，掉出底部判定丢命。
    /// 用 Ball.Instance 代替 Tag 查找，避免依赖 TagManager 配置（克隆后即可运行）。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Ball : MonoBehaviour
    {
        /// <summary>当前场上的小球，供碰撞方识别与重置使用。</summary>
        public static Ball Instance { get; private set; }

        [SerializeField] private float speed = 8f;
        [SerializeField] private Vector2 startDirection = new Vector2(0.35f, 0.95f);

        private Rigidbody2D rb;

        /// <summary>
        /// 惰性获取 Rigidbody2D。
        /// 不依赖 Awake 的调用时序（某些情况下 Awake 可能晚于首次方法调用），
        /// 避免 ResetBall / StopBall 里出现 NullReferenceException。
        /// </summary>
        private Rigidbody2D Rb
        {
            get
            {
                if (rb == null) rb = GetComponent<Rigidbody2D>();
                return rb;
            }
        }

        private void Awake()
        {
            Instance = this;
            rb = GetComponent<Rigidbody2D>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            ResetBall();
        }

        private void FixedUpdate()
        {
            // 保持速度大小恒定，避免物理运算带来的速度漂移
            var body = Rb;
            if (body == null) return;
            if (body.velocity.sqrMagnitude > 0.001f)
            {
                body.velocity = body.velocity.normalized * speed;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<BottomZone>() != null)
            {
                GameManager.Instance?.OnBallLost();

                // 游戏结束后不再发球，否则会出现「命数已归零但仍在发射小球」的 bug
                if (GameManager.Instance != null && GameManager.Instance.State.IsGameOver)
                {
                    StopBall();
                    return;
                }

                ResetBall();
            }
        }

        /// <summary>把球放回场中央并重新给一个向上的初速度。</summary>
        public void ResetBall()
        {
            var body = Rb;
            if (body == null) return;
            transform.position = new Vector3(0f, -3.5f, 0f);
            body.velocity = startDirection.normalized * speed;
        }

        /// <summary>停止小球运动（游戏结束时使用）。</summary>
        public void StopBall()
        {
            var body = Rb;
            if (body == null) return;
            body.velocity = Vector2.zero;
        }
    }

    /// <summary>底部出界触发区标记组件。</summary>
    public class BottomZone : MonoBehaviour
    {
    }
}
