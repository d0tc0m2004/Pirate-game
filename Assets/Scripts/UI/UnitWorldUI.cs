using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticalGame.Units;

namespace TacticalGame.UI
{
    /// <summary>
    /// World-space UI displaying unit stats (HP, Morale, Buzz, Arrows).
    /// </summary>
    public class UnitWorldUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private UnitStatus unitStatus;

        [Header("Bars")]
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Slider moraleSlider;
        [SerializeField] private Slider buzzSlider;
        [SerializeField] private Slider hullSlider;

        [Header("Text Numbers")]
        [SerializeField] private TMP_Text hpNumberText;
        [SerializeField] private TMP_Text moraleNumberText;
        [SerializeField] private TMP_Text arrowText;
        [SerializeField] private TMP_Text hullNumberText;

        #endregion

        #region Private State

        private Image buzzFillImage;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (unitStatus == null)
            {
                unitStatus = GetComponentInParent<UnitStatus>();
            }

            // Cache buzz slider fill image
            if (buzzSlider != null && buzzSlider.fillRect != null)
            {
                buzzFillImage = buzzSlider.fillRect.GetComponent<Image>();
            }
        }

        private void Update()
        {
            if (unitStatus == null) return;

            UpdateHPBar();
            UpdateMoraleBar();
            UpdateBuzzBar();
            UpdateHullBar();
            UpdateTexts();
        }

        #endregion

        #region UI Updates

        private void UpdateHPBar()
        {
            if (hpSlider == null) return;
            
            hpSlider.maxValue = unitStatus.MaxHP;
            hpSlider.value = unitStatus.CurrentHP;
        }

        private void UpdateMoraleBar()
        {
            if (moraleSlider == null) return;
            
            moraleSlider.maxValue = unitStatus.MaxMorale;
            moraleSlider.value = unitStatus.CurrentMorale;
        }

        private void UpdateBuzzBar()
        {
            if (buzzSlider == null) return;
            
            buzzSlider.maxValue = unitStatus.MaxBuzz;
            buzzSlider.value = unitStatus.CurrentBuzz;

            if (buzzFillImage != null)
            {
                buzzFillImage.color = unitStatus.IsTooDrunk ? Color.green : Color.yellow;
            }
        }

        private void UpdateHullBar()
        {
            if (hullSlider == null) return;
            
            hullSlider.maxValue = unitStatus.MaxHullPool;
            hullSlider.value = unitStatus.CurrentHullPool;
        }

        private void UpdateTexts()
        {
            if (hpNumberText != null)
            {
                hpNumberText.text = unitStatus.CurrentHP.ToString();
            }

            if (moraleNumberText != null)
            {
                moraleNumberText.text = unitStatus.CurrentMorale.ToString();
            }

            if (arrowText != null)
            {
                arrowText.text = unitStatus.CurrentArrows.ToString();
            }

            if (hullNumberText != null)
            {
                hullNumberText.text = unitStatus.CurrentHullPool.ToString();
            }
        }

        #endregion
    }
}