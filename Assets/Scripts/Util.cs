using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Util {

    /**
       Decompose the rotation on to 2 parts.
       1. Twist - rotation around the "direction" vector
       2. Swing - rotation around axis that is perpendicular to "direction" vector
       The rotation can be composed back by 
       rotation = swing * twist

       has singularity in case of swing_rotation close to 180 degrees rotation.
       if the input quaternion is of non-unit length, the outputs are non-unit as well
       otherwise, outputs are both unit
    */
    public static void SwingTwistDecomposition(Quaternion rotation, Vector3 direction, out Quaternion swing, out Quaternion twist) {
        Vector3 RotationAxis = new Vector3(rotation.x, rotation.y, rotation.z); // rotation axis
        Vector3 p = Vector3.Project(RotationAxis, direction); // return projection v1 on to v2  (parallel component)
        twist = new Quaternion(p.x, p.y, p.z, rotation.w);
        twist.Normalize();
        swing = rotation * Quaternion.Inverse(twist);
    }
}
