#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    /// <summary>
    /// A path that an AI can follow through the world
    /// </summary>
    public abstract class PathBase : MonoBehaviour {
        /// <summary>
        /// Get a param on the path whose point is close to the specified position, assuming
        /// the param is not far from the specified lastParam
        /// (in case multiple legs of the path come close to the same position)
        /// </summary>
        /// A param is a floating point number representing a position on the path, with
        /// 0 at the start, and some value l at the end (which might be the length of the path).
        /// If the path loops, the point at param l is the same as the 0 point,
        /// nd l+x is the same as x, and likewise, 0-x is the same as l-x.  If the path
        /// doesn't loop, values less than 0 are the same as 0, and values greater than l are
        /// the same as l.
        /// <param name="position">The world-space position to query</param>
        /// <param name="lastParam">A parameter value close to the desired param</param>
        /// <returns></returns>
        public abstract float GetParam(Vector3 position, float lastParam);

        /// <summary>
        /// Return the position on the path corresponding to the specified param.
        /// </summary>
        /// <param name="param">The param to evaluate</param>
        /// <returns>The world-space position</returns>
        public abstract Vector3 GetPosition(float param);
    }
}