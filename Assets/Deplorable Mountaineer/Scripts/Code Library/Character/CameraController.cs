using Cinemachine;
using Deplorable_Mountaineer.UI;
using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library.Character {
    /// <summary>
    /// Manage camera boom and first/third person view 
    /// </summary>
    public class CameraController : MonoBehaviour {
        [Tooltip("Culling mask for first person camera")] [SerializeField]
        private LayerMask firstPersonCullingMask;

        [Tooltip("Culling mask for third person camera")] [SerializeField]
        private LayerMask thirdPersonCullingMask;

        [Tooltip("Camera, with CinemachineBrain component attached")] [SerializeField]
        private Camera mainCamera;

        [Tooltip("CinemachineBrain component attached to main camera")] [SerializeField]
        private CinemachineBrain mainCameraBrain;

        [Tooltip("First person virtual camera connected to CinemachineBrain")] [SerializeField]
        private CinemachineVirtualCamera firstPersonVirtualCamera;

        [Tooltip("Third person virtual camera connected to CinemachineBrain")] [SerializeField]
        private CinemachineVirtualCamera thirdPersonVirtualCamera;

        [Tooltip("Third person camera arm length at start")] [SerializeField]
        private float defaultCameraDistance = 3;

        [Tooltip("Check this to try to guess Main Camera and Main Camera Brain")]
        [SerializeField]
        private bool autoSetMainCamera;

        [Tooltip("Normal, unzoomed FOV")] [SerializeField]
        private float defaultFOV = 60;

        [Tooltip("Divide FOV by this when zoomed in fully")] [SerializeField]
        private float zoomMultiplierTele = 3;

        [Tooltip("Divide FOV by this when zoomed out fully")] [SerializeField]
        private float zoomMultiplierWide = 2f/3f;

        [Tooltip("Hud crosshairs")] [SerializeField]
        private Crosshairs crosshairs;


        private float _zoomLevel;

        /// <summary>
        /// True when in first-person view mode
        /// </summary>
        public bool IsFirstPerson {
            get => firstPersonVirtualCamera.Priority > thirdPersonVirtualCamera.Priority;
            set {
                if(value) SwitchToFirstPerson();
                else SwitchToThirdPerson();
            }
        }

        /// <summary>
        /// Length of camera arm in third-person view mode
        /// </summary>
        public float CameraArmLength {
            get =>
                ((Cinemachine3rdPersonFollow) thirdPersonVirtualCamera
                    .GetCinemachineComponent(CinemachineCore.Stage.Body)).CameraDistance;
            set => ((Cinemachine3rdPersonFollow) thirdPersonVirtualCamera
                .GetCinemachineComponent(CinemachineCore.Stage.Body)).CameraDistance = value;
        }

        private void Reset(){
            AutoSetMainCamera(true);
        }

        private void OnValidate(){
            AutoSetMainCamera(autoSetMainCamera);
            autoSetMainCamera = false;
        }

        private void Start(){
            CameraArmLength = defaultCameraDistance;
        }

        private void OnEnable(){
            //ensure in a valid state
            if(IsFirstPerson) SwitchToFirstPerson();
            else SwitchToThirdPerson();
        }

        public void NextZoomLevel(){
            _zoomLevel = (_zoomLevel + 1)%3;
            if(!mainCameraBrain) return;
            float fov = _zoomLevel switch {
                0 => defaultFOV,
                1 => defaultFOV/zoomMultiplierTele,
                2 => defaultFOV/zoomMultiplierWide,
                _ => mainCamera.fieldOfView
            };
            thirdPersonVirtualCamera.m_Lens.FieldOfView = fov;
            firstPersonVirtualCamera.m_Lens.FieldOfView = fov;
            if(crosshairs){
                crosshairs.ZoomMultiplier = _zoomLevel switch {
                    0 => 1,
                    1 => zoomMultiplierTele,
                    2 => zoomMultiplierWide,
                    _ => crosshairs.ZoomMultiplier
                };
            }
        }

        private void SwitchToFirstPerson(){
            firstPersonVirtualCamera.Priority = 20;
            thirdPersonVirtualCamera.Priority = 10;
            mainCamera.cullingMask = firstPersonCullingMask;
        }

        private void SwitchToThirdPerson(){
            firstPersonVirtualCamera.Priority = 10;
            thirdPersonVirtualCamera.Priority = 20;
            mainCamera.cullingMask = thirdPersonCullingMask;
        }

        private void AutoSetMainCamera(bool force = false){
            if(force){
                mainCamera = null;
                mainCameraBrain = null;
            }

            if(!mainCamera && !mainCameraBrain) mainCamera = Camera.main;

            if(mainCamera && !mainCameraBrain){
                mainCameraBrain = mainCamera.GetComponent<CinemachineBrain>();
                if(mainCameraBrain) return;
            }

            if(mainCameraBrain && !mainCamera){
                mainCamera = mainCameraBrain.GetComponent<Camera>();
                if(mainCamera) return;
            }

            mainCamera = FindObjectOfType<Camera>();
            if(mainCamera && !mainCameraBrain){
                mainCameraBrain = mainCamera.GetComponent<CinemachineBrain>();
                if(mainCameraBrain) return;
            }

            mainCameraBrain = FindObjectOfType<CinemachineBrain>();
            if(mainCamera && force) defaultFOV = mainCamera.fieldOfView;
            else if(mainCamera) mainCamera.fieldOfView = defaultFOV;
        }
    }
}