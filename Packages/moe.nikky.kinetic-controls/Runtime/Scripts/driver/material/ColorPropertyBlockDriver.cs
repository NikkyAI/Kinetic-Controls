using nikkyai.ArrayExtensions;
using nikkyai.common;
using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace nikkyai.driver.material
{
    public class ColorPropertyBlockDriver : ColorDriver
    {
        [SerializeField] private Renderer materialSource;
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
        //     _propertyIds = new int[propertyNames.Length];
        //     for (var i = 0; i < propertyNames.Length; i++)
        //     {
        //         _propertyIds[i] = VRCShader.PropertyToID(propertyNames[i]);
        //         Log($"property {propertyNames[i]} => {_propertyIds[i]}");
        //     }
        }

        public override void OnUpdateColor(Color value)
        {
            if (!enabled) return;
            
            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
            if (_propertyId == 0) _propertyId = VRCShader.PropertyToID(propertyName);
            
            materialSource.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_propertyId, value);
            materialSource.SetPropertyBlock(_propertyBlock);
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
