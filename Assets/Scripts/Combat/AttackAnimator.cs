using UnityEngine;
using System;
using System.Collections;

namespace TacticalGame.Combat
{
    /// <summary>
    /// Handles melee dash and ranged projectile attack animations.
    /// Attach to any unit that attacks. Delays damage until animation completes.
    /// </summary>
    public class AttackAnimator : MonoBehaviour
    {
        [Header("Melee Settings")]
        [SerializeField] private float dashSpeed = 12f;
        [SerializeField] private float dashPauseAtTarget = 1.0f;

        [Header("Ranged Settings")]
        [SerializeField] private float projectileSpeed = 15f;
        [SerializeField] private float projectileSize = 0.15f;

        private bool isAnimating = false;
        public bool IsAnimating => isAnimating;

        /// <summary>
        /// Play a melee dash attack: unit rushes to target, pauses, then returns.
        /// Calls onHit when reaching the target (for damage), and onComplete when back.
        /// </summary>
        public void PlayMeleeAttack(Transform target, Action onHit, Action onComplete)
        {
            if (isAnimating) { onHit?.Invoke(); onComplete?.Invoke(); return; }
            StartCoroutine(MeleeDashCoroutine(target, onHit, onComplete));
        }

        /// <summary>
        /// Play a ranged projectile attack: spawns a bullet that flies to the target.
        /// Calls onHit when bullet reaches target, and onComplete right after.
        /// </summary>
        public void PlayRangedAttack(Transform target, Action onHit, Action onComplete)
        {
            if (isAnimating) { onHit?.Invoke(); onComplete?.Invoke(); return; }
            StartCoroutine(RangedProjectileCoroutine(target, onHit, onComplete));
        }

        private IEnumerator MeleeDashCoroutine(Transform target, Action onHit, Action onComplete)
        {
            isAnimating = true;

            Vector3 startPos = transform.position;
            // Stop slightly before the target so we don't overlap
            Vector3 targetPos = target.position;
            Vector3 direction = (targetPos - startPos).normalized;
            Vector3 stopPos = targetPos - direction * 0.5f;

            // === DASH FORWARD ===
            float dist = Vector3.Distance(startPos, stopPos);
            float elapsed = 0f;
            float duration = dist / dashSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease-in for impact feel
                float eased = t * t;
                transform.position = Vector3.Lerp(startPos, stopPos, eased);
                yield return null;
            }
            transform.position = stopPos;

            // === HIT ===
            onHit?.Invoke();

            // Brief pause at target
            yield return new WaitForSeconds(dashPauseAtTarget);

            // === RETURN ===
            elapsed = 0f;
            float returnDuration = duration * 0.6f; // Return faster
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / returnDuration);
                transform.position = Vector3.Lerp(stopPos, startPos, t);
                yield return null;
            }
            transform.position = startPos;

            isAnimating = false;
            onComplete?.Invoke();
        }

        private IEnumerator RangedProjectileCoroutine(Transform target, Action onHit, Action onComplete)
        {
            isAnimating = true;

            Vector3 startPos = transform.position + Vector3.up * 0.5f;
            Vector3 targetPos = target.position + Vector3.up * 0.5f;

            // Create bullet
            GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.name = "Projectile";
            bullet.transform.position = startPos;
            bullet.transform.localScale = Vector3.one * projectileSize;

            // Remove collider so it doesn't interact with physics
            var col = bullet.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Style the bullet
            var rend = bullet.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Standard"));
                rend.material.color = new Color(1f, 0.85f, 0.2f); // Golden bullet
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.1f) * 2f);
            }

            // === FLY TO TARGET ===
            float dist = Vector3.Distance(startPos, targetPos);
            float duration = dist / projectileSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                bullet.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            // === HIT ===
            Destroy(bullet);
            onHit?.Invoke();

            isAnimating = false;
            onComplete?.Invoke();
        }
    }
}
