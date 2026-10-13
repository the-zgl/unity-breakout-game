using UnityEngine;

namespace Breakout
{
    /// <summary>
    /// 挡板：A/D 或左右方向键控制，也支持鼠标拖动；限制在场地范围内。
    /// </summary>
    public class PaddleController : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float minX = -6.6f;
        [SerializeField] private float maxX = 6.6f;

        /// <summary>使用不依赖 Input Manager 的按键读取，克隆下来即可直接运行。</summary>
        private void Update()
        {
            float h = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) h -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) h += 1f;

            // 没有键盘输入时用鼠标跟随
            if (Mathf.Approximately(h, 0f) && Camera.main != null && Input.GetMouseButton(0))
            {
                Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                h = Mathf.Clamp((mouse.x - transform.position.x) * 3f, -1f, 1f);
            }

            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x + h * speed * Time.deltaTime, minX, maxX);
            transform.position = pos;
        }
    }
}
