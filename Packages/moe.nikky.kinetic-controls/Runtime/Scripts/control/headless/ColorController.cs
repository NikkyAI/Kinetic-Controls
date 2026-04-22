using JetBrains.Annotations;
using nikkyai.common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.control.headless
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ColorController : LoggerBase
    {
        // [SerializeField] private Material[] materials;
        // [FormerlySerializedAs("propertyNames")] //
        // [SerializeField] private string[] materialPropertyNames = { };
        // [Header("External Behaviours")] // header
        // [SerializeField] private UdonBehaviour[] externalBehaviours;
        // [SerializeField] private string colorPropertyField;
        [SerializeField] private GameObject colorDrivers;
        private ColorDriver[] _colorDrivers = {};
        
        // private int[] _propertyIds = { };
        protected override string LogPrefix => nameof(ColorController);
        void Start()
        {
            _EnsureInit();
        }
        
        protected override void _Init()
        {
            base._Init();
            InitDrivers();
        }

        private void InitDrivers()
        {
            
            Log($"Searching for float value drivers in {colorDrivers}");
            if (Utilities.IsValid(colorDrivers))
            {
                _colorDrivers = colorDrivers.GetComponents<ColorDriver>();
                Log($"found {_colorDrivers.Length} drivers for value");
            }
            else
            {
                LogError("missing object for color drivers");
            }
        }

        public float hue = 0.5f;
        public float saturation = 0.5f;
        public float brightness = 0.5f;
        private Color lastColor = Color.black;
        [UsedImplicitly]
        public void UpdateColor()
        {
            Color value = Color.HSVToRGB(hue, saturation, brightness);
            if (value != lastColor)
            {
                Log($"applying color {value} to {_colorDrivers.Length} drivers");
                for (var i = 0; i < _colorDrivers.Length; i++)
                {
                    _colorDrivers[i].OnUpdateColor(value);
                }
                lastColor = value;
            }
        }
    
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        [ContextMenu("Apply Color")]
        public void ApplyColor()
        {
            InitDrivers();
            Color value = Color.HSVToRGB(hue, saturation, brightness);
            foreach (var colorDriver in _colorDrivers)
            {
                Log($"Applying color {value} to driver {colorDriver}");
                colorDriver.ApplyColorValue(value);
            }
        }
#endif
    }
    
}
