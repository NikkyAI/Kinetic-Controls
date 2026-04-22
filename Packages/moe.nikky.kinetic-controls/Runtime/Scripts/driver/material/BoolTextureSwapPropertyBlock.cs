using nikkyai.ArrayExtensions;
using nikkyai.common;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace nikkyai.driver.material
{
    public class BoolTextureSwapPropertyBlock : BoolDriver
    {
        [SerializeField] private Renderer materialSource;
        // [SerializeField] private Material[] materials = { };
        // [SerializeField] private MeshRenderer[] renderers = { };
        [FormerlySerializedAs("property")] [SerializeField] private string propertyName;
        [SerializeField] private RenderTexture disabledTexture;
        [SerializeField] private RenderTexture enabledTexture;

        // private Material[] _materials = { };
        private int _propertyId;
        private MaterialPropertyBlock _propertyBlock;
    
        void Start()
        {
            _EnsureInit();
        }

        protected override string LogPrefix => nameof(BoolTextureSwapPropertyBlock);

        protected override void _Init()
        {
            base._Init();
            
            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
            if (_propertyId == 0) _propertyId = VRCShader.PropertyToID(propertyName);
        }

        public override void OnUpdateBool(bool value)
        {
            if (!enabled) return;
            
            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
            if (_propertyId == 0) _propertyId = VRCShader.PropertyToID(propertyName);

            materialSource.GetPropertyBlock(_propertyBlock);
            if (value)
            {
                Log($"applying texture {enabledTexture}");
                // for (var i = 0; i < _materials.Length; i++)
                // {
                //     _materials[i].SetTexture(_propertyId, enabledTexture);
                // }
                _propertyBlock.SetTexture(_propertyId, enabledTexture, RenderTextureSubElement.Color);
                // _propertyBlock.SetTexture(_propertyId, enabledTexture);
            }
            else
            {
                Log($"applying texture {disabledTexture}");
                // for (var i = 0; i < _materials.Length; i++)
                // {
                //     _materials[i].SetTexture(_propertyId, disabledTexture);
                // }
                _propertyBlock.SetTexture(_propertyId, disabledTexture, RenderTextureSubElement.Color);
            }
            materialSource.SetPropertyBlock(_propertyBlock);
        
        }
    }
}
