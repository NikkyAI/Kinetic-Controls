using nikkyai.common;
using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.material
{
    public class FloatPropertyBlockDriver : FloatDriver
    {
        [Header("Property Block")] //
        [SerializeField]
        private Renderer materialSource;

        [SerializeField] private int materialIndex = 0;

        [SerializeField] private string propertyName = "";

        private int _propertyId;
        private MaterialPropertyBlock _propertyBlock;

        protected override string LogPrefix => nameof(FloatPropertyBlockDriver);

        public string PropertyName
        {
            get => propertyName;
            set
            {
                propertyName = value;
                _propertyBlock = new MaterialPropertyBlock();
                _propertyId = VRCShader.PropertyToID(propertyName);
            }
        }

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
        }

        protected override void OnUpdateFloat(float value)
        {
            if (!enabled) return;

            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
            if (_propertyId == 0) _propertyId = VRCShader.PropertyToID(propertyName);
            // if (_lastValue == value) return;
            Log($"UpdateFloat {propertyName} to {value} on {materialSource}");
            // _lastValue = value

            materialSource.GetPropertyBlock(_propertyBlock, materialIndex);
            _propertyBlock.SetFloat(_propertyId, value);
            materialSource.SetPropertyBlock(_propertyBlock, materialIndex);
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        protected override bool UpdateInEditor => true;
#endif
    }
}