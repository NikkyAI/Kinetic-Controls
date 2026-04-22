using nikkyai.common;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.driver.converter
{
    public class BoolToColorHDR : BoolDriver
    {
        [SerializeField, ColorUsage(true, true)] 
        private Color colorDisabled = Color.grey;

        [SerializeField, ColorUsage(true, true)]
        private Color colorEnabled = new Color(0f,0.25f,1f);
    
        [SerializeField] private GameObject colorDrivers;
        private ColorDriver[] _colorDrivers = {};
    
        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
        
            if (Utilities.IsValid(colorDrivers))
            {
                _colorDrivers = colorDrivers.GetComponents<ColorDriver>();
            }
        }

        protected override string LogPrefix => nameof(BoolToColorHDR);
        public override void OnUpdateBool(bool value)
        {
            if (!enabled) return;
            Color color = value ? colorEnabled : colorDisabled;
            Log($"updating color: {value} -> {color} on {_colorDrivers.Length} drivers");
            for (var i = 0; i < _colorDrivers.Length; i++)
            {
                _colorDrivers[i].OnUpdateColor(color);
            }
        }
    }
}
