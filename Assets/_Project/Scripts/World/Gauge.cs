using UnityEngine;

namespace SignalLost
{
    /// <summary>A pressure gauge in Wing A: its needle points at this run's reading (0–9) for the breaker room code.</summary>
    public class Gauge : MonoBehaviour
    {
        /// <summary>Angle of the needle for a reading: 0 at the upper left, 9 at the upper right (270° sweep).</summary>
        public static float AngleFor(int value) => 135f - value * 30f;

        [SerializeField] Transform needlePivot;
        [Tooltip("0 = first gauge, 1 = second, 2 = third.")]
        [SerializeField] int index;

        void Start() => needlePivot.localRotation = Quaternion.Euler(0f, 0f, AngleFor(GameManager.Instance.GaugeReadings[index]));
    }
}
