using System;
using nikkyai.common;
using nikkyai.control.kinetic;
using nikkyai.Kinetic_Controls;
using nikkyai.Utils;
using Texel;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using VRC;
using VRC.SDK3.Midi;
using VRC.SDKBase;
using VRC.Udon;
using Object = UnityEngine.Object;

namespace nikkyai.Editor
{
    [ExecuteAlways]
    [RequireComponent(typeof(Lever))]
    public class LeverEditorHelper : MonoBehaviour, IEditorOnly, IPreprocessCallbackBehaviour
    {
        public Lever lever;

        [Header("Settings")] [SerializeField]
        private Axis axis = Axis.Y;

        [Header("Settings - Default Value")] [SerializeField]
        public Vector2 outputRange = new Vector2(0, 1);

        [SerializeField] [Range(0, 1)] public float defaultValueNormalized = 0.25f;

        [SerializeField] public float defaultValue = 0;

        [Range(-180, 180)]
        [SerializeField]
        private float minRot = -45;

        [Range(-180, 180)]
        [SerializeField]
        private float maxRot = 45;
        
        [Header("Settings - Smoothing")]
        [Tooltip(
             "smoothes out value updates over time, may impact CPU frametimes AND cause more updates to FloatDrivers"),
         SerializeField]
        public bool enableValueSmoothing = true;

        [Tooltip("amount of frames to skip when approaching target value," +
                 "higher number == less load, but more choppy smoothing"),
         SerializeField, Range(1, 10)]
        public int smoothingUpdateInterval = 3;
        
        [Tooltip("higher values -> faster synchronization with the target maxSpeed \n" +
                 "(see Unity Mathf.SmoothDamp smoothTime parameter)")]
        [SerializeField]
        [Range(0f, 2.5f)]
        public float smoothingTime = 0.1f;

        [Tooltip("Maximum speed that smoothing can move at (see Unity Mathf.SmoothDamp maxSpeed parameter)")]
        [SerializeField]
        [Range(0f, 1f)]
        public float smoothingMaxSpeed = 0.25f;

        [Header("Network Syncing")]
        [SerializeField]
        [Tooltip(
            "whether network sync is enabled or not, this can be 'animated' at runtime to stage changes and apply them when syncing is enabled again")]
        public bool synced = true;

        [Header("VR Support")] [Tooltip("switches between finger contacts and pickup")]
        public bool useContactsInVR = true;

        [Header("Desktop Support")]
        [SerializeField]
        [Tooltip("this needs to be a thin, flat box collider, " +
                 "it will be used for getting where the center of the screen is facing in desktop mode, " +
                 "if it is too thick then facing it even from a slight angle will provide the wrong values, " +
                 "leading to the the controls 'jumping' upon pickup in desktop mode")]
        public Collider desktopRaycastCollider;

        [Header("MIDI")] //

        [SerializeField, Tooltip("Sets up the required VRC MIDI Listener")]
        public bool addMidiListenerComponent = false;

        [SerializeField, Tooltip("Requires a VRC MIDI Listened with CC enabled")]
        public bool midiEnabled = true;

        [SerializeField, Range(0, 15)] public int midiChannel = 0;
        [SerializeField, Range(0, 127)] public int midiNumber = 0;
        [SerializeField, Range(0, 127)] public int midiInputRangeStart = 0;
        [SerializeField, Range(0, 127)] public int midiInputRangeEnd = 127;

        [Header("Components - Handle")] [SerializeField]
        public Handle handle;

        [SerializeField]
        [Tooltip("should be the same as targetIndicator or a child, " +
                 "handle will be reset to the given transform position / rotation on release" +
                 " (proxied to handle)")]
        public Transform handleReset;

        [Header("Components - Indicators")]
        [SerializeField] //
        [Tooltip("move this on the configured axis to the minumum fader range")]
        public Transform minLimitIndicator;

        [SerializeField] //
        [Tooltip("move this on the configured axis to the maximum fader range")]
        public Transform maxLimitIndicator;

        [SerializeField] //
        [Tooltip("will be moved to follow the handle (target value)")]
        public Transform targetIndicator;

        [SerializeField] //
        [Tooltip("will be moved to follow the smoothed value")]
        public Transform valueIndicator;

        [Header("Drivers")]
        [SerializeField] //
        [Tooltip("object containing FloatDrivers, will update to the current target/preview value")]
        public GameObject floatTargetValueDrivers;


        [SerializeField] //
        [Tooltip("object containing FloatDrivers, will update to the current smoothed value")]
        public GameObject floatSmoothedValueDrivers;

        [SerializeField] //
        [Tooltip("object containing BoolDrivers, will update to the current auth status")]
        public GameObject boolAuthorizedDrivers;

        // [Header("Drivers - Readonly")] [ReadOnly]
        // public FloatDriver[] targetValueDrivers;
        //
        // [ReadOnly] public FloatDriver[] smoothedValueDrivers;
        // [ReadOnly] public BoolDriver[] authDrivers;

        [Header("Access Control")] //  
        [SerializeField]
        public bool enforceACL = true;

        [Tooltip("ACL used to check who can use the toggle")] //
        [SerializeField]
        public AccessControl accessControl;

        [Header("Debug")] //
        [SerializeField]
        [Tooltip("enabled and moves this transform to where the raytrace collision happens, desktop only")]
        internal Transform debugDesktopRaytrace;

        [SerializeField] public DebugLog debugLog;

        private bool initialized = false;
        private float outputRangeMin => outputRange.x;
        private float outputRangeMax => outputRange.y;

        private float prevDefaultNormalized, prevDefault, prevMin, prevMax;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        private Lever GetLever()
        {
            if (Utilities.IsValid(lever)) return lever;
            Debug.Log("getting fader component", this);
            lever = gameObject.GetComponent<Lever>();
            return lever;
        }

        [ContextMenu("ApplyValues")]
        private void ApplyValues()
        {
            ApplyValues(onValidate: false);
        }

        private void ApplyValues(bool onValidate = false)
        {
            if (!initialized)
            {
                Debug.LogWarning("not initalized yet");
                return;
            }

            Debug.Log("Applying values", this);

            var l = GetLever();
            if (!Utilities.IsValid(l))
            {
                Debug.LogError("fader was not assigned");
                return;
            }

            if (!Mathf.Approximately(prevDefaultNormalized, defaultValueNormalized))
            {
                defaultValue = Mathf.Lerp(outputRangeMin, outputRangeMax, defaultValueNormalized);
                prevDefault = defaultValue;
                prevMin = outputRangeMin;
                prevMax = outputRangeMax;
            }

            if (!Mathf.Approximately(prevDefault, defaultValue))
            {
                defaultValueNormalized = Mathf.InverseLerp(outputRangeMin, outputRangeMax, defaultValue);
                prevDefaultNormalized = defaultValueNormalized;
                prevMin = outputRangeMin;
                prevMax = outputRangeMax;
            }

            if (!Mathf.Approximately(prevMin, outputRangeMin) || !Mathf.Approximately(prevMax, outputRangeMax))
            {
                defaultValueNormalized = Mathf.InverseLerp(outputRangeMin, outputRangeMax, defaultValue);
                prevDefaultNormalized = defaultValueNormalized;
                prevMin = outputRangeMin;
                prevMax = outputRangeMax;
            }

            prevDefaultNormalized = defaultValueNormalized;
            prevDefault = defaultValue;

            l.outputRange = outputRange;
            l.defaultValueNormalized = defaultValueNormalized;
            l.defaultValue = defaultValue;
            l.minRot = minRot;
            l.maxRot = maxRot;

            l.enableValueSmoothing = enableValueSmoothing;
            l.smoothingUpdateInterval = smoothingUpdateInterval;
            l.smoothingTime = smoothingTime;
            l.smoothingMaxSpeed = smoothingMaxSpeed;

            l.synced = synced;

            l.desktopRaycastCollider = desktopRaycastCollider;

            SetupMidiListener(onValidate: onValidate);
            l.midiEnabled = midiEnabled;
            l.midiChannel = midiChannel;
            l.midiNumber = midiNumber;
            l.midiInputRangeStart = midiInputRangeStart;
            l.midiInputRangeEnd = midiInputRangeEnd;

            l.handle = handle;
            // f.SetupHandle();
            if (Utilities.IsValid(handle))
            {
                handle.resetTransform = handleReset;
                handle.UseContactsInVR = useContactsInVR;
                handle.EditorACL = accessControl;
                handle.EditorEnforceACL = enforceACL;
                handle.EditorDebugLog = debugLog;

                handle._EnsureInit();
                handle.Register(l);
                handle.SetupPickup();
                handle.SetupPickupRigidbody();
                handle.MarkDirty();
            }
            else
            {
                Debug.LogError($"missing handle in {name}", this);
            }

            l.minLimitIndicator = minLimitIndicator;
            l.maxLimitIndicator = maxLimitIndicator;
            l.targetIndicator = targetIndicator;
            l.valueIndicator = valueIndicator;

            l.floatTargetValueDrivers = floatTargetValueDrivers;
            l.floatSmoothedValueDrivers = floatSmoothedValueDrivers;
            l.EditorBoolAuthorizedDrivers = boolAuthorizedDrivers;

            l.EditorEnforceACL = enforceACL;
            l.EditorACL = accessControl;

            l.debugDesktopRaytrace = debugDesktopRaytrace;
            l.EditorDebugLog = debugLog;
            l.UpdateIndicatorsInEditor();

            var minValue = Mathf.Min(l.MinValue, l.MaxValue);
            var maxValue = Mathf.Max(l.MinValue, l.MaxValue);

            l.FindDrivers();
            // targetValueDrivers = f._targetValueFloatDrivers;
            // smoothedValueDrivers = f._smoothedValueFloatDrivers;
            l.FindBoolAuthDrivers();
            // authDrivers = f.IsAuthorizedBoolDrivers;

            foreach (var valueFloatDriver in l._smoothedValueFloatDrivers)
            {
                valueFloatDriver.ApplyFloatValue(
                    Math.Clamp(defaultValue, minValue, maxValue)
                );
            }

            foreach (var targetFloatDriver in l._targetValueFloatDrivers)
            {
                targetFloatDriver.ApplyFloatValue(
                    Math.Clamp(defaultValue, minValue, maxValue)
                );
            }

            l.MarkDirty();
        }

       
        protected void SetupMidiListener(bool onValidate = false)
        {
            if (addMidiListenerComponent)
            {
                var listener = GetComponent<VRCMidiListener>();
                if (listener == null)
                {
                    listener = gameObject.AddComponent<VRCMidiListener>();
                }

                if (listener != null)
                {
                    SerializedObject listenerSerialized = new SerializedObject(listener);
                    var behaviourProperty = listenerSerialized.FindProperty("behaviour");
                    if (Utilities.IsValid(lever))
                    {
                        behaviourProperty.objectReferenceValue = new SerializedObject(lever)
                            .FindProperty("_udonSharpBackingUdonBehaviour")
                            .objectReferenceValue;
                        listenerSerialized.ApplyModifiedProperties();
                    }

                }

                if (listener != null)
                {
                    listener.activeEvents = VRCMidiListener.MidiEvents.CC;
                    listener.enabled = true;
                }
            }
            else
            {
                var listeners = GetComponents<VRCMidiListener>();
                foreach (var vrcMidiListener in listeners)
                {
                    if (vrcMidiListener == null) continue;
                    // vrcMidiListener.enabled = false;
                    if (onValidate)
                    {
                        vrcMidiListener.enabled = false;
                        EditorApplication.delayCall += ()=>
                        {
                            Undo.DestroyObjectImmediate(vrcMidiListener);
                        };
                    }
                    else if (Application.isEditor)
                    {
                        DestroyImmediate(vrcMidiListener);
                    }
                    else
                    {
                        Destroy(vrcMidiListener);
                    }
                }
            }
        }

        void OnValidate()
        {
            if (!initialized)
            {
                CopyFromLever();
            }
            if (
                initialized && ValidationCache.ShouldRunValidation(
                    this,
                    HashCode.Combine(
                        HashCode.Combine(
                            outputRange,
                            defaultValueNormalized,
                            defaultValue,
                            minRot,
                            maxRot
                        ),
                        HashCode.Combine(
                            enableValueSmoothing,
                            smoothingUpdateInterval,
                            smoothingTime,
                            smoothingMaxSpeed
                        ),
                        HashCode.Combine(
                            synced,
                            addMidiListenerComponent,
                            midiEnabled,
                            midiChannel,
                            midiNumber,
                            midiInputRangeStart,
                            midiInputRangeEnd
                        ),
                        HashCode.Combine(
                            handle,
                            handleReset,
                            minLimitIndicator,
                            maxLimitIndicator,
                            targetIndicator,
                            valueIndicator),
                        HashCode.Combine(
                            floatTargetValueDrivers,
                            floatSmoothedValueDrivers,
                            boolAuthorizedDrivers,
                            enforceACL,
                            accessControl,
                            debugLog
                        )
                    )
                )
            )
            {
                ApplyValues(onValidate: true);
            }
        }

        [ContextMenu("Setup Values from Lever")]
        internal void CopyFromLever()
        {
            var l = GetLever();
            lever = l;
            outputRange = l.outputRange;
            defaultValueNormalized = l.defaultValueNormalized;
            defaultValue = l.defaultValue;
            minRot = l.minRot;
            maxRot = l.maxRot;

            enableValueSmoothing = l.enableValueSmoothing;
            smoothingUpdateInterval = l.smoothingUpdateInterval;
            smoothingTime = l.smoothingTime;
            smoothingMaxSpeed = l.smoothingMaxSpeed;

            synced = l.synced;
            useContactsInVR = l.handle.useContactsInVR;
            desktopRaycastCollider = l.desktopRaycastCollider;

            midiEnabled = l.midiEnabled;
            midiChannel = l.midiChannel;
            midiNumber = l.midiNumber;
            midiInputRangeStart = l.midiInputRangeStart;
            midiInputRangeEnd = l.midiInputRangeEnd;

            handle = l.handle;
            handleReset = l.handle.resetTransform;
            handleReset = l.handleResetDeprecated;

            minLimitIndicator = l.minLimitIndicator;
            maxLimitIndicator = l.maxLimitIndicator;
            targetIndicator = l.targetIndicator;
            valueIndicator = l.valueIndicator;

            floatTargetValueDrivers = l.floatTargetValueDrivers;
            floatSmoothedValueDrivers = l.floatSmoothedValueDrivers;
            boolAuthorizedDrivers = l.EditorBoolAuthorizedDrivers;

            enforceACL = l.EditorEnforceACL;
            accessControl = l.EditorACL;

            debugDesktopRaytrace = l.debugDesktopRaytrace;
            debugLog = l.EditorDebugLog;

            initialized = true;
            this.MarkDirty();
        }

        private void Awake()
        {
            CopyFromLever();
            ApplyValues(onValidate: true);
        }
#endif

        public bool OnPreprocess()
        {
            Debug.Log($"Preprocess: is editor: {Application.isEditor}");
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            ApplyValues();
#else
            Debug.LogWarning("Preprocess: is not running");
#endif
            return true;
        }

        public int PreprocessOrder { get; }
    }
}