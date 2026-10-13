using UnityEngine;

namespace Breakout
{
    /// <summary>砖块：被球击中扣血，血量为 0 时通知 GameManager 加分并销毁。</summary>
    public class Brick : MonoBehaviour
    {
        [SerializeField] private int hp = 1;

        private SpriteRenderer sr;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // 用组件判断是不是小球（不依赖 Tag 配置）
            if (collision.collider.GetComponent<Ball>() == null) return;

            hp--;
            if (sr != null && hp > 0) sr.color = new Color(0.95f, 0.55f, 0.25f);
            if (hp <= 0)
            {
                GameManager.Instance?.OnBrickDestroyed();
                Destroy(gameObject);
            }
        }
    }
}
