using UnityEngine;

namespace Infinicraft.Player
{
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField]
        private float sensitivity = 300f;

        [SerializeField]
        private Transform playerBody;

        [SerializeField]
        private PlayerInput playerInput;

        [SerializeField]
        private GameObject playerObject;

        float verticalRotation = 0f;


        private void Awake()
        {
            playerInput = GetComponentInParent<PlayerInput>();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            playerObject.GetComponent<Renderer>().enabled = false;
        }

        // Update is called once per frame
        void Update()
        {
            float mouseX = playerInput.MousePosition.x * sensitivity;
            float mouseY = playerInput.MousePosition.y * sensitivity;

            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -90, 90);

            transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}
