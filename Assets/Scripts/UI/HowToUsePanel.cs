using System.Collections;
using UnityEngine;

namespace SolarSystemAR
{
    public class HowToUsePanel : MonoBehaviour
    {
        [SerializeField] private RectTransform card;
        [SerializeField] private CanvasGroup canvasGroup;

        private Coroutine anim;

        public void Open()
        {
            gameObject.SetActive(true);
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Animate(true));
        }

        public void Close()
        {
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Animate(false));
        }

        private IEnumerator Animate(bool show)
        {
            const float dur = 0.18f;
            float t = 0f;
            float startScale = show ? 0.92f : 1f;
            float endScale = show ? 1f : 0.92f;
            float startA = show ? 0f : 1f;
            float endA = show ? 1f : 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                if (card != null) card.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, k);
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(startA, endA, k);
                yield return null;
            }
            if (!show) gameObject.SetActive(false);
        }
    }
}
