using System;
using nikkyai.common;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace nikkyai.driver.postprocessing
{
    public class BoolSwitchPostprocessVolume : BoolDriver
    {
        [SerializeField] //
        private PostProcessVolume volume;
        [SerializeField, Range(0,10)] //
        private float timeToSwitch = 1.5f;
    
        void Start()
        {
            _EnsureInit();
        }

        protected override string LogPrefix =>  nameof(BoolSwitchPostprocessVolume);

        public override void OnUpdateBool(bool value)
        {
            volume.enabled = true;
            _startWeight = volume.weight;
            // _currentWeight = volume.weight;
            _startTime = Time.time;
            _targetWeight = value ? 1 : 0;
            // if (_currentWeight > 0.5f)
            // {
            //     _targetWeight = 0f;
            // }
            // else
            // {
            //     _targetWeight = 1f;
            // }
            SendCustomEventDelayedFrames(nameof(OnUpdateFrame), 1);
        }

        private float _startTime = 0f;
        private float _startWeight = 0f;
        // private float _currentWeight = 0f;
        private float _targetWeight = 0f;
    
        public void OnUpdateFrame()
        {
            float currentTIme = Time.time;
            float deltaTime = currentTIme - _startTime;
            
            var normalized = Mathf.InverseLerp(0, timeToSwitch, deltaTime);

            volume.weight = Mathf.Lerp(_startWeight, _targetWeight, normalized);
        
            if (deltaTime < timeToSwitch)
            {
                SendCustomEventDelayedFrames(nameof(OnUpdateFrame), 1);
            }
            else
            {
                SendCustomEventDelayedFrames(nameof(OnTargetReached), 1);
            }
        
        }
    
        public void OnTargetReached()
        {
            Log("On Target Reached");
            if (Mathf.Approximately(volume.weight, 0f))
            {
                volume.enabled = false;
            }
        
        }
    }
}
