using System;
using nikkyai.common;
using nikkyai.control.headless;
using nikkyai.extensions;
using UnityEngine;

namespace nikkyai.driver.control.headless
{
    public class FloatProxyDriver: FloatDriver
    {
        [SerializeField] private FloatProxy[] proxies = {};

        public FloatProxy[] Proxies
        {
            get => proxies;
            set
            {
                proxies = value;
                OnUpdateFloat(lastValue);
            }
        }
        public FloatProxy Proxy
        {
            get => proxies[0];
            set
            {
                proxies = new FloatProxy[1];
                proxies[0] = value;
                OnUpdateFloat(lastValue);
            }
        }

        private float lastValue = 0f;
        
        protected override string LogPrefix => nameof(FloatProxyDriver);
        
        void Start()
        {
            _EnsureInit();
        }

        protected override void OnUpdateFloat(float value)
        {
            if (!enabled) return;
            lastValue = value;

            for (var index = 0; index < proxies.Length; index++)
            {
                var proxyFloat = proxies[index];
                proxyFloat.UpdateFloat(value);
            }
        }
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        protected override void EditorUpdateFloatValue(float value)
        {
            base.EditorUpdateFloatValue(value);
            Log($"applying new value: {value}");
            
            foreach (var proxyFloat in proxies)
            {
                proxyFloat.EditorUpdateFloatRescale(value);
            }
        }
#endif
    }
}