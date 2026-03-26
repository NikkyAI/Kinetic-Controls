using nikkyai.common;
using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.material
{
    public class FloatMaterialPropertyDriver : FloatDriver
    {
        private float _lastValue = 0.0f;
        [SerializeField] private Material[] materials;
        [SerializeField] private string[] propertyNames = { };
        private int[] _propertyIds = { };

        protected override string LogPrefix => nameof(FloatMaterialPropertyDriver);

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

        protected override void UpdateFloat(float value)
        {
            if (!enabled) return;
            if (_lastValue == value) return;
            // Log($"UpdateFloat {value} on {materials.Length} materials {_propertyIds.Length} properties");
            for (var i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                for (var j = 0; j < _propertyIds.Length; j++)
                {
                    if (_lastValue != value)
                    {
                        Log($"Set {propertyNames[j]} to {value}");
                        mat.SetFloat(_propertyIds[j], value);
                    }
                }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
                mat.MarkDirty();
#endif
            }

            _lastValue = value;
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyFloatValue(float value)
        {
            InitProperties();
            UpdateFloat(value);
        }
#endif
    }
}