using UnityEngine;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    public class WiggleAnimation : MonoBehaviour
    {
        [SerializeField] private float m_speed = 10.0f;
        [SerializeField] private float m_amount = 2.0f;

        private Vector3 m_initialPosition;

        private void OnEnable()
        {
            m_initialPosition = transform.localPosition;
        }

        private void Update()
        {
            transform.localPosition = m_initialPosition + Vector3.right * Mathf.Sin(Time.time * m_speed) * m_amount;
        }
    }
}
