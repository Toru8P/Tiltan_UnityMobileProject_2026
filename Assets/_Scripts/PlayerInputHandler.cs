using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts
{
    public class PlayerInputHandler : MonoBehaviour
    {
        private PlayerInput _playerInput;
        private PlayerMovementController _movementController;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _movementController = GetComponent<PlayerMovementController>();
        }

        private void OnEnable()
        {
            _playerInput.onActionTriggered += HandleAction;
        }

        private void OnDisable()
        {
            _playerInput.onActionTriggered -= HandleAction;
        }

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
