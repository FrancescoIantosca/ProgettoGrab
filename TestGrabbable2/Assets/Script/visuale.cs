using UnityEngine;

public class visuale : MonoBehaviour
{
    public float mouseSensitivity = 1250f;
    public Transform playerBody;

    private float xRotation = 0f;

    void Start()
    {
        // Blocca il cursore al centro dello schermo e lo rende invisibile
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (playerBody.CompareTag("Player"))
        {
            // Ottiene il movimento del mouse
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            // Applica il movimento in verticale
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -60f, 60f); // Limita la rotazione verticale

            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Ruota il corpo del giocatore in base al movimento orizzontale del mouse
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}
