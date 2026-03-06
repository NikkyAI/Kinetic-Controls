using System;
using TMPro;
using UnityEngine;
using VRC;

namespace nikkyai.driver.text
{
    public class IntTextDriver : IntDriver
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
        private String valueDisplayFormat = "00";

        protected override string LogPrefix => $"IntTextDriver {name}";

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

        public override void UpdateInt(int value)
        {
            if(textMeshPro) {
                textMeshPro.text = value.ToString(valueDisplayFormat);
            }
        }

        [NonSerialized] private int cachedValue;
        [NonSerialized] private String prevFormat;
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP

        [ContextMenu("Update UI")]
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            UnityEditor.EditorUtility.SetDirty(this);

            if (valueDisplayFormat != prevFormat && !float.IsNaN(cachedValue))
            {
                UpdateInt(cachedValue);
                if (textMeshPro)
                {
                    textMeshPro.MarkDirty();
                }
                prevFormat = valueDisplayFormat;
            }
        }

        public override void ApplyIntValue(int value)
        {
            UpdateInt(value);
            cachedValue = value;
            if (textMeshPro)
            {
                textMeshPro.MarkDirty();
            }
        }
#endif
    }
}