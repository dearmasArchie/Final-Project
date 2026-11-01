using UnityEngine;
using UnityEngine.UI;

namespace SignalLost
{
    /// <summary>
    /// Main menu backdrop: slowly pans and zooms through pictures of the island, cross-fading from one to the next.
    /// The pictures are rendered from the island scene by MenuBackgroundBuilder (bridge command: menu-bg).
    /// </summary>
    public class MenuBackground : MonoBehaviour
    {
        [SerializeField] Texture2D[] pictures;
        [SerializeField] RawImage front;
        [SerializeField] RawImage back;
        [SerializeField] float secondsPerPicture = 9f;
        [SerializeField] float fadeSeconds = 2f;
        [SerializeField] float zoomAmount = 0.08f;

        int index;
        float timer;

        void Start()
        {
            if (pictures == null || pictures.Length == 0 || front == null || back == null) { enabled = false; return; }
            index = Random.Range(0, pictures.Length);
            Show(front, index);
            Show(back, (index + 1) % pictures.Length);
            SetAlpha(back, 0f);
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / secondsPerPicture;
            Pan(front, index, t);
            float fade = Mathf.Clamp01((timer - (secondsPerPicture - fadeSeconds)) / fadeSeconds);
            SetAlpha(back, fade);
            Pan(back, (index + 1) % pictures.Length, 0f);

            if (timer >= secondsPerPicture)
            {
                // The incoming picture becomes the front one; the old front is reused for the one after
                (front, back) = (back, front);
                front.transform.SetSiblingIndex(0);
                back.transform.SetSiblingIndex(1);
                index = (index + 1) % pictures.Length;
                Show(back, (index + 1) % pictures.Length);
                SetAlpha(front, 1f);
                SetAlpha(back, 0f);
                timer = 0f;
            }
        }

        void Show(RawImage image, int i)
        {
            image.texture = pictures[i];
            image.uvRect = new Rect(0, 0, 1, 1);
        }

        static void SetAlpha(RawImage image, float a)
        {
            var c = image.color;
            c.a = a;
            image.color = c;
        }

        // Slow zoom towards a different corner for each picture, using the UV window so no layout changes are needed
        void Pan(RawImage image, int i, float t)
        {
            float zoom = Mathf.Clamp01(t) * zoomAmount;
            float size = 1f - zoomAmount * 0.5f - zoom * 0.5f;
            float ox = (i % 2 == 0 ? 0.25f : 0.75f) * (1f - size);
            float oy = (i % 3 == 0 ? 0.3f : 0.6f) * (1f - size);
            image.uvRect = new Rect(ox, oy, size, size);
        }
    }
}
