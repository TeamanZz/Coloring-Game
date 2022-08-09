using BizzyBeeGames.PictureColoring;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames
{
    public class SettingsPopup : Popup
    {
        #region Inspector Variables

        [Space]

        [SerializeField] private ToggleSlider musicToggle = null;
        [SerializeField] private ToggleSlider soundToggle = null;
        [SerializeField] private ToggleSlider holdSelectionToggle;
        #endregion

        #region Unity Methods

        private void Start()
        {
            musicToggle.SetToggle(SoundManager.Instance.IsMusicOn, false);
            soundToggle.SetToggle(SoundManager.Instance.IsSoundEffectsOn, false);
            holdSelectionToggle.SetToggle(true, false);

            musicToggle.OnValueChanged += OnMusicValueChanged;
            soundToggle.OnValueChanged += OnSoundEffectsValueChanged;
            holdSelectionToggle.OnValueChanged += OnHoldSelectionValueChanged;
        }

        #endregion

        #region Private Methods

        private void OnMusicValueChanged(bool isOn)
        {
            SoundManager.Instance.SetSoundTypeOnOff(SoundManager.SoundType.Music, isOn);
        }

        private void OnSoundEffectsValueChanged(bool isOn)
        {
            SoundManager.Instance.SetSoundTypeOnOff(SoundManager.SoundType.SoundEffect, isOn);
        }

        private void OnHoldSelectionValueChanged(bool isOn)
        {
            // if (GameScreen.Instance != null)
            GameScreen.Instance.SetHoldSelectednOff(isOn);
        }
        #endregion
    }
}
