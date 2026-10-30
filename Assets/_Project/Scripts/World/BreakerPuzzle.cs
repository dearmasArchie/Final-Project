using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Wing A puzzle: three breakers must be pulled in an order that is random each run and written in the
    /// maintenance log. A wrong lever sounds the alarm, resets the levers, and calls an Enforcer.
    /// Solving it removes the glass pod around Override Fuse 1.
    /// </summary>
    public class BreakerPuzzle : MonoBehaviour
    {
        [SerializeField] Lever[] levers;
        [SerializeField] DialogueSource maintenanceLog;
        [SerializeField] GameObject podGlass;
        [SerializeField] AlarmSpawner alarm;
        [SerializeField] GameObject alarmEnemyPrefab;

        Lever[] order;
        int step;

        public bool Solved { get; private set; }

        void Start()
        {
            if (GameManager.Instance.HasFuse(1))
            {
                Solve();
                return;
            }

            order = (Lever[])levers.Clone();
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            maintenanceLog.SetLines(
                "MAINTENANCE LOG – Breaker Room B-2",
                "After a surge, restart the fuse pod breakers in this order ONLY:\n\n" +
                $"1. {order[0].Label}    2. {order[1].Label}    3. {order[2].Label}",
                "Wrong sequence triggers a security alert. – J. Ocampo, Facilities");
        }

        public void OnLeverPulled(Lever lever)
        {
            if (Solved) return;

            if (lever == order[step])
            {
                lever.SetPulled(true);
                step++;
                if (step == order.Length) Solve();
                return;
            }

            step = 0;
            foreach (var l in levers) l.SetPulled(false);
            if (alarm != null && alarmEnemyPrefab != null) alarm.Spawn(alarmEnemyPrefab);
        }

        void Solve()
        {
            Solved = true;
            foreach (var l in levers) l.SetPulled(true);
            if (podGlass != null) podGlass.SetActive(false);
        }
    }
}
