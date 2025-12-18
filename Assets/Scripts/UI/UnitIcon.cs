using UnityEngine;
using UnityEngine.UI;
using TacticalGame.Units;

namespace TacticalGame.UI
{
    /// <summary>
    /// UI icon representing a unit's status.
    /// </summary>
    public class UnitIcon : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Image iconImage;
        
        #endregion

        #region Private State

        private UnitStatus targetUnit;
        private Color teamColor;

        #endregion

        #region Public Properties

        public UnitStatus TargetUnit => targetUnit;

        #endregion

        #region Setup

        /// <summary>
        /// Initialize the icon with a unit and team color.
        /// </summary>
        public void Setup(UnitStatus unit, Color color)
        {
            targetUnit = unit;
            teamColor = color;
            
            if (iconImage == null)
            {
                iconImage = GetComponent<Image>();
            }
            
            if (iconImage != null)
            {
                iconImage.color = teamColor;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (iconImage == null) return;

            if (targetUnit == null)
            {
                iconImage.color = Color.black;
                return;
            }

            if (targetUnit.HasSurrendered)
            {
                iconImage.color = Color.grey;
                return;
            }

            iconImage.color = teamColor;
        }

        #endregion
    }
}