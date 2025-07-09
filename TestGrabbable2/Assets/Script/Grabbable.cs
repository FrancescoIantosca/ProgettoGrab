using UnityEngine;

public class Grabbable : MonoBehaviour
{
    public float grabDistance = 3.5f; // Distanza di grab
    public float smoothSpeed = 10.0f; // Fluidità di movimento
    private float rotationSpeed = 40.0f; // Velocità di rotazione

    public Camera mainCamera; // La camera del player

    private GameObject grabbedObject; // L'oggetto che viene grabbato
    private Vector3 grabOffset; // L'OffSet del suddetto oggetto

    private bool isStretching = false; // Variabile di controllo per lo stretch
    private Vector3 initialMousePosWorld; // Posizione iniziale del mouse
    private Vector3 initialScale; // Dimensioni iniziali dell'oggetto
    private Vector3 initialCornerWorld;
    private Vector3 stretchAxis;

    private bool isRotating = false; // Variabile di controllo per la rotazione
    private string rotationTag = ""; // Variabile di controllo per i tag di rotazione
    private Vector3 lastMousePosition; // Ultima posizione del mouse

    private Renderer activeCornerRenderer; // Variabile per i colori degli spigoli
    private Renderer activeRotationRenderer; // Variabile per i colori dei cubi per la rotazione
    private Renderer hoveredRenderer; // Renderer attualmente sotto il mouse
    private Renderer grabbedObjectRenderer; // Renderer dell'oggetto grabbato

    private Color originalCornerColor = new Color32(128, 128, 128, 255); // Grigio (#808080) Colore originale degli spigoli
    private Color originalRotationColor = new Color32(128, 128, 128, 255); // Grigio (#808080) Colore originale dei cubi rotazione e maniglia grab
    private Color originalObjectColor = new Color32(128, 128, 128, 255); // Grigio (#808080) Colore originale degli oggetti grabbabili

    private Color highlightColor = new Color32(173, 216, 230, 255); // LightBlue (#ADD8E6) Colore di evidenziamento
    private Color hoverColor = new Color32(0, 0, 139, 255); // DarkBlue (#00008B) Colore al passaggio del mouse

    void Update()
    {
        HandleHoverHighlight(); // Gestione evidenziazione in hover

        if (Input.GetMouseButtonDown(0)) // Gestione del grab dell'oggetto
        {
            TryGrabOrStretchObject();
        }

        if (Input.GetMouseButtonUp(0)) // Gestione del rilascio dell'oggetto
        {
            ReleaseObject();
        }

        if (grabbedObject != null)
        {
            if (isStretching) // Gestione dello stretch
            {
                HandleStretching();
            }
            else
            {
                if (isRotating) // Gestione della rotazione
                {
                    DragRotateObject();
                }
                else
                {
                    MoveObject();
                }
            }
        }
    }

    // Evidenziazione dinamica del cubo sotto al cursore
    void HandleHoverHighlight()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Se c'era un oggetto evidenziato prima (solo se non è l'attuale selezionato)
        if (hoveredRenderer != null &&
            hoveredRenderer != activeCornerRenderer &&
            hoveredRenderer != activeRotationRenderer)
        {
            hoveredRenderer.material.color = originalRotationColor;
            hoveredRenderer = null;
        }

        // Se NON stiamo interagendo (né stretch né rotazione), ripristina anche eventuali altri evidenziamenti
        if (grabbedObject == null)
        {
            if (activeCornerRenderer != null)
            {
                activeCornerRenderer.material.color = originalCornerColor;
                activeCornerRenderer = null;
            }

            if (activeRotationRenderer != null)
            {
                activeRotationRenderer.material.color = originalRotationColor;
                activeRotationRenderer = null;
            }
        }

        if (Physics.Raycast(ray, out hit, grabDistance))
        {
            GameObject hitObj = hit.collider.gameObject;
            string tag = hitObj.tag;

            // Solo i cubi di controllo (no oggetto grabbato)
            if (tag == "Stretch" || tag == "RotationVertical" || tag == "RotationOrizontal" || tag == "ManigliaGrab")
            {
                Renderer rend = hit.collider.GetComponent<Renderer>();
                if (rend != null && rend != activeCornerRenderer && rend != activeRotationRenderer)
                {
                    hoveredRenderer = rend;
                    originalRotationColor = rend.material.color;
                    rend.material.color = hoverColor;
                }
            }

            // Evidenziazione dei figli dello Stretch (es. spigoli)
            if (hitObj.transform.parent != null && hitObj.transform.parent.CompareTag("Stretch"))
            {
                // Esclude il figlio con tag "Oggetto"
                if (!hitObj.CompareTag("Oggetto"))
                {
                    Renderer rend = hitObj.GetComponent<Renderer>();
                    if (rend != null && rend != activeCornerRenderer && rend != activeRotationRenderer)
                    {
                        hoveredRenderer = rend;
                        originalCornerColor = rend.material.color;
                        rend.material.color = hoverColor;
                    }
                }
            }

            // Rende visibili tutti i figli dell'oggetto Stretch colpito
            if (tag == "Stretch" || hitObj.transform.parent?.CompareTag("Stretch") == true)
            {
                GameObject stretchRoot = hitObj.CompareTag("Stretch") ? hitObj : hitObj.transform.parent.gameObject;
                SetStretchVisibility(stretchRoot, true);
            }
        }
        else
        {
            // Nessun oggetto sotto il mouse, nasconde tutti i controlli Stretch
            GameObject[] stretchObjects = GameObject.FindGameObjectsWithTag("Stretch");
            foreach (GameObject stretch in stretchObjects)
            {
                if (grabbedObject != null && IsChildOf(grabbedObject.transform, stretch.transform))
                {
                    SetStretchVisibility(stretch, true);
                }
                else
                {
                    SetStretchVisibility(stretch, false);
                }
            }
        }
    }

    // Sistema di grab dell'oggetto
    void TryGrabOrStretchObject()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabDistance))
        {
            GameObject hitObject = hit.collider.gameObject;

            // Verifica che stiamo grabbando per stretchare
            if (IsCorner(hitObject, out stretchAxis, out initialCornerWorld))
            {
                grabbedObject = hitObject.transform.parent.gameObject; // Il padre è l'oggetto "Stretch"
                initialScale = grabbedObject.transform.localScale;

                Plane plane = new Plane(-mainCamera.transform.forward, initialCornerWorld);
                if (plane.Raycast(ray, out float enter))
                {
                    initialMousePosWorld = ray.GetPoint(enter);
                }

                isStretching = true;

                // Evidenzia corner attivo
                activeCornerRenderer = hitObject.GetComponent<Renderer>();
                if (activeCornerRenderer != null)
                {
                    originalCornerColor = activeCornerRenderer.material.color;
                    activeCornerRenderer.material.color = highlightColor;
                }
            }
            // Verifica che stiamo grabbando per ruotare
            else if (hitObject.CompareTag("RotationVertical") || hitObject.CompareTag("RotationOrizontal"))
            {
                grabbedObject = hitObject.transform.parent.gameObject;
                isRotating = true;
                rotationTag = hitObject.tag;
                lastMousePosition = Input.mousePosition;

                activeRotationRenderer = hitObject.GetComponent<Renderer>();
                if (activeRotationRenderer != null)
                {
                    originalRotationColor = activeRotationRenderer.material.color;
                    activeRotationRenderer.material.color = highlightColor;
                }
            }
            // Verifica che stiamo grabbando la maniglia
            else if (hitObject.CompareTag("ManigliaGrab") && hitObject.transform.parent.CompareTag("Stretch"))
            {
                grabbedObject = hitObject.transform.parent.gameObject;
                grabOffset = grabbedObject.transform.position - hit.point;

                Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = false;
                }

                isStretching = false;
                isRotating = false;

                activeRotationRenderer = hitObject.GetComponent<Renderer>();
                if (activeRotationRenderer != null)
                {
                    originalRotationColor = activeRotationRenderer.material.color;
                    activeRotationRenderer.material.color = highlightColor;
                }
            }
            // Grab di un oggetto NON Stretch
            else if (!hitObject.CompareTag("Stretch"))
            {
                Rigidbody rb = hitObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    grabbedObject = hitObject;
                    grabOffset = grabbedObject.transform.position - hit.point;
                    rb.useGravity = false;
                    isStretching = false;
                    isRotating = false;

                    grabbedObjectRenderer = grabbedObject.GetComponent<Renderer>();
                    if (grabbedObjectRenderer != null)
                    {
                        originalObjectColor = grabbedObjectRenderer.material.color;
                        grabbedObjectRenderer.material.color = highlightColor;
                    }
                }
            }
        }
    }

    // Funzione per quando rilasciamo l'oggetto
    void ReleaseObject()
    {
        if (grabbedObject != null)
        {
            if (!isStretching && !isRotating)
            {
                Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
                if (rb != null && grabbedObject.tag != "Stretch")
                {
                    rb.useGravity = true;
                }

                if (grabbedObjectRenderer != null)
                {
                    grabbedObjectRenderer.material.color = originalObjectColor;
                    grabbedObjectRenderer = null;
                }
            }

            grabbedObject = null;
            isStretching = false;
            isRotating = false;
            rotationTag = "";

            if (activeCornerRenderer != null)
            {
                activeCornerRenderer.material.color = originalCornerColor;
                activeCornerRenderer = null;
            }

            if (activeRotationRenderer != null)
            {
                activeRotationRenderer.material.color = originalRotationColor;
                activeRotationRenderer = null;
            }

            if (hoveredRenderer != null)
            {
                hoveredRenderer.material.color = originalRotationColor;
                hoveredRenderer = null;
            }
        }
    }

    // Per muovere l'oggetto
    void MoveObject()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint = ray.GetPoint(grabDistance) + grabOffset;
        grabbedObject.transform.position = Vector3.Lerp(grabbedObject.transform.position, targetPoint, smoothSpeed * Time.deltaTime);
    }

    // Il grab per la rotazione
    void DragRotateObject()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (rotationTag == "RotationVertical")
        {
            grabbedObject.transform.Rotate(Vector3.right, -mouseY * rotationSpeed, Space.World);
        }
        else if (rotationTag == "RotationOrizontal")
        {
            grabbedObject.transform.Rotate(Vector3.up, mouseX * rotationSpeed, Space.World);
        }
    }

    // Il grab per lo stretching
    void HandleStretching()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(-mainCamera.transform.forward, initialCornerWorld);

        if (plane.Raycast(ray, out float enter))
        {
            Vector3 currentMouseWorld = ray.GetPoint(enter);
            Vector3 delta = currentMouseWorld - initialMousePosWorld;

            float scaleAmount = Vector3.Dot(delta, stretchAxis);
            Vector3 newScale = initialScale + stretchAxis * scaleAmount * 2f;

            newScale = Vector3.Max(newScale, Vector3.one * 0.2f);
            grabbedObject.transform.localScale = newScale;
        }
    }

    // Verifica se l'oggetto cliccato è un corner valido per lo stretch
    bool IsCorner(GameObject hitObject, out Vector3 axisOut, out Vector3 cornerWorldPos)
    {
        axisOut = Vector3.zero;
        cornerWorldPos = Vector3.zero;

        // Deve avere il tag giusto ed essere figlio di qualcosa
        if (!hitObject.CompareTag("StretchCorner") || hitObject.transform.parent == null)
            return false;

        Transform parent = hitObject.transform.parent;

        // Il padre deve avere tag "Stretch"
        if (!parent.CompareTag("Stretch"))
            return false;

        // Calcolo direzione e posizione del corner
        Vector3 localCorner = hitObject.transform.localPosition;
        axisOut = localCorner.normalized;
        cornerWorldPos = hitObject.transform.position;

        return true;
    }


    // Regolare la visibilità durante lo stretch
    void SetStretchVisibility(GameObject stretch, bool visible)
    {
        Renderer parentRenderer = stretch.GetComponent<Renderer>();
        if (parentRenderer != null)
        {
            parentRenderer.enabled = visible;
        }

        foreach (Transform child in stretch.transform)
        {
            if (child.CompareTag("Oggetto")) continue;

            Renderer rend = child.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.enabled = visible;
            }
        }
    }

    // Verifica che il "child" sia legato al "parent"
    bool IsChildOf(Transform child, Transform parent)
    {
        while (child != null)
        {
            if (child == parent) return true;
            child = child.parent;
        }
        return false;
    }
}