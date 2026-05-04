using UnityEngine;
using TMPro;

namespace TacticalGame.UI
{
    /// <summary>
    /// Floating damage/heal number that spawns above a unit, drifts up, and fades out.
    /// Works in world space — no Canvas required.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        private TextMeshPro textMesh;
        private float lifetime;
        private float maxLifetime;
        private Vector3 moveDirection;
        private Color startColor;

        private const float DEFAULT_LIFETIME = 1.0f;
        private const float RISE_SPEED = 1.2f;
        private const float SPREAD = 0.3f;

        /// <summary>
        /// Spawn a damage popup at a world position.
        /// </summary>
        public static DamagePopup Create(Vector3 position, int amount, PopupType type)
        {
            // Create a new GameObject with TextMeshPro
            GameObject go = new GameObject($"DmgPopup_{amount}");
            go.transform.position = position + Vector3.up * 1.2f; // Above unit

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 2;
            tmp.sortingOrder = 100;
            
            // Enable face camera
            go.transform.rotation = Quaternion.identity;

            DamagePopup popup = go.AddComponent<DamagePopup>();
            popup.textMesh = tmp;
            popup.maxLifetime = DEFAULT_LIFETIME;
            popup.lifetime = DEFAULT_LIFETIME;
            
            // Random horizontal spread so overlapping popups don't stack
            float xSpread = Random.Range(-SPREAD, SPREAD);
            popup.moveDirection = new Vector3(xSpread, RISE_SPEED, 0f);

            // Style based on type
            switch (type)
            {
                case PopupType.Damage:
                    tmp.text = $"-{amount}";
                    popup.startColor = new Color(1f, 0.25f, 0.2f, 1f); // Red
                    tmp.fontSize = Mathf.Clamp(2 + amount / 100, 2, 5); // Bigger for bigger hits
                    break;

                case PopupType.Heal:
                    tmp.text = $"+{amount}";
                    popup.startColor = new Color(0.2f, 1f, 0.4f, 1f); // Green
                    break;

                case PopupType.HullDamage:
                    tmp.text = $"-{amount}";
                    popup.startColor = new Color(0.4f, 0.7f, 1f, 1f); // Blue (hull/shield)
                    tmp.fontSize = 2;
                    break;

                case PopupType.MoraleDamage:
                    tmp.text = $"-{amount}";
                    popup.startColor = new Color(1f, 0.85f, 0.2f, 1f); // Yellow (morale)
                    tmp.fontSize = 2;
                    break;

                case PopupType.Miss:
                    tmp.text = "MISS";
                    popup.startColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Gray
                    tmp.fontSize = 2;
                    break;

                case PopupType.Stun:
                    tmp.text = "STUNNED";
                    popup.startColor = new Color(1f, 1f, 0.3f, 1f); // Bright yellow
                    tmp.fontSize = 2;
                    break;

                case PopupType.Environmental:
                    tmp.text = $"-{amount}";
                    popup.startColor = new Color(1f, 0.6f, 0.2f, 1f); // Orange
                    break;
            }

            tmp.color = popup.startColor;
            return popup;
        }

        private void Update()
        {
            // Float upward
            transform.position += moveDirection * Time.deltaTime;

            // Fade out
            lifetime -= Time.deltaTime;
            float alpha = Mathf.Clamp01(lifetime / maxLifetime);
            
            if (textMesh != null)
            {
                Color c = startColor;
                c.a = alpha;
                textMesh.color = c;
            }

            // Face camera
            if (UnityEngine.Camera.main != null)
            {
                transform.forward = UnityEngine.Camera.main.transform.forward;
            }

            // Destroy when faded
            if (lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    public enum PopupType
    {
        Damage,
        Heal,
        HullDamage,
        MoraleDamage,
        Miss,
        Stun,
        Environmental
    }
}
