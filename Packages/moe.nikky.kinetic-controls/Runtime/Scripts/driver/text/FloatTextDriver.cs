using System;
using TMPro;
using UnityEngine;
using VRC;

namespace nikkyai.driver.text
{
    public class FloatTextDriver : FloatDriver
    {
        [Header("TextMeshPro")] // header
        [SerializeField]
        private TextMeshPro textMeshPro;

        [Tooltip(
            "What the slider value will be formated as.\n" +
            "- 0.0 means it will always at least show one digit with one decimal point\n" +
            "- 00 means it will fill always be formated as two digits with no decimal point\n" +
            "- P0 will format it as a percentage, number is the amount of decimals to show")]
        [SerializeField]
        private String valueDisplayFormat = "0.0";

        protected override string LogPrefix => $"FloatTextDriver {name}";

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();
            
            //TODO: check if all fields are valid
            // or find the TMP component
        }

        public override void UpdateFloat(float value)
        {
            if(textMeshPro) {
                textMeshPro.text = value.ToString(valueDisplayFormat);
            }
        }

        [NonSerialized] private float cachedValue = float.NaN;
        [NonSerialized] private String prevFormat;
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        [ContextMenu("Update UI")]
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            UnityEditor.EditorUtility.SetDirty(this);

            if (valueDisplayFormat != prevFormat && !float.IsNaN(cachedValue))
            {
                UpdateFloat(cachedValue);
                if (textMeshPro)
                {
                    textMeshPro.MarkDirty();
                }
                prevFormat = valueDisplayFormat;
            }
        }

        public override void ApplyFloatValue(float value)
        {
            UpdateFloat(value);
            cachedValue = value;
            if (textMeshPro)
            {
                textMeshPro.MarkDirty();
            }
        }
#endif
    }
}