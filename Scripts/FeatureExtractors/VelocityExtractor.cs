using System.Collections;
using UnityEngine;

namespace InteractML.Telemetry.Extractors
{
    public class VelocityExtractor
    {
        #region Variables

        /// <summary>
        /// The feature that has been previously extracted and from which we are calculating the velocity (i.e. position, rotation, etc)
        /// </summary>
        public FeatureTelemetry FeatureToInput;

        /// <summary>
        /// Velocity that has been extracted this frame
        /// </summary>
        public float[] VelocityExtracted;

        /// <summary>
        /// The private feature values extracted in a more specific data type
        /// </summary>
        private float[] m_CurrentVelocity;
        private float[] m_PreviousFeatureValues;

        /// <summary>
        /// Used to calculate the velocity
        /// </summary>
        public float[] m_LastFrameFeatureValue;

        /// <summary>
        /// Was the feature already updated?
        /// </summary>
        public bool isUpdated { get; set; }

        #endregion

        #region Constructor/Destructor

        /// <summary>
        /// Constructor
        /// </summary>

        public VelocityExtractor()
        {
            Initialize(null);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public VelocityExtractor(FeatureTelemetry input)
        {
            Initialize(input);
        }

        #endregion

        // Use this for initialization
        private void Initialize(FeatureTelemetry input)
        {
            // The velocity extractor expects any other feature extracted to make calculations
            FeatureToInput = input;

            // If we managed to get the input
            if (FeatureToInput != null)
            {
                var featureToUse = FeatureToInput.Data;
                if (featureToUse != null)
                {
                    // Calculate the velocity arrays size
                    m_CurrentVelocity = new float[featureToUse.Length];
                    m_LastFrameFeatureValue = new float[m_CurrentVelocity.Length];
                    VelocityExtracted = new float[m_CurrentVelocity.Length];

                    // initialise helper variables
                    m_PreviousFeatureValues = new float[m_CurrentVelocity.Length];
                }
            }
            else
            {
                // Initialise velocity arrays
                m_CurrentVelocity = new float[0];
                m_LastFrameFeatureValue = new float[0];
                VelocityExtracted = new float[m_CurrentVelocity.Length];

                // initialise helper variables
                m_PreviousFeatureValues = new float[m_CurrentVelocity.Length];
            }
        }

        /// <summary>
        /// Updates Feature values
        /// </summary>
        /// <returns></returns>
        public float[] UpdateFeature(FeatureTelemetry input)
        {
            // Get values from the input list
            FeatureToInput = input;

            // If we managed to get the input
            if (FeatureToInput != null)
            {
                // We check that it is an IML Feature
                var featureToUse = input.Data;
                if (featureToUse != null)
                {
                    // If the velocity hasn't been updated yet
                    if (!isUpdated)
                    {
                        // Calculate velocity itself
                        for (int i = 0; i < m_CurrentVelocity.Length; i++)
                            m_CurrentVelocity[i] = (featureToUse[i] - m_LastFrameFeatureValue[i]) / Time.smoothDeltaTime;

                        // Set values for velocity extracted
                        if (VelocityExtracted == null || VelocityExtracted.Length != m_CurrentVelocity.Length)
                            VelocityExtracted = new float[m_CurrentVelocity.Length];

                        // Set velocity extracted
                        for (int i = 0; i < m_CurrentVelocity.Length; i++)
                        {
                            VelocityExtracted[i] = m_CurrentVelocity[i];
                        }

                        // Set values for last frame feature value
                        m_LastFrameFeatureValue = m_CurrentVelocity;

                        // COMMENTED FOR THE MOMENT, MIGHT NOT BE NEEDED HERE// Make sure to mark the feature as updated to avoid calculating twice 
                        //isUpdated = true;
                    }

                    return m_CurrentVelocity;
                }
                else
                {
                    // If input is not an IML feature, return null
                    return null;
                }
            }
            // If we couldn't get an input, we return null
            else
            {
                return null;
            }

        }


    }
}