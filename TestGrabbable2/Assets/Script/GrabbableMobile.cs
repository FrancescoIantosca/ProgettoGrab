using UnityEngine;

public class GrabbableMobile : MonoBehaviour
{
    public float grabDistance = 3.5f;
    public float smoothSpeed = 10.0f;
    private float rotationSpeed = 40.0f;

    public Camera mainCamera;

    private GameObject grabbedObject;
    private Vector3 grabOffset;

    private bool isStretching = false;
    private Vector3 initialTouchPosWorld;
    private Vector3 initialScale;
    private Vector3 initialCornerWorld;
    private Vector3 stretchAxis;

    private bool isRotating = false;
    private string rotationTag = "";

    private Renderer activeCornerRenderer;
    private Renderer activeRotationRenderer;
    private Renderer hoveredRenderer;
    private Renderer grabbedObjectRenderer;

    private Color originalCornerColor = new Color32(128, 128, 128, 255);
    private Color originalRotationColor = new Color32(128, 128, 128, 255);
    private Color originalObjectColor = new Color32(128, 128, 128, 255);
    private Color highlightColor = new Color32(173, 216, 230, 255);
    private Color hoverColor = new Color32(0, 0, 139, 255);

    void Update()
    {
        HandleHoverHighlight();

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    TryGrabOrStretchObject(touch.position);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (grabbedObject != null)
                    {
                        if (isStretching)
                        {
                            HandleStretching(touch.position);
                        }
                        else if (isRotating)
                        {
                            DragRotateObject(touch.deltaPosition);
                        }
                        else
                        {
                            MoveObject(touch.position);
                        }
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    ReleaseObject();
                    break;
            }
        }
    }

    void HandleHoverHighlight()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        Ray ray = mainCamera.ScreenPointToRay(touch.position);
        RaycastHit hit;

        if (hoveredRenderer != null &&
            hoveredRenderer != activeCornerRenderer &&
            hoveredRenderer != activeRotationRenderer)
        {
            hoveredRenderer.material.color = originalRotationColor;
            hoveredRenderer = null;
        }

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

            if (hitObj.transform.parent != null && hitObj.transform.parent.CompareTag("Stretch"))
            {
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

            if (tag == "Stretch" || hitObj.transform.parent?.CompareTag("Stretch") == true)
            {
                GameObject stretchRoot = hitObj.CompareTag("Stretch") ? hitObj : hitObj.transform.parent.gameObject;
                SetStretchVisibility(stretchRoot, true);
            }
        }
        else
        {
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

    void TryGrabOrStretchObject(Vector2 touchPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(touchPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabDistance))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (IsCorner(hitObject, out stretchAxis, out initialCornerWorld))
            {
                grabbedObject = hitObject.transform.parent.gameObject;
                initialScale = grabbedObject.transform.localScale;

                Plane plane = new Plane(-mainCamera.transform.forward, initialCornerWorld);
                if (plane.Raycast(ray, out float enter))
                {
                    initialTouchPosWorld = ray.GetPoint(enter);
                }

                isStretching = true;

                activeCornerRenderer = hitObject.GetComponent<Renderer>();
                if (activeCornerRenderer != null)
                {
                    originalCornerColor = activeCornerRenderer.material.color;
                    activeCornerRenderer.material.color = highlightColor;
                }
            }
            else if (hitObject.CompareTag("RotationVertical") || hitObject.CompareTag("RotationOrizontal"))
            {
                grabbedObject = hitObject.transform.parent.gameObject;
                isRotating = true;
                rotationTag = hitObject.tag;

                activeRotationRenderer = hitObject.GetComponent<Renderer>();
                if (activeRotationRenderer != null)
                {
                    originalRotationColor = activeRotationRenderer.material.color;
                    activeRotationRenderer.material.color = highlightColor;
                }
            }
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

    void MoveObject(Vector2 touchPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(touchPos);
        Vector3 targetPoint = ray.GetPoint(grabDistance) + grabOffset;
        grabbedObject.transform.position = Vector3.Lerp(grabbedObject.transform.position, targetPoint, smoothSpeed * Time.deltaTime);
    }

    void DragRotateObject(Vector2 delta)
    {
        float mouseX = delta.x * 0.1f;
        float mouseY = delta.y * 0.1f;

        if (rotationTag == "RotationVertical")
        {
            grabbedObject.transform.Rotate(Vector3.right, -mouseY * rotationSpeed * Time.deltaTime, Space.World);
        }
        else if (rotationTag == "RotationOrizontal")
        {
            grabbedObject.transform.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    void HandleStretching(Vector2 touchPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(touchPos);
        Plane plane = new Plane(-mainCamera.transform.forward, initialCornerWorld);

        if (plane.Raycast(ray, out float enter))
        {
            Vector3 currentTouchWorld = ray.GetPoint(enter);
            Vector3 delta = currentTouchWorld - initialTouchPosWorld;

            float scaleAmount = Vector3.Dot(delta, stretchAxis);
            Vector3 newScale = initialScale + stretchAxis * scaleAmount * 2f;

            newScale = Vector3.Max(newScale, Vector3.one * 0.2f);
            grabbedObject.transform.localScale = newScale;
        }
    }

    bool IsCorner(GameObject hitObject, out Vector3 axisOut, out Vector3 cornerWorldPos)
    {
        axisOut = Vector3.zero;
        cornerWorldPos = Vector3.zero;

        if (!hitObject.CompareTag("StretchCorner") || hitObject.transform.parent == null)
            return false;

        Transform parent = hitObject.transform.parent;
        if (!parent.CompareTag("Stretch"))
            return false;

        Vector3 localCorner = hitObject.transform.localPosition;
        axisOut = localCorner.normalized;
        cornerWorldPos = hitObject.transform.position;

        return true;
    }

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
