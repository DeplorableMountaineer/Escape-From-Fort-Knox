using System;
using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library.Character {
    /// <summary>
    /// Decouple actions from controls;  This way, if we decide to switch to the
    /// new input system, it will be easier.
    ///
    /// Currently uses the old input manager.
    /// </summary>
    public class InputController : MonoBehaviour {
        [Tooltip("Maps input manager action strings to control intention")] [SerializeField]
        private Actions actions;

        [Tooltip("If true, pulling back on mouse raises aim, like aircraft controls;" +
                 "If false, pushing mouse raises aim, associating forward with up, like" +
                 "the Unity editor scene window navigation")]
        [SerializeField]
        private bool invertMouseY = false;

        /// <summary>
        /// The Movement Controller component that this should control.
        /// </summary>
        [SerializeField] private MovementControllerBase movementController;

        private CursorLockMode _saveLockState;
        private bool _saveCursorVisibility;

        private void OnEnable(){
            _saveLockState = Cursor.lockState;
            _saveCursorVisibility = Cursor.visible;
            if(actions.hideMouseCursor){
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable(){
            Cursor.lockState = _saveLockState;
            Cursor.visible = _saveCursorVisibility;
        }

        /// <summary>
        /// Axis controls use physics update
        /// </summary>
        private void FixedUpdate(){
            if(!movementController) return;
            float multiplier = invertMouseY ? -1 : 1;
            movementController.Turn(
                Input.GetAxis(actions.turn) + Input.GetAxis(actions.mouseX));
            movementController.Aim(
                -Input.GetAxis(actions.aim) + multiplier*Input.GetAxis(actions.mouseY));
            movementController.Move(new Vector3(Input.GetAxis(actions.strafe), 0,
                Input.GetAxis(actions.move)));
        }

        /// <summary>
        /// Button controls need normal update, or some presses/releases will be missed.
        /// </summary>
        private void Update(){
            if(Input.GetButtonDown(actions.jump)) movementController.Jump();
            if(Input.GetButtonDown(actions.crouch)) movementController.WantsToCrouch = true;
            if(Input.GetButtonUp(actions.crouch)) movementController.WantsToCrouch = false;
            if(Input.GetButtonDown(actions.run))
                movementController.IsRunning = !movementController.IsRunning;
        }

        [Serializable]
        private class Actions {
            [SerializeField] public bool hideMouseCursor = true;
            [SerializeField] public string mouseX = "Mouse X";
            [SerializeField] public string mouseY = "Mouse Y";
            [SerializeField] public string scrollwheel = "Mouse Scrollwheel";
            [SerializeField] public string move = "Move";
            [SerializeField] public string strafe = "Strafe";
            [SerializeField] public string turn = "Turn";
            [SerializeField] public string aim = "Aim";
            [SerializeField] public string jump = "Jump";
            [SerializeField] public string crouch = "Crouch";
            [SerializeField] public string run = "Toggle Run";
            [SerializeField] public string operate = "Operate";
        }
    }
}