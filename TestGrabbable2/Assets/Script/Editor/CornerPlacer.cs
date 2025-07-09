using UnityEngine;
using UnityEditor;

public class CornerPlacer : MonoBehaviour
{
    [MenuItem("Tools/Generate Corners For Selected Stretch Object")]
    static void GenerateCorners()
    {
        if (Selection.activeGameObject == null || Selection.activeGameObject.tag != "Stretch")
        {
            Debug.LogWarning("Seleziona un GameObject con tag 'Stretch'!");
            return;
        }

        GameObject sfere = Selection.activeGameObject;
        Vector3 halfScale = sfere.transform.localScale / 2f;

        int i = 0;
        foreach (int xSign in new int[] { -1, 1 })
            foreach (int ySign in new int[] { -1, 1 })
                foreach (int zSign in new int[] { -1, 1 })
                {
                    Vector3 localPos = new Vector3(
                        xSign * halfScale.x,
                        ySign * halfScale.y,
                        zSign * halfScale.z
                    );

                    GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    corner.name = $"Corner_{i}";
                    corner.transform.parent = sfere.transform;
                    corner.transform.localPosition = localPos;
                    corner.transform.localRotation = Quaternion.identity;
                    corner.transform.localScale = Vector3.one * 0.1f;

                    // Imposta il tag "StretchCorner"
                    corner.tag = "StretchCorner";

                    // Rimuove Rigidbody se presente
                    if (corner.TryGetComponent(out Rigidbody rb))
                        Object.DestroyImmediate(rb);

                    i++;
                }

        Debug.Log("8 Corner aggiunti al cubo senza Collider!");
    }
}
