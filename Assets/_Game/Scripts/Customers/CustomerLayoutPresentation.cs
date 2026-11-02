using UnityEngine;

[DisallowMultipleComponent]
public class CustomerLayoutPresentation : MonoBehaviour
{
    private Renderer[] renderers;
    private bool[] previousForceRenderingOffStates;

    private Collider[] colliders;
    private bool[] previousColliderEnabledStates;

    private bool isHidden;

    public void HideForLayout()
    {
        if (isHidden)
            return;

        isHidden = true;

        HideRenderers();
        DisableColliders();
    }

    public void ShowAfterLayout()
    {
        if (!isHidden)
            return;

        RestoreRenderers();
        RestoreColliders();

        isHidden = false;
    }

    private void HideRenderers()
    {
        renderers =
            GetComponentsInChildren<Renderer>(true);

        previousForceRenderingOffStates =
            new bool[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer customerRenderer =
                renderers[i];

            if (customerRenderer == null)
                continue;

            previousForceRenderingOffStates[i] =
                customerRenderer.forceRenderingOff;

            customerRenderer.forceRenderingOff = true;
        }
    }

    private void RestoreRenderers()
    {
        if (renderers == null ||
            previousForceRenderingOffStates == null)
        {
            return;
        }

        int count =
            Mathf.Min(
                renderers.Length,
                previousForceRenderingOffStates.Length
            );

        for (int i = 0; i < count; i++)
        {
            Renderer customerRenderer =
                renderers[i];

            if (customerRenderer == null)
                continue;

            customerRenderer.forceRenderingOff =
                previousForceRenderingOffStates[i];
        }

        renderers = null;
        previousForceRenderingOffStates = null;
    }

    private void DisableColliders()
    {
        colliders =
            GetComponentsInChildren<Collider>(true);

        previousColliderEnabledStates =
            new bool[colliders.Length];

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider customerCollider =
                colliders[i];

            if (customerCollider == null)
                continue;

            previousColliderEnabledStates[i] =
                customerCollider.enabled;

            customerCollider.enabled = false;
        }
    }

    private void RestoreColliders()
    {
        if (colliders == null ||
            previousColliderEnabledStates == null)
        {
            return;
        }

        int count =
            Mathf.Min(
                colliders.Length,
                previousColliderEnabledStates.Length
            );

        for (int i = 0; i < count; i++)
        {
            Collider customerCollider =
                colliders[i];

            if (customerCollider == null)
                continue;

            customerCollider.enabled =
                previousColliderEnabledStates[i];
        }

        colliders = null;
        previousColliderEnabledStates = null;
    }
}