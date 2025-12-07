// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class DetectionSpawnMarkerAnim : MonoBehaviour
    {
        [SerializeField] private Vector3 m_anglesSpeed = new(20.0f, 40.0f, 60.0f);
        [SerializeField] private Transform m_model;
        [SerializeField] private TextMesh m_textModel;
        [SerializeField] private Transform m_textEntity;

        private Vector3 m_angles;
        private OVRCameraRig m_camera;

        private void Update()
        {
            m_angles.x = AddAngle(m_angles.x, m_anglesSpeed.x * Time.deltaTime);
            m_angles.y = AddAngle(m_angles.y, m_anglesSpeed.y * Time.deltaTime);
            m_angles.z = AddAngle(m_angles.z, m_anglesSpeed.z * Time.deltaTime);

            m_model.rotation = Quaternion.Euler(m_angles);

            if (!m_camera)
            {
                m_camera = FindFirstObjectByType<OVRCameraRig>();
            }
            else
            {
                m_textEntity.gameObject.transform.LookAt(m_camera.centerEyeAnchor);
            }
        }

        private float AddAngle(float value, float toAdd)
        {
            value += toAdd;
            if (value > 360.0f)
            {
                value -= 360.0f;
            }

            if (value < 0.0f)
            {
                value = 360.0f - value;
            }

            return value;
        }

        public void SetYoloClassName(string name)
        {
            m_textModel.text = name;
        }

        public string GetYoloClassName()
        {
            return m_textModel.text;
        }

        public void SetVisual(bool isFireHazard)
        {
            // Hide the default 3D model and text
            if (m_model) m_model.gameObject.SetActive(false);
            if (m_textModel) m_textModel.gameObject.SetActive(false);

            // Create or update the visual icon
            var iconName = isFireHazard ? "FireIconSpawned" : "iceSpawned";
            var existingIcon = transform.Find("SpawnedIcon");
            if (existingIcon != null)
            {
                Destroy(existingIcon.gameObject);
            }

            GameObject iconObj = new GameObject("SpawnedIcon");
            iconObj.transform.SetParent(transform, false);
            iconObj.transform.localPosition = Vector3.zero;

            // Use a SpriteRenderer
            SpriteRenderer sr = iconObj.AddComponent<SpriteRenderer>();

            string resourcePath = isFireHazard ? "Textures/FireIcon" : "Textures/ice";
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
            {
                 sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            else
            {
                // Fallback
            }

            // Adjust scale - world space
            iconObj.transform.localScale = Vector3.one * 0.2f; // Adjust as needed
        }
    }
}
