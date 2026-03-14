using UnityEngine;

namespace MatchThree.UI
{
    public class ScorePopup : MonoBehaviour
    {
        private TextMesh _text;
        private float _lifetime;
        private float _elapsed;
        private Vector3 _startPos;
        private Color _startColour;

        private const float RiseDistance = 1.5f;

        public void Init(int points, float duration = 1f)
        {
            _lifetime = duration;
            _startPos = transform.position;

            _text = gameObject.AddComponent<TextMesh>();
            _text.text = $"+{points}";
            _text.characterSize = points >= 300 ? 0.15f : points >= 150 ? 0.12f : 0.1f;
            _text.fontSize = 64;
            _text.color = Color.white;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;

            var customFont = Resources.Load<Font>("Fonts/monogram-extended");
            if (customFont != null)
            {
                _text.font = customFont;
                var mr = GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sortingOrder = 100;
                    mr.material = customFont.material;
                }
            }
            else
            {
                var mr = GetComponent<MeshRenderer>();
                if (mr != null)
                    mr.sortingOrder = 100;
            }

            _startColour = _text.color;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / _lifetime;

            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            // Rise
            transform.position = _startPos + Vector3.up * (RiseDistance * t);

            // Fade out in second half
            if (t > 0.5f)
            {
                float fadeT = (t - 0.5f) / 0.5f;
                var c = _startColour;
                c.a = 1f - fadeT;
                _text.color = c;
            }

            // Scale punch at start
            float scale = t < 0.1f ? Mathf.Lerp(0.5f, 1.2f, t / 0.1f) :
                          t < 0.2f ? Mathf.Lerp(1.2f, 1f, (t - 0.1f) / 0.1f) : 1f;
            transform.localScale = Vector3.one * scale;
        }
    }
}
