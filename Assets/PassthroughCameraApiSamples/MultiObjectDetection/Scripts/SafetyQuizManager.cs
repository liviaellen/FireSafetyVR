using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    public class SafetyQuizManager : MonoBehaviour
    {
        [Header("Quiz UI")]
        [SerializeField] private GameObject m_quizPanel;
        [SerializeField] private Text m_questionText;
        [SerializeField] private Button m_yesButton;
        [SerializeField] private Button m_noButton;
        [SerializeField] private Text m_resultText;
        [SerializeField] private Text m_currentScoreText; // Shows score during quiz
        [SerializeField] private GameObject m_finalScorePanel;
        [SerializeField] private Text m_finalScoreText;
        [SerializeField] private Text m_finalMessageText;
        [SerializeField] private Button m_restartButton;

        [Header("Settings")]
        [SerializeField] private int m_questionsToAsk = 3;

        private int m_currentQuestionCount = 0;
        private int m_currentScore = 0;
        private bool m_isQuizActive = false;
        private GameObject m_currentSelectedObject;
        private bool m_currentIsFireHazard;
        private bool m_canAnswer = false;

        public bool IsQuizActive => m_isQuizActive;

        private DetectionUiMenuManager m_menuManager;
        private SentisInferenceUiManager m_inferenceUi;

        private void Start()
        {
            m_menuManager = FindFirstObjectByType<DetectionUiMenuManager>();
            m_inferenceUi = FindFirstObjectByType<SentisInferenceUiManager>();

            // Ensure UI is hidden at start
            if (m_quizPanel) m_quizPanel.SetActive(false);
            if (m_finalScorePanel) m_finalScorePanel.SetActive(false);

            // Wire up buttons if assigned in inspector (programmatic fallback below)
            if (m_yesButton) m_yesButton.onClick.AddListener(() => AnswerQuestion(true));
            if (m_noButton) m_noButton.onClick.AddListener(() => AnswerQuestion(false));
            if (m_restartButton) m_restartButton.onClick.AddListener(EndQuiz);
        }

        public void StartQuiz()
        {
            m_isQuizActive = true;
            m_currentQuestionCount = 0;
            m_currentScore = 0;
            m_currentSelectedObject = null;

            if (m_quizPanel) m_quizPanel.SetActive(false); // Only show when an object is selected
            if (m_finalScorePanel) m_finalScorePanel.SetActive(false);

            // Tell inference UI to hide labels
            if (m_inferenceUi) m_inferenceUi.SetQuizMode(true);

            Debug.Log("Safety Quiz Started! Select an object.");
        }

        public void OnObjectSelected(GameObject selectedObj, bool isFireHazard)
        {
            if (!m_isQuizActive) return;
            if (m_currentQuestionCount >= m_questionsToAsk) return;

            m_currentSelectedObject = selectedObj;
            m_currentIsFireHazard = isFireHazard;

            // Try to get class name
            string className = "object";
            var marker = selectedObj.GetComponent<DetectionSpawnMarkerAnim>();
            if (marker != null)
            {
                className = marker.GetYoloClassName();
            }

            // Show Question UI
            if (m_quizPanel)
            {
                m_quizPanel.SetActive(true);
                m_questionText.text = $"Is this {className} a Fire Hazard?";
                m_resultText.text = ""; // Clear previous result

                // We need to store the correct answer for the button callbacks

                // Prevent immediate input processing (debounce)
                m_canAnswer = false;
                StartCoroutine(EnableAnswerInput());
            }
        }

        private IEnumerator EnableAnswerInput()
        {
            yield return new WaitForSeconds(0.5f);
            m_canAnswer = true;
        }

        public void AnswerQuestion(bool userSaidYes)
        {
            if (!m_canAnswer) return;
            if (m_currentSelectedObject == null) return;

            // Use the stored truth value (reliable)
            bool isActuallyHazard = m_currentIsFireHazard;

            bool isCorrect = (userSaidYes == isActuallyHazard);

            if (isCorrect)
            {
                m_currentScore++;
                m_resultText.text = "Correct!";
                m_resultText.color = Color.green;
            }
            else
            {
                m_resultText.text = "Wrong!";
                m_resultText.color = Color.red;
            }

            m_currentQuestionCount++;

            // Update current score display
            if (m_currentScoreText) m_currentScoreText.text = $"Score: {m_currentScore}/{m_currentQuestionCount}";

            StartCoroutine(NextQuestionRoutine());
        }

        private IEnumerator NextQuestionRoutine()
        {
            yield return new WaitForSeconds(1.5f);

            m_quizPanel.SetActive(false);
            m_currentSelectedObject = null;

            if (m_currentQuestionCount >= m_questionsToAsk)
            {
                ShowFinalScore();
            }
        }

        private void ShowFinalScore()
        {
            if (m_finalScorePanel)
            {
                m_finalScorePanel.SetActive(true);
                m_finalMessageText.text = "Keep learning and stay safe!";

                // Auto-return to menu after 3 seconds
                StartCoroutine(ReturnToMenuAfterDelay());
            }
        }

        private IEnumerator ReturnToMenuAfterDelay()
        {
            yield return new WaitForSeconds(3f);
            EndQuiz();
        }

        public void EndQuiz()
        {
            m_isQuizActive = false;
            if (m_quizPanel) m_quizPanel.SetActive(false);
            if (m_finalScorePanel) m_finalScorePanel.SetActive(false);

            // Restore normal UI
            if (m_inferenceUi) m_inferenceUi.SetQuizMode(false);

            // Return to main menu
            if (m_menuManager) m_menuManager.GoToMainMenu();
        }

        private void Update()
        {
            if (!m_isQuizActive || m_currentSelectedObject == null) return;

            // Shortcuts for answering
            // YES = Index Finger (Trigger) or Button A
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger) || OVRInput.GetUp(OVRInput.Button.One))
            {
                AnswerQuestion(true);
            }
            // NO = Middle Finger (Grip) or Button B
            else if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger) || OVRInput.GetUp(OVRInput.Button.Two))
            {
                AnswerQuestion(false);
            }
        }

        // Programmatic UI Creation helper (since we don't have a prefab yet)
        public void CreateQuizUI(GameObject canvasRoot)
        {
            // Create a panel for the quiz question
            GameObject panelObj = new GameObject("QuizPanel");
            panelObj.transform.SetParent(canvasRoot.transform, false);
            m_quizPanel = panelObj;

            Image bg = panelObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.9f);

            RectTransform rt = panelObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(500, 350); // Slightly larger for instructions
            rt.anchoredPosition = new Vector2(0, 0);

            // Question Text
            GameObject qTextObj = new GameObject("QuestionText");
            qTextObj.transform.SetParent(panelObj.transform, false);
            m_questionText = qTextObj.AddComponent<Text>();
            m_questionText.font = Resources.Load<Font>("Fonts/Montserrat-Bold");
            m_questionText.fontSize = 28;
            m_questionText.alignment = TextAnchor.MiddleCenter;
            m_questionText.color = Color.white;
            m_questionText.text = "Is this object a Fire Hazard?";
            RectTransform qRt = qTextObj.GetComponent<RectTransform>();
            qRt.anchorMin = new Vector2(0, 0.6f);
            qRt.anchorMax = new Vector2(1, 0.9f);
            qRt.offsetMin = Vector2.zero; qRt.offsetMax = Vector2.zero;

            // Simple Instruction Text
            GameObject infoTextObj = new GameObject("InfoText");
            infoTextObj.transform.SetParent(panelObj.transform, false);
            Text infoText = infoTextObj.AddComponent<Text>();
            infoText.font = Resources.Load<Font>("Fonts/Montserrat-Bold");
            infoText.fontSize = 16;
            infoText.alignment = TextAnchor.MiddleCenter;
            infoText.color = Color.yellow;
            infoText.text = "Index Finger (A) = YES  |  Middle Finger (B) = NO";
            RectTransform iRt = infoTextObj.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0, 0.55f);
            iRt.anchorMax = new Vector2(1, 0.65f);
            iRt.offsetMin = Vector2.zero; iRt.offsetMax = Vector2.zero;

            // Yes Button
            m_yesButton = CreateSimpleButton(panelObj, "YES", new Vector2(-100, -20), Color.green, () => AnswerQuestion(true));
            // No Button
            m_noButton = CreateSimpleButton(panelObj, "NO", new Vector2(100, -20), Color.red, () => AnswerQuestion(false));

            // Result Text
            GameObject rTextObj = new GameObject("ResultText");
            rTextObj.transform.SetParent(panelObj.transform, false);
            m_resultText = rTextObj.AddComponent<Text>();
            m_resultText.font = Resources.Load<Font>("Fonts/Montserrat-Bold");
            m_resultText.fontSize = 28;
            m_resultText.alignment = TextAnchor.MiddleCenter;
            m_resultText.text = "";
            RectTransform rRt = rTextObj.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0, 0.2f);
            rRt.anchorMax = new Vector2(1, 0.4f);
            rRt.offsetMin = Vector2.zero;
            rRt.offsetMax = Vector2.zero;

            // Current Score Text (bottom of panel)
            GameObject scoreTextObj = new GameObject("CurrentScoreText");
            scoreTextObj.transform.SetParent(panelObj.transform, false);
            m_currentScoreText = scoreTextObj.AddComponent<Text>();
            m_currentScoreText.font = Resources.Load<Font>("Fonts/Montserrat-Bold");
            m_currentScoreText.fontSize = 20;
            m_currentScoreText.alignment = TextAnchor.MiddleCenter;
            m_currentScoreText.color = Color.cyan;
            m_currentScoreText.text = "Score: 0/0";
            RectTransform scoreRt = scoreTextObj.GetComponent<RectTransform>();
            scoreRt.anchorMin = new Vector2(0, 0);
            scoreRt.anchorMax = new Vector2(1, 0.15f);
            scoreRt.anchoredPosition = new Vector2(0, -10); // Moved lower

            // Final Score Panel - Only shows encouragement message
            m_finalScorePanel = new GameObject("FinalScorePanel");
            m_finalScorePanel.transform.SetParent(canvasRoot.transform, false);
            Image fbg = m_finalScorePanel.AddComponent<Image>();
            fbg.color = new Color(0,0,0,0.95f);
            RectTransform frt = m_finalScorePanel.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0.5f, 0.5f);
            frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = new Vector2(400, 200); // Smaller since no title/score
            frt.anchoredPosition = new Vector2(0, 0);

            // Only the encouragement message
            GameObject fMsg = new GameObject("FinalMessage");
            fMsg.transform.SetParent(m_finalScorePanel.transform, false);
            m_finalMessageText = fMsg.AddComponent<Text>();
            m_finalMessageText.font = Resources.Load<Font>("Fonts/Montserrat-Bold");
            m_finalMessageText.fontSize = 24;
            m_finalMessageText.alignment = TextAnchor.MiddleCenter;
            m_finalMessageText.color = Color.yellow;
            RectTransform fmrt = fMsg.GetComponent<RectTransform>();
            fmrt.anchorMin = new Vector2(0, 0.4f);
            fmrt.anchorMax = new Vector2(1, 0.7f);
            fmrt.offsetMin = Vector2.zero;
            fmrt.offsetMax = Vector2.zero;

            // No close button - quiz auto-returns to menu after delay
            m_quizPanel.SetActive(false);
            m_finalScorePanel.SetActive(false);
        }

        private Button CreateSimpleButton(GameObject parent, string text, Vector2 pos, Color color, UnityAction action)
        {
            GameObject btnObj = new GameObject(text + "Button");
            btnObj.transform.SetParent(parent.transform, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = color;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(action);
            btn.targetGraphic = img;

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 50);
            rt.anchoredPosition = pos;

            GameObject fpsText = new GameObject("Text");
            fpsText.transform.SetParent(btnObj.transform, false);
            Text t = fpsText.AddComponent<Text>();
            t.text = text;
            t.font = Resources.Load<Font>("Fonts/Montserrat-Bold");
            t.fontSize = 20;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.black;

            RectTransform trt = fpsText.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

            return btn;
        }
    }
}
