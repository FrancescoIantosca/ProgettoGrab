using UnityEngine;

public class VisualeMobile : MonoBehaviour
{
    public Transform playerBody;
    private float xRotation = 0f;

    void Start()
    {
        // Abilita il giroscopio
        Input.gyro.enabled = true;

        // Blocca il cursore solo se stai testando in editor
#if UNITY_EDITOR
        Cursor.lockState = CursorLockMode.Locked;
#endif
    }

    void Update()
    {
        if (!SystemInfo.supportsGyroscope)
        {
            Debug.LogWarning("Giroscopio non supportato su questo dispositivo.");
            return;
        }

        // Ottieni la rotazione dal giroscopio
        Quaternion deviceRotation = Input.gyro.attitude;

        // Converte la rotazione dal sistema di coordinate del telefono a quello di Unity
        Quaternion convertedRotation = new Quaternion(-deviceRotation.x, -deviceRotation.y, deviceRotation.z, deviceRotation.w);

        // Applica la rotazione al giocatore
        transform.localRotation = Quaternion.Euler(convertedRotation.eulerAngles.x, 0f, 0f);
        playerBody.rotation = Quaternion.Euler(0f, convertedRotation.eulerAngles.y, 0f);
    }
}
