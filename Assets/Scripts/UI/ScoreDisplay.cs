using UnityEngine;
using TMPro;
using MatchThree.Board;

namespace MatchThree.UI
{
    public class ScoreDisplay : MonoBehaviour
    {
        [SerializeField] private GameBoard _board;
        [SerializeField] private TextMeshProUGUI _scoreText;

        private void Start()
        {
            _board.ScoreManager.OnScoreChanged += UpdateScore;
            UpdateScore(0);
        }

        private void OnDestroy()
        {
            if (_board != null && _board.ScoreManager != null)
                _board.ScoreManager.OnScoreChanged -= UpdateScore;
        }

        private void UpdateScore(int score)
        {
            _scoreText.text = $"Score: {score}";
        }
    }
}
