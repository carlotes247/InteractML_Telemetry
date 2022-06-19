using System.Collections.Generic;
using UnityEngine;

namespace InteractML.Telemetry
{
    [System.Serializable]
    public class FeatureTelemetry
    {
        public string FeatureName;
        public string GameObject;
        public float[] Data;

        public void AddAsPosition(GameObject go)
        {
            if (go != null)
            {
                FeatureName = "Position";
                GameObject = go.name;
                Data = new float[3];
                Data[0] = go.transform.position.x;
                Data[1] = go.transform.position.y;
                Data[2] = go.transform.position.z;
            }
        }

        public void AddAsRotation(GameObject go, bool isEuler = false)
        {
            if (go != null)
            {
                if (isEuler)
                {
                    FeatureName = "Rotation (Euler)";
                    GameObject = go.name;
                    Data = new float[3];
                    Data[0] = go.transform.rotation.eulerAngles.x;
                    Data[1] = go.transform.rotation.eulerAngles.y;
                    Data[2] = go.transform.rotation.eulerAngles.z;
                }
                else
                {
                    FeatureName = "Rotation (Quaternion)";
                    GameObject = go.name;
                    Data = new float[4];
                    Data[0] = go.transform.rotation.x;
                    Data[1] = go.transform.rotation.y;
                    Data[2] = go.transform.rotation.z;
                    Data[3] = go.transform.rotation.w;
                }
            }
        }

        public void AddAsVelocity(Vector3 inVector, bool isRotation = false)
        {

        }

        public void AddAsVelocity(Quaternion inQuaternion)
        {

        }

        public void AddAsAcceleration(Vector3 inVector, bool isRotation = false)
        {

        }

        public void AddAsAcceleration(Quaternion inQuaternion)
        {

        }
    }
}