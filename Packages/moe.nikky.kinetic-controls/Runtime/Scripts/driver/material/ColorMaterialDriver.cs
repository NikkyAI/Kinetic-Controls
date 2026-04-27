using nikkyai.common;
using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.material
{
    public class ColorMaterialDriver : ColorDriver
    {
        private Color _lastValue = Color.clear;
        [SerializeField] private Material[] materials;
        [SerializeField] private string[] propertyNames = { };
        private int[] _propertyIds = { };

        protected override string LogPrefix => nameof(ColorMaterialDriver);

        private void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            InitProperties();
        }

        private void InitProperties()
        {
            _propertyIds = new int[propertyNames.Length];
            for (var i = 0; i < propertyNames.Length; i++)
            {
                _propertyIds[i] = VRCShader.PropertyToID(propertyNames[i]);
                Log($"property {propertyNames[i]} => {_propertyIds[i]}");
            }
        }

        public override void OnUpdateColor(Color value)
        {
            if (!enabled) return;
//             if (_lastValue == value)
//             {
//                 
// #if UNITY_EDITOR && !COMPILER_UDONSHARP
//                 Log($"color is the same, skipping");
// #endif
//                 return;
//             }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            Log($"UpdateColor {value} on {materials.Length} materials {_propertyIds.Length} properties");
#endif
            for (var i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                for (var j = 0; j < _propertyIds.Length; j++)
                {
                    if (_lastValue != value)
                    {
                        // Log($"Set {propertyNames[j]} to {value}");
                        mat.SetColor(_propertyIds[j], value);
                    }
                }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
                mat.MarkDirty();
#endif
            }

            _lastValue = value;
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyColorValue(Color value)
        {
            base.ApplyColorValue(value);
            OnUpdateColor(value);
            Log("marking materials as Dirty");
            foreach (var material in materials)
            {
                material.MarkDirty();
            }
        }
#endif
    }
}