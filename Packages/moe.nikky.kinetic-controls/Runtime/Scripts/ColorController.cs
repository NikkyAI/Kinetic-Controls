using BestHTTP.Forms;
using nikkyai.common;
using UdonSharp;
using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace nikkyai
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ColorController : LoggerBase
    {
        [SerializeField] private Material[] materials;
        [SerializeField] private string[] propertyNames = { };
        private int[] _propertyIds = { };
        protected override string LogPrefix => nameof(ColorController);
        void Start()
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

        public float hue = 0.5f;
        public float saturation = 0.5f;
        public float brightness = 0.5f;

        public void UpdateColor()
        {
            // if (!enabled) return;
            Color value = Color.HSVToRGB(hue, saturation, brightness);
            for (var i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                for (var j = 0; j < _propertyIds.Length; j++)
                {
                    // Log($"Set {propertyNames[j]} to {value}");
                    mat.SetColor(_propertyIds[j], value);
                }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
                materials[i].MarkDirty();
#endif
            }
        }
    }
}
