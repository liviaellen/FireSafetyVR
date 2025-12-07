// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class DetectionUiMenuManager : MonoBehaviour
    {
        [Header("Ui buttons")]
        [SerializeField] private OVRInput.RawButton m_actionButton = OVRInput.RawButton.A;

        [Header("Ui elements ref.")]
        [SerializeField] private GameObject m_loadingPanel;
        [SerializeField] private GameObject m_initialPanel;
        [SerializeField] private GameObject m_noPermissionPanel;
        [SerializeField] private Text m_labelInformation;
        [SerializeField] private AudioSource m_buttonSound;

        public bool IsInputActive { get; set; } = false;

        public UnityEvent<bool> OnPause;

        private bool m_initialMenu;
        private SafetyQuizManager m_quizManager;

        // start menu
        private int m_objectsDetected = 0;
        private int m_objectsIdentified = 0;

        // pause menu
        public bool IsPaused { get; private set; } = true;

        #region Unity Functions
        private IEnumerator Start()
        {
            m_initialPanel.SetActive(false);
            m_noPermissionPanel.SetActive(false);

            // Wait until Sentis model is loaded
            m_loadingPanel.SetActive(true);
            var sentisInference = FindFirstObjectByType<SentisInferenceRunManager>();
            while (!sentisInference.IsModelLoaded)
            {
                yield return null;
            }
            m_loadingPanel.SetActive(false);

            // Wait for permissions
            OnNoPermissionMenu();
            UnityEngine.Android.Permission.RequestUserPermission("horizonos.permission.HEADSET_CAMERA");
            while (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.Scene) ||
                   !OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess) ||
                   !UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.CAMERA") ||
                   !UnityEngine.Android.Permission.HasUserAuthorizedPermission("horizonos.permission.HEADSET_CAMERA"))
            {
                yield return null;
            }

            // Setup Launcher UI
            var audioClip = Resources.Load<AudioClip>("Audio/spark");
            if (audioClip != null)
            {
                var audioParams = m_initialPanel.AddComponent<AudioSource>();
                audioParams.clip = audioClip;
                audioParams.loop = true;
                audioParams.Play();
            }

            var logoTexture = Resources.Load<Texture2D>("Textures/SparkLogo");
            if (logoTexture != null)
            {
                // Disable the original placeholder image if it exists
                var images = m_initialPanel.GetComponentsInChildren<Image>();
                if (images.Length > 0)
                {
                     // Assuming the first image was the background/placeholder
                     // We check if it's not one of our new things (which aren't created yet or are distinct)
                     // Since we create LogoImage AFTER this check, images[0] is definitely the old one.
                     images[0].gameObject.SetActive(false);
                }

                // Create a dedicated logo object to ensure correct aspect ratio and sizing
                GameObject logoObj = new GameObject("LogoImage");
                logoObj.transform.SetParent(m_initialPanel.transform, false);

                Image logoImg = logoObj.AddComponent<Image>();
                logoImg.sprite = Sprite.Create(logoTexture, new Rect(0, 0, logoTexture.width, logoTexture.height), new Vector2(0.5f, 0.5f));
                logoImg.preserveAspect = true;
                logoImg.raycastTarget = false; // Prevent blocking interactions

                // Position it at the top
                RectTransform rt = logoObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0, 0); // Moved up to top edge
                rt.sizeDelta = new Vector2(400, 200); // Max size, preserveAspect will handle the rest
            }

            var texts = m_initialPanel.GetComponentsInChildren<Text>();
            foreach (var t in texts)
            {
                if (t.text.Contains("PCA") || t.text.Contains("sample") || t.text.Contains("Test"))
                {
                    // Hide the old text title if we are using the new logo
                    if (logoTexture != null)
                    {
                         t.gameObject.SetActive(false);
                    }
                    else
                    {
                        t.text = "Fire Safety Hazard Training";
                    }
                }
            }

            OnInitialMenu();

            // Apply global font upgrade
            Font niceFont = Resources.Load<Font>("Fonts/Montserrat-Bold");
            if (niceFont != null)
            {
                var allTexts = m_initialPanel.GetComponentsInChildren<Text>(true);
                foreach (var t in allTexts) t.font = niceFont;
                if (m_labelInformation != null) m_labelInformation.font = niceFont;
            }

            CreateMainMenu();

            // Auto-configure VR Input for UI
            SetupVRInput();

            // Ensure input loop runs for shortcuts
            IsInputActive = true;

            // Setup Quiz Manager
            if (m_quizManager == null)
            {
                GameObject quizObj = new GameObject("SafetyQuizManager");
                m_quizManager = quizObj.AddComponent<SafetyQuizManager>();
                // Pass the Canvas root so it can draw UI
                Canvas canvas = m_initialPanel.GetComponentInParent<Canvas>();
                if (canvas != null) m_quizManager.CreateQuizUI(canvas.gameObject);
            }
        }

        private void SetupVRInput()
        {
            // 1. Ensure EventSystem and correct Input Module
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var esObj = new GameObject("EventSystem");
                eventSystem = esObj.AddComponent<EventSystem>();
            }

            // Disable Standalone Input Module if present (conflicts with OVR)
            var standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null) standalone.enabled = false;

            // Ensure OVRInputModule
            var ovrModule = eventSystem.GetComponent<OVRInputModule>();
            if (ovrModule == null)
            {
                ovrModule = eventSystem.gameObject.AddComponent<OVRInputModule>();
            }
            // Default to Index Trigger OR Pinch (Button One) for clicking
            ovrModule.joyPadClickButton = OVRInput.Button.PrimaryIndexTrigger | OVRInput.Button.One | OVRInput.Button.SecondaryIndexTrigger;

            // 2. Ensure Canvas has OVRRaycaster
            Canvas canvas = m_initialPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                 // VR interaction requires World Space
                 if (canvas.renderMode != RenderMode.WorldSpace)
                 {
                     canvas.renderMode = RenderMode.WorldSpace;
                 }

                 // Reposition Canvas if it seems to be at origin (likely inside head)
                 // We only do this if it's very close to origin
                 if (canvas.transform.position.sqrMagnitude < 0.1f)
                 {
                     var rig = FindFirstObjectByType<OVRCameraRig>();
                     if (rig != null)
                     {
                         // Place 2.5m in front of camera rig, at eye level-ish (assuming rig is floor level or eye level)
                         // If rig is floor level, camera is at Y=1.7. If rig is eye level, camera is at Y=0.
                         // Safer to just put it Z=2.5 relative to rig.
                         canvas.transform.position = rig.transform.position + rig.transform.forward * 2.5f + Vector3.up * 1.5f;
                         canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - rig.transform.position);

                         // Scale might need adjustment if it was screen space. Ensure reasonable scale.
                         if (canvas.transform.localScale.x > 0.01f) // Text might be huge
                            canvas.transform.localScale = Vector3.one * 0.002f; // Typical UI scale for World Space
                     }
                 }

                 // Remove/Disable GraphicRaycaster if present as it might conflict or be useless in VR
                 var gr = canvas.GetComponent<GraphicRaycaster>();
                 if (gr != null) gr.enabled = false;

                 if (canvas.GetComponent<OVRRaycaster>() == null)
                 {
                     canvas.gameObject.AddComponent<OVRRaycaster>();
                 }

                 // Ensure world camera is set for proper interaction distance
                 var rigForCam = FindFirstObjectByType<OVRCameraRig>();
                 if (rigForCam != null)
                 {
                     canvas.worldCamera = rigForCam.centerEyeAnchor.GetComponent<Camera>();
                 }
            }

            // 3. Ensure Input Module has a ray source (Controller)
            // We can add the HandedInputSelector component if available, or a simple internal helper
            if (ovrModule != null)
            {
                // Add a simple helper to update the ray source
                if (FindFirstObjectByType<SimpleInputSelector>() == null)
                {
                    var selectorObj = new GameObject("SimpleInputSelector");
                    selectorObj.AddComponent<SimpleInputSelector>();
                }
            }
        }

        private class SimpleInputSelector : MonoBehaviour
        {
            private OVRCameraRig m_cameraRig;
            private OVRInputModule m_inputModule;
            private LineRenderer m_lineRenderer;

            private void Start()
            {
                m_cameraRig = FindFirstObjectByType<OVRCameraRig>();
                m_inputModule = FindFirstObjectByType<OVRInputModule>();

                // Setup visual laser
                m_lineRenderer = gameObject.AddComponent<LineRenderer>();
                m_lineRenderer.startWidth = 0.01f;
                m_lineRenderer.endWidth = 0.01f;
                m_lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                m_lineRenderer.startColor = Color.cyan;
                m_lineRenderer.endColor = new Color(0, 1, 1, 0);
                m_lineRenderer.useWorldSpace = true;
                // Add a default sorting order to ensure it's visible on top of UI
                m_lineRenderer.sortingOrder = 1000;
            }

            private void Update()
            {
                if (m_cameraRig == null || m_inputModule == null) return;

                Transform activeTransform = null;
                OVRInput.Controller activeController = OVRInput.GetActiveController();

                // Logic to select the best pointing source
                if ((activeController & OVRInput.Controller.LTouch) != 0 ||
                    (activeController & OVRInput.Controller.LHand) != 0)
                {
                    activeTransform = m_cameraRig.leftHandAnchor;
                }
                else
                {
                    // Default to right hand (Right Touch or Right Hand)
                    activeTransform = m_cameraRig.rightHandAnchor;
                }

                // Update Input Module which actually does the clicking
                m_inputModule.rayTransform = activeTransform;

                // Update Visuals and Interaction Feedback
                if (activeTransform != null)
                {
                    m_lineRenderer.SetPosition(0, activeTransform.position);
                    m_lineRenderer.SetPosition(1, activeTransform.position + activeTransform.forward * 3.0f); // 3m reach

                    // Visual feedback for click - Check ANY standard interaction button
                    bool isClicking = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger) ||
                                      OVRInput.Get(OVRInput.Button.One) ||
                                      OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger);

                    m_lineRenderer.startColor = isClicking ? Color.magenta : Color.cyan;
                    m_lineRenderer.endColor = isClicking ? Color.magenta : new Color(0, 1, 1, 0);
                }
            }
        }

        private void Update()
        {
            if (!IsInputActive)
                return;

            if (m_initialMenu)
            {
                InitialMenuUpdate();
            }
        }
        #endregion

        #region Ui state: No permissions Menu
        private void OnNoPermissionMenu()
        {
            m_initialMenu = false;
            IsPaused = true;
            m_initialPanel.SetActive(false);
            m_noPermissionPanel.SetActive(true);
        }
        #endregion

        #region Ui state: Initial Menu

        private void OnInitialMenu()
        {
            m_initialMenu = true;
            IsPaused = true;
            m_initialPanel.SetActive(true);
            m_noPermissionPanel.SetActive(false);
        }

        private void InitialMenuUpdate()
        {
            // Shortcut A: Fire Inspector
            if (OVRInput.GetUp(OVRInput.Button.One))
            {
                 m_buttonSound?.Play();
                 OnPauseMenu(false);
            }

            // Shortcut B: Learn Safety Hazard (Quiz)
            if (OVRInput.GetUp(OVRInput.Button.Two))
            {
                m_buttonSound?.Play();
                if (m_quizManager != null)
                {
                    m_quizManager.StartQuiz();
                    OnPauseMenu(false); // Hide main menu, start loop
                }
            }
        }

        private void CreateMainMenu()
        {
            // Create a container for the buttons
            GameObject menuContainer = new GameObject("MenuContainer");
            menuContainer.transform.SetParent(m_initialPanel.transform, false);

            // Allow this to be positioned - let's center it but move it down a bit
            RectTransform containerRect = menuContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = new Vector2(0, -150); // Shift down further below logo
            containerRect.sizeDelta = new Vector2(400, 300);

            // Add Layout Group
            VerticalLayoutGroup layout = menuContainer.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // Font to use
            Font font = m_labelInformation != null ? m_labelInformation.font : Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Create Buttons
            CreateButton(menuContainer, "Fire Inspector (A)", font, () =>
            {
                m_buttonSound?.Play();
                OnPauseMenu(false);
            });

            CreateButton(menuContainer, "Learn Safety Hazard (B)", font, () =>
            {
                m_buttonSound?.Play();
                if (m_quizManager != null)
                {
                    m_quizManager.StartQuiz();
                    OnPauseMenu(false);
                }
            });
        }

        private void CreateButton(GameObject parent, string text, Font font, UnityAction onClick)
        {
            // Use nicer font if available
            Font niceFont = Resources.Load<Font>("Fonts/Montserrat-Bold");
            Font useFont = niceFont != null ? niceFont : font;

            GameObject buttonObj = new GameObject(text + "Button");
            buttonObj.transform.SetParent(parent.transform, false);

            Image img = buttonObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f); // Dark gray background

            Button btn = buttonObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            btn.targetGraphic = img;

            // Setup Highlight colors
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            cb.highlightedColor = Color.cyan; // Bright highlight
            cb.pressedColor = Color.green;
            cb.selectedColor = Color.cyan;
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            cb.colorMultiplier = 1;
            cb.fadeDuration = 0.1f;
            btn.colors = cb;

            // Set size
            LayoutElement layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 60;
            layoutElement.preferredHeight = 60;

            // Create Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            Text txt = textObj.AddComponent<Text>();
            txt.text = text;
            txt.font = useFont;
            txt.fontSize = 24;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void ShowToast(string message)
        {
            // Simple toast implementation using the existing label info or a temporary text
            if (m_labelInformation != null)
            {
                StartCoroutine(ToastRoutine(message));
            }
        }

        private IEnumerator ToastRoutine(string message)
        {
            string original = m_labelInformation.text;
            m_labelInformation.text = message;
            yield return new WaitForSeconds(2.0f);
            m_labelInformation.text = original;
        }



        private void OnPauseMenu(bool visible)
        {
            m_initialMenu = false;
            IsPaused = visible;

            m_initialPanel.SetActive(false);
            m_noPermissionPanel.SetActive(false);

            OnPause?.Invoke(visible);
        }

        public void GoToMainMenu()
        {
            OnInitialMenu();
        }
        #endregion

        #region Ui state: detection information
        private void UpdateLabelInformation()
        {
            string text = $"Unity Sentis version: 2.1.3\nAI model: Yolo\nDetecting objects: {m_objectsDetected}\nObjects identified: {m_objectsIdentified}";
            if (!m_initialMenu)
            {
                text += "\n\nPress A to mark object";
            }
            m_labelInformation.text = text;
        }

        public void OnObjectsDetected(int objects)
        {
            m_objectsDetected = objects;
            UpdateLabelInformation();
        }

        public void OnObjectsIndentified(int objects)
        {
            if (objects < 0)
            {
                // reset the counter
                m_objectsIdentified = 0;
            }
            else
            {
                m_objectsIdentified += objects;
            }
            UpdateLabelInformation();
        }
        #endregion
    }
}
