using System;
using nikkyai.common;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai.Editor
{
    [ExecuteAlways]
    [RequireComponent(typeof(BaseBehaviour))]
    public class PreProcessEditorHelper: MonoBehaviour, IEditorOnly, IPreprocessCallbackBehaviour {
        public bool OnPreprocess()
        {
            Debug.Log($"Starting Preprocess on {name} (is editor: {Application.isEditor})", this);
            var behaviours = GetComponents<BaseBehaviour>();
            foreach (var behaviour in behaviours)
            {
                behaviour.OnPreprocess();
            }
            return true;
        }

        [ContextMenu("Preprocess")]
        public void TriggerManually()
        {
            OnPreprocess();
        }

        public void Awake()
        {
            OnPreprocess();
        }

        public int PreprocessOrder { get; }
    }
}