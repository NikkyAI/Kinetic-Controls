using nikkyai.common;
using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.material
{
    public class VectorPropertyBlockDriver : VectorDriver
    {
        [SerializeField] private Renderer materialSource;
        // [SerializeField] private string[] propertyNames = { };
        [SerializeField] private string propertyName = "";
        // private int[] _propertyIds = { };
        // private Vector4 _lastValue = Vector4.negativeInfinity;

        private int _propertyId;
        private MaterialPropertyBlock _propertyBlock;

        protected override string LogPrefix => nameof(VectorPropertyBlockDriver);

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
            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
            if (_propertyId == 0) _propertyId = VRCShader.PropertyToID(propertyName);
        //     _propertyIds = new int[propertyNames.Length];
        //     for (var i = 0; i < propertyNames.Length; i++)
        //     {
        //         _propertyIds[i] = VRCShader.PropertyToID(propertyNames[i]);
        //         Log($"property {propertyNames[i]} => {_propertyIds[i]}");
        //     }
        }

        protected override void OnUpdateVector(Vector4 value)
        {
            if (!enabled) return;
            
            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
            if (_propertyId == 0) _propertyId = VRCShader.PropertyToID(propertyName);
            // if (_lastValue == value) return;
            // Log($"UpdateFloat {value} on {materials.Length} materials {_propertyIds.Length} properties");
            // _lastValue = value
            
            materialSource.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetVector(_propertyId, value);
            materialSource.SetPropertyBlock(_propertyBlock);
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyVectorValue(Vector4 value)
        {
            base.ApplyVectorValue(value);
            Log($"applying new value: {value}");
            UpdateVector(value);
        }
#endif
    }
}
