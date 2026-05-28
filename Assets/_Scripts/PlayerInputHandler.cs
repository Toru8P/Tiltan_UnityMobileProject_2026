using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts
{
    public class PlayerInputHandler : MonoBehaviour
    {
        private PlayerInput _playerInput;
        private PlayerMovementController _movementController;

        // Awake runs before Start. We grab references to the PlayerInput (new Input System)
        // and the movement controller that lives on the same GameObject.
        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _movementController = GetComponent<PlayerMovementController>();
        }

        // Subscribe to input events when this component becomes active.
        // OnEnable is called every time the object is enabled (not just at start), making it the safe place to hook events.
        private void OnEnable()
        {
            _playerInput.onActionTriggered += HandleAction;
        }

        // Unsubscribe when disabled — prevents memory leaks and double-firing if the object is re-enabled later.
        private void OnDisable()
        {
            _playerInput.onActionTriggered -= HandleAction;
        }

        // One callback for every input action. We branch based on the action name and forward
        // the input to the movement controller. "performed" means the button was fully pressed (not just touched).
        private void HandleAction(InputAction.CallbackContext context)
        {
            string actionName = context.action.name;

            if (actionName == "Move")
            {
                _movementController.SetMoveInput(context.ReadValue<Vector2>());
            }
            else if (actionName == "Roll")
            {
                if (context.performed)
                {
                    _movementController.PerformRoll();
                }
            }
            else if (actionName == "Attack")
            {
                if (context.performed)
                {
                    _movementController.PerformAttack();
                }
            }
        }
    }
}
