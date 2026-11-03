using UnityEngine;

public enum FurnitureSnapSocketType
{
    RunEdge = 0
}

public enum FurnitureSnapSocketSide
{
    Left = 0,
    Right = 1
}

[DisallowMultipleComponent]
public sealed class FurnitureSnapSocket : MonoBehaviour
{
    [Header("Compatibility")] 
    [SerializeField] private FurnitureSnapSocketType socketType = FurnitureSnapSocketType.RunEdge;
    [SerializeField] private FurnitureSnapSocketSide side = FurnitureSnapSocketSide.Left;

    public FurnitureSnapSocketType SocketType => socketType;
    public FurnitureSnapSocketSide Side => side;

    public FurnitureIdentity Owner => GetComponentInParent<FurnitureIdentity>();

    public Vector3 WorldOutward => transform.forward.normalized;

    public bool IsCompatibleWith(FurnitureSnapSocket other)
    {
        if (other == null || other == this)
        {
            return false;
        }

        return socketType == other.socketType && side != other.side;
    }

    public Vector3 GetLocalPosition(FurnitureIdentity owner)
    {
        return owner != null ? owner.transform.InverseTransformPoint(transform.position) : transform.localPosition;
    }

    public Vector3 GetLocalOutward(FurnitureIdentity owner)
    {
        Vector3 direction = owner != null
            ? owner.transform.InverseTransformDirection(transform.forward)
            : transform.forward;

        direction.y = 0f;

        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
    }

    private void OnDrawGizmosSelected()
    {
        // Editor-only authoring aid.
        // #5FE3F0FF
        Gizmos.color = new Color32(95, 227, 240, 255);

        Gizmos.DrawWireSphere(transform.position, 0.08f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.35f);
    }
}