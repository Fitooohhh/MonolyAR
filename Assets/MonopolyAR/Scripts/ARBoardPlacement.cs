using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace MonopolyAR
{
    // Runs before the existing game's Start so no turn can begin before placement.
    [DefaultExecutionOrder(-100)]
    public sealed class ARBoardPlacement : MonoBehaviour
    {
        public Transform boardRoot;
        public MonopolyTurnManager game;
        public Camera arCamera;
        public ARRaycastManager raycastManager;
        public ARPlaneManager planeManager;
        public ARAnchorManager anchorManager;
        public Transform indicator;
        public float boardWidthMeters = .6f;
        public float originalBoardWidth = 14f;
        [Tooltip("Frente legible del modelo en el espacio local de BoardRoot. El tablero actual usa +Z.")]
        public Vector3 modelFrontLocal = Vector3.forward;
        public bool BoardPlaced { get; private set; }
        public string PlacementMessage { get; private set; } = "Busca una superficie y toca para colocar el tablero";
        private Quaternion baseRotation;
        private Vector3 baseScale;
        private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        private void Awake()
        {
            if (!boardRoot || !game || !arCamera || !raycastManager || !planeManager || !anchorManager || !indicator)
            {
                if (game) game.enabled = false;
                PlacementMessage = "Faltan referencias AR.";
                Debug.LogError(PlacementMessage, this);
                enabled = false;
                return;
            }
            baseRotation = boardRoot.rotation;
            baseScale = boardRoot.localScale;
            game.enabled = false;
            boardRoot.gameObject.SetActive(false);
            indicator.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (BoardPlaced) return;
            if (ARSession.state == ARSessionState.Unsupported)
            {
                PlacementMessage = "Este dispositivo no admite ARCore.";
                indicator.gameObject.SetActive(false);
                return;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                PlacementMessage = "Permite el acceso a la cámara para usar AR.";
                indicator.gameObject.SetActive(false);
                return;
            }
#endif
            if (ARSession.state != ARSessionState.SessionTracking)
            {
                PlacementMessage = "Busca una superficie y toca para colocar el tablero";
                indicator.gameObject.SetActive(false);
                return;
            }
            Vector2 point = new Vector2(Screen.width * .5f, Screen.height * .5f);
            bool pressed = false;
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                point = touch.position;
                pressed = touch.phase == TouchPhase.Began;
            }
#if UNITY_EDITOR
            else if (Input.GetMouseButton(0))
            {
                point = Input.mousePosition;
                pressed = Input.GetMouseButtonDown(0);
            }
#endif
            if (IsOverUI(point) || !FindSurface(point, out ARRaycastHit hit, out ARPlane plane))
            {
                indicator.gameObject.SetActive(false);
                return;
            }
            indicator.SetPositionAndRotation(hit.pose.position + (hit.pose.rotation * Vector3.up) * .002f, hit.pose.rotation);
            indicator.gameObject.SetActive(true);
            if (!pressed) return;
            // Keep a real AR anchor at scale 1; scale the existing board beneath it.
            var anchor = anchorManager.AttachAnchor(plane, hit.pose);
            if (!anchor)
            {
                PlacementMessage = "No se pudo fijar el tablero. Toca la superficie otra vez.";
                return;
            }
            PlaceBoard(hit.pose, anchor.transform);
        }

        private bool IsOverUI(Vector2 point)
        {
            if (!EventSystem.current) return false;
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, uiHits);
            return uiHits.Count > 0;
        }

        private bool FindSurface(Vector2 point, out ARRaycastHit hit, out ARPlane plane)
        {
            hit = default;
            plane = null;
            if (!raycastManager.Raycast(point, hits, TrackableType.PlaneWithinPolygon)) return false;
            foreach (var candidate in hits)
            {
                var p = planeManager.GetPlane(candidate.trackableId);
                if (!p || p.alignment != PlaneAlignment.HorizontalUp || p.trackingState != TrackingState.Tracking) continue;
                hit = candidate;
                plane = p;
                return true;
            }
            return false;
        }

        private bool PlaceBoard(Pose pose, Transform anchor)
        {
            if (BoardPlaced) return false;
            boardRoot.SetParent(anchor, true);
            boardRoot.SetPositionAndRotation(pose.position, CalculatePlacementRotation(pose));
            boardRoot.localScale = baseScale * (boardWidthMeters / originalBoardWidth);
            boardRoot.gameObject.SetActive(true);
            BoardPlaced = true;
            PlacementMessage = "";
            indicator.gameObject.SetActive(false);
            // Plane tracking and the anchor remain alive; no plane visualizer is created.
            planeManager.requestedDetectionMode = PlaneDetectionMode.None;
            game.enabled = true;
            return true;
        }

        /// <summary>Calcula una sola orientación horizontal hacia la posición inicial de la cámara.</summary>
        public Quaternion CalculatePlacementRotation(Pose pose)
        {
            Vector3 surfaceUp = pose.rotation * Vector3.up;
            Vector3 towardCamera = Vector3.ProjectOnPlane(arCamera.transform.position - pose.position, surfaceUp);
            if (towardCamera.sqrMagnitude < 0.0001f)
                return pose.rotation * baseRotation;
            towardCamera.Normalize();
            Quaternion desiredBasis = Quaternion.LookRotation(towardCamera, surfaceUp);
            Vector3 front = modelFrontLocal.sqrMagnitude < 0.0001f ? Vector3.forward : modelFrontLocal.normalized;
            Quaternion modelBasis = Quaternion.LookRotation(front, Vector3.up);
            return desiredBasis * Quaternion.Inverse(modelBasis) * baseRotation;
        }
    }
}

