using TMPro;
using UnityEngine;

namespace SignalLost
{
    /// <summary>Fills a text label with Resources/Credits.txt, so the Credits screen always matches the credits file.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class CreditsText : MonoBehaviour
    {
        void Awake()
        {
            var asset = Resources.Load<TextAsset>("Credits");
            if (asset != null) GetComponent<TMP_Text>().text = asset.text;
        }
    }
}
