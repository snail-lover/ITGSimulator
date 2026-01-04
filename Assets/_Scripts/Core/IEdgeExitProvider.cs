// IEdgeExitProvider.cs  (Core)
using UnityEngine;

public interface IEdgeExitProvider
{
    /// <summary>
    /// If 'interactor' is currently standing on this surface and wants to go toward
    /// 'desiredWorldPoint' OFF the surface, compute an edge point ON the surface and
    /// a safe ground target at the base. Return true if computed successfully.
    /// </summary>
    bool TryComputeExit(GameObject interactor, Vector3 desiredWorldPoint,
                        out Vector3 topEdgePoint, out Vector3 groundTarget);
}
