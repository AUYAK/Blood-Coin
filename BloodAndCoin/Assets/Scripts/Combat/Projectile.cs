using System;
using System.Collections;
using UnityEngine;

namespace BloodAndCoin.Combat
{
    // Placeholder "arrow" — a plain square that lobs from attacker to defender along a
    // parabolic arc (навесная траектория). Doesn't gate or delay damage/HP/turn logic, which
    // already resolved by the time this plays — but a killed defender's GameObject is only
    // destroyed on arrival (via onArrival), not the instant the attack resolves, otherwise the
    // target visually vanishes before the arrow reaches it. Swap the sprite for real art
    // later; the arc math doesn't need to change.
    public class Projectile : MonoBehaviour
    {
        public void Launch(Vector3 start, Vector3 end, float duration, float arcHeight, Action onArrival = null)
        {
            StartCoroutine(Fly(start, end, duration, arcHeight, onArrival));
        }

        private IEnumerator Fly(Vector3 start, Vector3 end, float duration, float arcHeight, Action onArrival)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 linear = Vector3.Lerp(start, end, t);
                float arc = arcHeight * 4f * t * (1f - t); // 0 at both ends, peaks at t=0.5
                transform.position = linear + new Vector3(0f, arc, 0f);
                yield return null;
            }

            onArrival?.Invoke();
            Destroy(gameObject);
        }
    }
}
