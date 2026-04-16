using nikkyai.ArrayExtensions;
using nikkyai.common;
using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.material
{
    public class VectorMaterialDriver : VectorDriver
    {
        [SerializeField] private Material[] materials = { };
        [SerializeField] private MeshRenderer[] renderers = { };
        [SerializeField] private string[] propertyNames = { };
        private Material[] _materials = { };
        private int[] _propertyIds = { };
        // private Vector4 _lastValue = Vector4.negativeInfinity;

        protected override string LogPrefix => nameof(VectorMaterialDriver);

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
            if (Utilities.IsValid(materials))
            {
                _materials = _materials.AddRange(materials);
            }

            for (var index = 0; index < renderers.Length; index++)
            {
                var meshRenderer =  renderers[index];
                if(Utilities.IsValid(meshRenderer))
                {
                    _materials = _materials.AddRange(meshRenderer.materials);
                }
            }
            
            _propertyIds = new int[propertyNames.Length];
            for (var i = 0; i < propertyNames.Length; i++)
            {
                _propertyIds[i] = VRCShader.PropertyToID(propertyNames[i]);
                Log($"property {propertyNames[i]} => {_propertyIds[i]}");
            }
        }

        protected override void OnUpdateVector(Vector4 value)
        {
            if (!enabled) return;
            // if (_lastValue == value) return;
            // Log($"UpdateFloat {value} on {materials.Length} materials {_propertyIds.Length} properties");
            for (var i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                for (var j = 0; j < _propertyIds.Length; j++)
                {
                    Log($"Set {propertyNames[j]} to {value}");
                    mat.SetVector(_propertyIds[j], value);
                }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
                mat.MarkDirty();
#endif
            }

            // _lastValue = value;
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyVectorValue(Vector4 value)
        {
            base.ApplyVectorValue(value);
            Log($"applying new value: {value}");
            _EnsureInit();
            UpdateVector(value);
        }
#endif
    }
}
