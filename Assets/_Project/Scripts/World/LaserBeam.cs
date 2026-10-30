using UnityEngine;

namespace SignalLost
{
    /// <summary>Added to each beam by LaserTripwire; forwards trigger contacts to the emitter.</summary>
    public class LaserBeam : MonoBehaviour
    {
        public LaserTripwire owner;

        void OnTriggerEnter(Collider other) => owner.OnBeamTouched(other);
        void OnTriggerStay(Collider other) => owner.OnBeamTouched(other);
    }
}
