using UnityEngine;
using static VRC.SDKBase.VRCShader;

namespace nikkyai.driver.audiolink
{
    public class AudiolinkDriver: FloatDriver
    {
        [Header("Audiolink")] // header
        [SerializeField] private AudiolinkField field;
        [SerializeField] private AudioLink.AudioLink audioLink;
        [SerializeField] private AudioLink.AudioLinkController audioLinkController;
        private Material _audioLinkUI;
        private int _propertyId;

        protected override string LogPrefix => nameof(AudiolinkDriver);

        void Start()
        {
            _EnsureInit();
        }

        protected override void _PreInit()
        {
            InitIDs();
            _audioLinkUI = audioLinkController.audioLinkUI;
        }
        
        private void InitIDs()
        {
            if (field == AudiolinkField.Bass)
            {
                _propertyId = PropertyToID("_Threshold0");
            }
            else if (field == AudiolinkField.LowMid)
            {
                _propertyId = PropertyToID("_Threshold1");
            }
            else if (field == AudiolinkField.HighMid)
            {
                _propertyId = PropertyToID("_Threshold2");
            }
            else if (field == AudiolinkField.Treble)
            {
                _propertyId = PropertyToID("_Threshold3");
            }
            else if (field == AudiolinkField.Gain)
            {
                _propertyId = PropertyToID("_Gain");
            }
            else
            {
                LogError($"Invalid field value {field}");
            }
        }
        
        public override void UpdateFloat(float value)
        {
            if (!enabled) return;
            _audioLinkUI.SetFloat(_propertyId, value);

            if (field == AudiolinkField.Bass)
            {
                audioLink.threshold0 = value;
                audioLinkController.threshold0Slider.SetValueWithoutNotify(audioLink.threshold0);
            }
            else if (field == AudiolinkField.LowMid)
            {
                audioLink.threshold1 = value;
                audioLinkController.threshold1Slider.SetValueWithoutNotify(audioLink.threshold1);
            }
            else if (field == AudiolinkField.HighMid)
            {
                audioLink.threshold2 = value;
                audioLinkController.threshold2Slider.SetValueWithoutNotify(audioLink.threshold2);
            }
            else if (field == AudiolinkField.Treble)
            {
                audioLink.threshold3 = value;
                audioLinkController.threshold3Slider.SetValueWithoutNotify(audioLink.threshold3);
            }
            else if (field == AudiolinkField.Gain)
            {
                audioLink.gain = value;
                audioLinkController.gainSlider.SetValueWithoutNotify(audioLink.gain);
            }
            else
            {
                LogError($"Invalid field value {field}");
                return;
            }
            
            audioLink.UpdateSettings();
        }
    }
}