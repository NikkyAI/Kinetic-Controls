using nikkyai.common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace nikkyai
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RaytraceTest : LoggingSimple
    {
        [SerializeField] private Transform hitTracker;
        [SerializeField, Min(0)] private float maxRange = 5;

        private VRCPlayerApi _localPlayer;

        protected override string LogPrefix => nameof(RaytraceTest);

        void Start()
        {
            _EnsureInit();
        }

        protected override void _Init()
        {
            base._Init();

            _localPlayer = Networking.LocalPlayer;
        }

        private void Update()
        {
            // Log($"TrackingData: {trackingData.position} {trackingData.rotation}");
            // var localPos = transform.InverseTransformPoint(trackingData.position);
            // var LocalRot = transform.InverseTransformVector(trackingData.position));

            var trackingData = _localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var inPoint = transform.position;
            // var planeDirection = trackingData.rotation * Vector3.back;
            var planeDirection = trackingData.position - inPoint;
            // direction[(int)axis] = 0;
            planeDirection.y = 0;
            var plane = new Plane(
                Vector3.Normalize(planeDirection),
                inPoint
            );
            var lookDirection = trackingData.rotation * Vector3.forward;
            var ray = new Ray(
                trackingData.position,
                lookDirection
            );
            
            //TODO: setup layer mask
            var wasHit = Physics.Raycast(
                ray: ray, 
                hitInfo: out RaycastHit hit,
                maxDistance: maxRange,
                Physics.DefaultRaycastLayers,
                queryTriggerInteraction: QueryTriggerInteraction.Ignore
                );
            // var wasHit = plane.Raycast(ray, out var distance);
            // if (!wasHit)
            // {
            //     distance = maxRange;
            // }
            // var lookDirectionAxisLocked = lookDirection;
            // lookDirectionAxisLocked.y = 0;
            // var angle = Vector3.Angle(lookDirectionAxisLocked, -planeDirection);
            if (wasHit)
            {
                // Log($"angle: {angle}");
                // var hitPosition = ray.GetPoint(distance);
                
                Log($"raycast hit, distance: {hit.distance}, point: {hit.point}");
                hitTracker.transform.position = hit.point;
                // hitTracker.gameObject.SetActive(true);
                // hitTracker.position = hitPosition;
            }
            else
            {
                hitTracker.transform.position = ray.GetPoint(maxRange);
                // hitTracker.gameObject.SetActive(false);
            }
        }
    }
}