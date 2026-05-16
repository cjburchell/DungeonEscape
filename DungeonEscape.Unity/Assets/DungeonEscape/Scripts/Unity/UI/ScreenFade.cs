using System.Collections;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed class ScreenFade : MonoBehaviour
    {
        private const float DefaultDuration = 0.2f;
        private static ScreenFade instance;
        private Texture2D texture;
        private float alpha;

        public static ScreenFade GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindAnyObjectByType<ScreenFade>();
            if (instance == null)
            {
                instance = new GameObject("ScreenFade").AddComponent<ScreenFade>();
            }

            return instance;
        }

        public Coroutine FadeTransition(MonoBehaviour owner, System.Action transitionAction)
        {
            return FadeTransition(owner, transitionAction, DefaultDuration);
        }

        public Coroutine FadeTransition(MonoBehaviour owner, System.Action transitionAction, float duration)
        {
            if (owner == null)
            {
                transitionAction?.Invoke();
                return null;
            }

            return owner.StartCoroutine(RunFadeTransition(transitionAction, duration));
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.black);
            texture.Apply();
        }

        private IEnumerator RunFadeTransition(System.Action transitionAction, float duration)
        {
            yield return FadeTo(1f, duration);
            transitionAction?.Invoke();
            yield return null;
            yield return FadeTo(0f, duration);
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            var startAlpha = alpha;
            if (duration <= 0f)
            {
                alpha = targetAlpha;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            alpha = targetAlpha;
        }

        private void OnGUI()
        {
            if (alpha <= 0f)
            {
                return;
            }

            var previousDepth = GUI.depth;
            var previousColor = GUI.color;
            GUI.depth = -2000;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), texture);
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }
}
