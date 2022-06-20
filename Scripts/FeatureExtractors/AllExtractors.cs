using System.Collections;
using UnityEngine;

namespace InteractML.Telemetry.Extractors
{
    /// <summary>
    /// A combined class wrapping all possible velocity and acceleration extractors for a GameObject
    /// </summary>
    public class AllExtractors
    {
        public VelocityExtractor VelocityPosition;
        public VelocityExtractor AccelerationPosition;
        public VelocityExtractor VelocityRotationQuat;
        public VelocityExtractor AccelerationRotationQuat;
        public VelocityExtractor VelocityRotationEuler;
        public VelocityExtractor AccelerationRotationEuler;   
        
        public AllExtractors(GameObject go)
        {
            VelocityPosition = new VelocityExtractor();
            AccelerationPosition = new VelocityExtractor();
            VelocityRotationQuat = new VelocityExtractor();
            AccelerationRotationQuat = new VelocityExtractor();
            VelocityRotationEuler = new VelocityExtractor();
            AccelerationRotationEuler = new VelocityExtractor();
        }
    }
}