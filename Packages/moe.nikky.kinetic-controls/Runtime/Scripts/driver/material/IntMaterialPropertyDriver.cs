using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.material
{
    public class IntMaterialPropertyDriver : IntDriver
    {
        [SerializeField] private Material[] materials;
        [SerializeField] private string[] propertyNames = { };
        private int[] _propertyIds = { };
        protected override string LogPrefix => nameof(IntMaterialPropertyDriver);
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


        public override void UpdateInt(int value)
        {
            if (!enabled) return;
            for (var i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                for (var j = 0; j < _propertyIds.Length; j++)
                {
                    Log($"Set {propertyNames[j]} to {value}");
                    mat.SetInt(_propertyIds[j], value);
                }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
                materials[i].MarkDirty();
#endif
            }
        }
    
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyIntValue(int value)
        {
            InitProperties();
            UpdateInt(value);
        }
#endif
    }
}
