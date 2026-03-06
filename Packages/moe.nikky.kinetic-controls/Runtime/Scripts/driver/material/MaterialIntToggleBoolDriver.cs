using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.material
{
    public class MaterialIntToggleBoolDriver : BoolDriver
    {
        [SerializeField] private Material[] materials;
        [SerializeField] private string[] propertyNames = { };
        private int[] _propertyIds = { };
        protected override string LogPrefix => nameof(MaterialIntToggleBoolDriver);
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

        public override void UpdateBool(bool value)
        {
            if (!enabled) return;
            var intVal = value ? 1 : 0;
            for (var i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                for (var j = 0; j < _propertyIds.Length; j++)
                {
                    Log($"Set {propertyNames[j]} to {intVal} {value}");
                    mat.SetInt(_propertyIds[j], intVal);
                }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
                materials[i].MarkDirty();
#endif
            }
        }
    
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyBoolValue(bool value)
        {
            InitProperties();
            
            UpdateBool(value);
        }
#endif
    }
}
