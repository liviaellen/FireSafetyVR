// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.XR;
using Meta.XR.Samples;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class SentisInferenceUiManager : MonoBehaviour
    {
        [Header("Placement configureation")]
        [SerializeField] private EnvironmentRayCastSampleManager m_environmentRaycast;
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;

        [Header("UI display references")]
        [SerializeField] private SentisObjectDetectedUiManager m_detectionCanvas;
        [SerializeField] private RawImage m_displayImage;
        [SerializeField] private Sprite m_boxTexture;
        [SerializeField] private Color m_boxColor;
        [SerializeField] private Font m_font;
        [SerializeField] private Color m_fontColor;
        [SerializeField] private int m_fontSize = 80;
        [Space(10)]
        public UnityEvent<int> OnObjectsDetected;

        public List<BoundingBox> BoxDrawn = new();

        private string[] m_labels;
        private List<GameObject> m_boxPool = new();
        private Transform m_displayLocation;
        private bool m_isQuizMode = false;

        public void SetQuizMode(bool active)
        {
            m_isQuizMode = active;
        }

        private readonly Dictionary<string, bool> m_fireHazardMap = new Dictionary<string, bool>
        {
            { "person", false },
            { "bicycle", false },
            { "car", true },
            { "motorbike", true },
            { "aeroplane", true },
            { "bus", true },
            { "train", true },
            { "truck", true },
            { "boat", true },
            { "traffic light", false },
            { "fire hydrant", false },
            { "stop sign", false },
            { "parking meter", false },
            { "bench", false },
            { "bird", false },
            { "cat", false },
            { "dog", false },
            { "horse", false },
            { "sheep", false },
            { "cow", false },
            { "elephant", false },
            { "bear", false },
            { "zebra", false },
            { "giraffe", false },
            { "backpack", true },
            { "umbrella", false },
            { "handbag", true },
            { "tie", false },
            { "suitcase", true },
            { "frisbee", false },
            { "skis", false },
            { "snowboard", false },
            { "sports ball", false },
            { "kite", false },
            { "baseball bat", false },
            { "baseball glove", false },
            { "skateboard", false },
            { "surfboard", false },
            { "tennis racket", false },
            { "bottle", true },
            { "wine glass", false },
            { "cup", false },
            { "fork", false },
            { "knife", false },
            { "spoon", false },
            { "bowl", false },
            { "banana", false },
            { "apple", false },
            { "sandwich", false },
            { "orange", false },
            { "broccoli", false },
            { "carrot", false },
            { "hot dog", false },
            { "pizza", false },
            { "donut", false },
            { "cake", false },
            { "chair", false },
            { "sofa", true },
            { "pottedplant", false },
            { "bed", true },
            { "diningtable", false },
            { "toilet", false },
            { "tvmonitor", true },
            { "laptop", true },
            { "mouse", false },
            { "remote", true },
            { "keyboard", false },
            { "cell phone", true },
            { "microwave", true },
            { "oven", true },
            { "toaster", true },
            { "sink", false },
            { "refrigerator", true },
            { "book", true },
            { "clock", false },
            { "vase", false },
            { "scissors", false },
            { "teddy bear", true },
            { "hair drier", true },
            { "toothbrush", false }
        };

        public bool IsFireHazard(string className)
        {
            if (m_fireHazardMap.TryGetValue(className, out bool isHazard))
            {
                return isHazard;
            }
            return false;
        }

        //bounding box data
        public struct BoundingBox
        {
            public float CenterX;
            public float CenterY;
            public float Width;
            public float Height;
            public string Label;
            public Vector3? WorldPos;
            public string ClassName;
        }

        #region Unity Functions
        private void Start()
        {
            m_displayLocation = m_displayImage.transform;
        }
        #endregion

        #region Detection Functions
        public void OnObjectDetectionError()
        {
            // Clear current boxes
            ClearAnnotations();

            // Set obejct found to 0
            OnObjectsDetected?.Invoke(0);
        }
        #endregion

        #region BoundingBoxes functions
        public void SetLabels(TextAsset labelsAsset)
        {
            //Parse neural net m_labels
            m_labels = labelsAsset.text.Split('\n');
        }

        public void SetDetectionCapture(Texture image)
        {
            m_displayImage.texture = image;
            m_detectionCanvas.CapturePosition();
        }

        public void DrawUIBoxes(Tensor<float> output, Tensor<int> labelIDs, float imageWidth, float imageHeight, Pose cameraPose)
        {
            // Updte canvas position
            m_detectionCanvas.UpdatePosition();

            // Clear current boxes
            ClearAnnotations();

            float displayWidth = m_displayImage.rectTransform.rect.width;
            var displayHeight = m_displayImage.rectTransform.rect.height;

            var boxesFound = output.shape[0];
            if (boxesFound <= 0)
            {
                OnObjectsDetected?.Invoke(0);
                return;
            }
            var maxBoxes = Mathf.Min(boxesFound, 200);

            OnObjectsDetected?.Invoke(maxBoxes);

            //Draw the bounding boxes
            for (var n = 0; n < maxBoxes; n++)
            {
                // Get bounding box center coordinates
                var normalizedCenterX = output[n, 0] / imageWidth;
                var normalizedCenterY = output[n, 1] / imageHeight;
                var centerX = displayWidth * (normalizedCenterX - 0.5f);
                var centerY = displayHeight * (normalizedCenterY - 0.5f);

                // Get object class name
                var classname = m_labels[labelIDs[n]].Replace(" ", "_");

                // Get the 3D marker world position using Depth Raycast
                var ray = m_cameraAccess.ViewportPointToRay(new Vector2(normalizedCenterX, 1.0f - normalizedCenterY), cameraPose);
                var worldPos = m_environmentRaycast.Raycast(ray);

                var rawLabel = m_labels[labelIDs[n]].Trim();
                bool isFireHazard = false;
                if (m_fireHazardMap.TryGetValue(rawLabel, out bool hazard))
                {
                    isFireHazard = hazard;
                }

                // Create a new bounding box
                var box = new BoundingBox
                {
                    CenterX = centerX,
                    CenterY = centerY,
                    ClassName = classname,
                    Width = output[n, 2] * (displayWidth / imageWidth),
                    Height = output[n, 3] * (displayHeight / imageHeight),
                    Label = m_isQuizMode ? "?" : $"{classname}\n{(isFireHazard ? "Fire Hazard" : "No Fire Hazard")}",
                    WorldPos = worldPos,
                };

                // Add to the list of boxes
                BoxDrawn.Add(box);

                // Draw 2D box
                DrawBox(box, n, isFireHazard);
            }
        }

        private void ClearAnnotations()
        {
            foreach (var box in m_boxPool)
            {
                box?.SetActive(false);
            }
            BoxDrawn.Clear();
        }

        private void DrawBox(BoundingBox box, int id, bool isFireHazard)
        {
            //Create the bounding box graphic or get from pool
            GameObject panel;
            if (id < m_boxPool.Count)
            {
                panel = m_boxPool[id];
                if (panel == null)
                {
                    panel = CreateNewBox(m_boxColor);
                }
                else
                {
                    panel.SetActive(true);
                }
            }
            else
            {
                panel = CreateNewBox(m_boxColor);
            }
            //Set box position
            panel.transform.localPosition = new Vector3(box.CenterX, -box.CenterY, box.WorldPos.HasValue ? box.WorldPos.Value.z : 0.0f);
            //Set box rotation
            panel.transform.rotation = Quaternion.LookRotation(panel.transform.position - m_detectionCanvas.GetCapturedCameraPosition());
            //Set box size
            var rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(box.Width, box.Height);

            // Set box color
            var img = panel.GetComponent<Image>();
            if (m_isQuizMode) {
                img.color = Color.yellow; // Neutral color
            } else {
                img.color = isFireHazard ? Color.red : Color.blue;
            }

            //Set label text
            var label = panel.GetComponentInChildren<Text>();
            label.text = box.Label;
            label.fontSize = 24;

            // Handle Fire/Ice Icon
            var fireIcon = panel.transform.Find("FireIcon");
            var iceIcon = panel.transform.Find("IceIcon");

            if (m_isQuizMode)
            {
                // Hide icons to not spoil the answer
                if (fireIcon != null) fireIcon.gameObject.SetActive(false);
                if (iceIcon != null) iceIcon.gameObject.SetActive(false);
            }
            else
            {
                if (isFireHazard)
                {
                    if (fireIcon != null) fireIcon.gameObject.SetActive(true);
                    if (iceIcon != null) iceIcon.gameObject.SetActive(false);
                }
                else
                {
                    if (fireIcon != null) fireIcon.gameObject.SetActive(false);
                    if (iceIcon != null) iceIcon.gameObject.SetActive(true);
                }
            }
        }

        private GameObject CreateNewBox(Color color)
        {
            //Create the box and set image
            var panel = new GameObject("ObjectBox");
            _ = panel.AddComponent<CanvasRenderer>();
            var img = panel.AddComponent<Image>();
            img.color = color;
            img.sprite = m_boxTexture;
            img.type = Image.Type.Sliced;
            img.fillCenter = false;
            panel.transform.SetParent(m_displayLocation, false);

            //Create the label
            var text = new GameObject("ObjectLabel");
            _ = text.AddComponent<CanvasRenderer>();
            text.transform.SetParent(panel.transform, false);
            var txt = text.AddComponent<Text>();
            txt.font = m_font;
            txt.color = m_fontColor;
            txt.fontSize = m_fontSize;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.alignment = TextAnchor.UpperRight;

            var rt2 = text.GetComponent<RectTransform>();
            rt2.offsetMin = new Vector2(20, rt2.offsetMin.y);
            rt2.offsetMax = new Vector2(0, rt2.offsetMax.y);
            rt2.offsetMin = new Vector2(rt2.offsetMin.x, 0);
            rt2.offsetMax = new Vector2(rt2.offsetMax.x, 30);
            rt2.anchorMin = new Vector2(0, 0);
            rt2.anchorMax = new Vector2(1, 1);

            // Create Fire Icon
            CreateIcon(panel, "FireIcon", "Textures/FireIcon", Color.white, true);

            // Create Ice Icon (for safe objects) - Shifted left to avoid overlap
            CreateIcon(panel, "IceIcon", "Textures/ice", new Color(0.6f, 0.8f, 1f), false, new Vector2(-60, -20)); // Shifted further left

            m_boxPool.Add(panel);
            return panel;
        }

        private void CreateIcon(GameObject parent, string name, string resourcePath, Color fallbackColor, bool wiggle, Vector2? posOverride = null)
        {
            var iconObj = new GameObject(name);
            iconObj.transform.SetParent(parent.transform, false);
            var iconImg = iconObj.AddComponent<Image>();

            var iconSprite = Resources.Load<Texture2D>(resourcePath);
            if (iconSprite != null)
            {
                 iconImg.sprite = Sprite.Create(iconSprite, new Rect(0, 0, iconSprite.width, iconSprite.height), new Vector2(0.5f, 0.5f));
            }
            else
            {
                // Fallback procedural texture if resource not found (e.g. IceIcon)
                // Create a simple circle or square
                Texture2D texture = new Texture2D(32, 32);
                Color[] colors = new Color[32 * 32];
                for (int i = 0; i < colors.Length; i++) colors[i] = fallbackColor;
                texture.SetPixels(colors);
                texture.Apply();
                iconImg.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            }

            if (wiggle)
            {
                iconObj.AddComponent<WiggleAnimation>();
            }

            var iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(40, 40); // Size of the icon
            iconRt.anchorMin = new Vector2(1, 1); // Top right corner
            iconRt.anchorMax = new Vector2(1, 1);
            iconRt.pivot = new Vector2(0.5f, 0.5f);

            // Use override or default
            if (posOverride.HasValue)
                iconRt.anchoredPosition = posOverride.Value;
            else
                iconRt.anchoredPosition = new Vector2(-20, -20); // Offset from corner

            // Default to hidden
            iconObj.SetActive(false);
        }
        #endregion
    }
}
