using nikkyai.Kinetic_Controls.Drivers;
using UdonSharp;
using UnityEngine;
using static VRC.SDKBase.VRCShader;

namespace nikkyai.Kinetic_Controls
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PickupFaderAudiolink : PickupFaderAbstract
    {
        [Space(10)] // space
        [Header("Audiolink")] // header
        [SerializeField] private AudiolinkField field;
        [SerializeField] private AudioLink.AudioLink audioLink;
        [SerializeField] private AudioLink.AudioLinkController audioLinkController;
        private Material _audioLinkUI;
        private int _propertyId;

        protected override string LogPrefix => nameof(PickupFaderAudiolink);

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

        protected override void _UpdateFloat(float val)
        {
            _audioLinkUI.SetFloat(_propertyId, val);

            if (field == AudiolinkField.Bass)
            {
                audioLink.threshold0 = val;
                audioLinkController.threshold0Slider.SetValueWithoutNotify(audioLink.threshold0);
            }
            else if (field == AudiolinkField.LowMid)
            {
                audioLink.threshold1 = val;
                audioLinkController.threshold1Slider.SetValueWithoutNotify(audioLink.threshold1);
            }
            else if (field == AudiolinkField.HighMid)
            {
                audioLink.threshold2 = val;
                audioLinkController.threshold2Slider.SetValueWithoutNotify(audioLink.threshold2);
            }
            else if (field == AudiolinkField.Treble)
            {
                audioLink.threshold3 = val;
                audioLinkController.threshold3Slider.SetValueWithoutNotify(audioLink.threshold3);
            }
            else if (field == AudiolinkField.Gain)
            {
                audioLink.gain = val;
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