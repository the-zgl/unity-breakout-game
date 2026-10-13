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
            if (rb.velocity.sqrMagnitude > 0.001f)
            {
                rb.velocity = rb.velocity.normalized * speed;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<BottomZone>() != null)
            {
                GameManager.Instance?.OnBallLost();
                ResetBall();
            }
        }

        /// <summary>把球放回场中央并重新给一个向上的初速度。</summary>
        public void ResetBall()
        {
            transform.position = new Vector3(0f, -3.5f, 0f);
            rb.velocity = startDirection.normalized * speed;
        }
    }

    /// <summary>底部出界触发区标记组件。</summary>
    public class BottomZone : MonoBehaviour
    {
    }
}
