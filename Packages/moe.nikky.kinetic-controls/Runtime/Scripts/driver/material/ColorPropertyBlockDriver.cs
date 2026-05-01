using nikkyai.common;
using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.material
{
    public class ColorPropertyBlockDriver : ColorDriver
    {
        [Header("Property Block")] //
        [SerializeField]
        private Renderer materialSource;

        [SerializeField] private int materialIndex = 0;
        [SerializeField] private string propertyName = "";

        private int _propertyId;
        private MaterialPropertyBlock _propertyBlock;

        protected override string LogPrefix => nameof(ColorDriver);

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

        public override void OnUpdateColor(Color value)
        {
            if (!enabled) return;

            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
            if (_propertyId == 0) _propertyId = VRCShader.PropertyToID(propertyName);

            materialSource.GetPropertyBlock(_propertyBlock, materialIndex);
            _propertyBlock.SetColor(_propertyId, value);
            materialSource.SetPropertyBlock(_propertyBlock, materialIndex);
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public override void ApplyColorValue(Color value)
        {
            base.ApplyColorValue(value);
            Log($"applying new value: {value}");
            OnUpdateColor(value);
        }
#endif
    }
}