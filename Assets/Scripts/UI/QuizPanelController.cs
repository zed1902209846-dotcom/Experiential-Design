using System;
using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    /// AR 场景内的小测验面板（参考 Kivicube 样式：底部卡片 + 三个选项 + 即时反馈）。
    /// 由 SceneBuilder 生成 UI 并绑定；题库为内置英文题（UI 全英文要求）。
    public class QuizPanelController : MonoBehaviour
    {
        [Serializable]
        public class Question
        {
            public string text;
            public string[] options;
            public int answer; // 正确选项下标
        }

        [SerializeField] private Text questionText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Button[] optionButtons = new Button[3];
        [SerializeField] private Text[] optionLabels = new Text[3];
        [SerializeField] private Button nextButton;
        [SerializeField] private Text nextLabel;
        [SerializeField] private Button closeButton;

        private static readonly Question[] Questions =
        {
            new Question { text = "Which planet is the largest in the Solar System?",
                options = new[] { "A. Earth", "B. Jupiter", "C. Mars" }, answer = 1 },
            new Question { text = "Which planet is closest to the Sun?",
                options = new[] { "A. Venus", "B. Earth", "C. Mercury" }, answer = 2 },
            new Question { text = "Which planet is known as the Red Planet?",
                options = new[] { "A. Mars", "B. Jupiter", "C. Neptune" }, answer = 0 },
            new Question { text = "Which planet has the most prominent rings?",
                options = new[] { "A. Venus", "B. Saturn", "C. Mercury" }, answer = 1 },
            new Question { text = "How many planets are in the Solar System?",
                options = new[] { "A. 7", "B. 8", "C. 9" }, answer = 1 },
            new Question { text = "Which planet spins on its side with a 98 degree tilt?",
                options = new[] { "A. Uranus", "B. Saturn", "C. Mars" }, answer = 0 },
            new Question { text = "Which planet is famous for its Great Red Spot storm?",
                options = new[] { "A. Mars", "B. Venus", "C. Jupiter" }, answer = 2 },
            new Question { text = "Which planet has the fastest winds, up to 2,100 km/h?",
                options = new[] { "A. Jupiter", "B. Neptune", "C. Earth" }, answer = 1 },
        };

        private int index;
        private int score;
        private bool answered;
        private bool finished;

        public void Open()
        {
            gameObject.SetActive(true);
            index = 0;
            score = 0;
            finished = false;
            ShowQuestion();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void ShowQuestion()
        {
            answered = false;
            var q = Questions[index];
            if (questionText != null) questionText.text = (index + 1) + "/" + Questions.Length + "  " + q.text;
            if (feedbackText != null) feedbackText.text = "";
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null) optionButtons[i].interactable = true;
                if (optionLabels != null && i < optionLabels.Length && optionLabels[i] != null)
                    optionLabels[i].text = q.options[i];
            }
            if (nextLabel != null) nextLabel.text = "SKIP";
            finished = false;
        }

        public void SelectOption(int i)
        {
            if (answered) return;
            answered = true;
            var q = Questions[index];
            bool correct = i == q.answer;
            if (correct) score++;
            if (feedbackText != null)
                feedbackText.text = correct ? "Correct! " + (index + 1) + "/" + Questions.Length + " right" : "Wrong. The answer is " + q.options[q.answer];
            foreach (var b in optionButtons)
                if (b != null) b.interactable = false;
            if (nextLabel != null) nextLabel.text = (index == Questions.Length - 1) ? "FINISH" : "NEXT";
        }

        public void Next()
        {
            if (finished)
            {
                index = 0;
                score = 0;
                ShowQuestion();
                return;
            }
            if (index < Questions.Length - 1)
            {
                index++;
                ShowQuestion();
            }
            else
            {
                finished = true;
                if (feedbackText != null) feedbackText.text = "Done! Your score: " + score + "/" + Questions.Length;
                if (questionText != null) questionText.text = "QUIZ COMPLETE";
                if (nextLabel != null) nextLabel.text = "RESTART";
            }
        }
    }
}
