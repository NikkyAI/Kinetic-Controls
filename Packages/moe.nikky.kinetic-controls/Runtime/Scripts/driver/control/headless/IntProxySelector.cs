using System;
using nikkyai.common;
using nikkyai.control.headless;
using UnityEngine;

namespace nikkyai.driver.control.headless
{
    public class IntProxySelector : IntDriver
    {
        [SerializeField] private FloatProxyDriver proxyDriver;
        [SerializeField] private FloatProxy[] proxies = { };

        void Start()
        {
            _EnsureInit();
        }

        protected override string LogPrefix => nameof(IntProxySelector);
        protected override void OnUpdateInt(int value)
        {
            Log($"selecting proxy index: {value}");
            if (value >= 0 && value < proxies.Length)
            {
                Log($"selecting proxy: {proxies[value]}");
                proxyDriver.Proxy = proxies[value];
            }
            else
            {
                LogError($"index out of bounds of the array: {value} for proxies of length {proxies.Length}");
            }
        }
    }
}