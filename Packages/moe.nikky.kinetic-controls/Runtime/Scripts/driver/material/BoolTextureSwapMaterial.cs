using nikkyai.common;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDKBase;

namespace nikkyai.driver.material
{
    public class BoolTextureSwapMaterial : BoolDriver
    {
        [SerializeField] private Material[] materials = { };
        // [SerializeField] private MeshRenderer[] renderers = { };
        [SerializeField] private string property;
        [SerializeField] private RenderTexture disabledTexture;
        [SerializeField] private RenderTexture enabledTexture;

        private int _propertyId;
    
        void Start()
        {
            _EnsureInit();
        }

        protected override string LogPrefix => nameof(BoolTextureSwapPropertyBlock);

        protected override void _Init()
        {
            base._Init();

            _propertyId = VRCShader.PropertyToID(property);
        }

        public override void OnUpdateBool(bool value)
        {
            if (!enabled) return;
            
            if (value)
            {
                Log($"applying texture {enabledTexture}");
                for (var i = 0; i < materials.Length; i++)
                {
                    materials[i].SetTexture(_propertyId, enabledTexture);
                }
            }
            else
            {
                Log($"applying texture {disabledTexture}");
                for (var i = 0; i < materials.Length; i++)
                {
                    materials[i].SetTexture(_propertyId, disabledTexture);
                }
            }
        
        }
    }
}
