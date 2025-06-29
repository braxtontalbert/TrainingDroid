using UnityEngine;
using ThunderRoad;
using UnityEngine.PlayerLoop;

namespace TrainingDroid
{
    public class PID
    {
        public enum DerivativeMeasurement
        {
            Velocity,
            ErrorRateOfChange
        }
        public float proportionalGain;
        public float integralGain;
        public float derivativeGain;

        public Vector3 integrationStored = new Vector3();
        public Vector3 integralSaturation = new Vector3(1,1,1);

        public float outputMin = -1f;
        public float outputMax = 1f;

        public Vector3 errorLast;
        public Vector3 angleErrorLast;
        public Vector3 valueLast;
        public DerivativeMeasurement derivativeMeasurement;
        public bool dTermInitialized;

        public Vector3 Update(float dt, Vector3 currentValue, Vector3 targetValue)
        {
            Vector3 error = (targetValue - currentValue);

            Vector3 P = proportionalGain * error;

            Vector3 errorRateOfChange = (error - errorLast) / dt;
            errorLast = error;

            Vector3 valueRateOfChange = (currentValue - valueLast) / dt;
            valueLast = currentValue;

            Vector3 derivativeMeasure = new Vector3(0, 0, 0);
            if (dTermInitialized)
            {
                if (derivativeMeasurement == DerivativeMeasurement.Velocity)
                {
                    derivativeMeasure = -valueRateOfChange;
                }
                else
                {
                    derivativeMeasure = errorRateOfChange;
                }
            }
            else
            {
                dTermInitialized = true;
            }

            Vector3 D = derivativeGain * derivativeMeasure;


            integrationStored = ClampVector(integrationStored + (error * dt), 
                -integralSaturation, integralSaturation);
            Vector3 I = integralGain * integrationStored;
            //Vector3 result = P + Vector3.ClampMagnitude(I, outputMax) + D;
            Vector3 result = P + I + D;
            //return Vector3.ClampMagnitude(result, outputMax);
            return ClampVector(result, new Vector3(outputMin, outputMin, outputMin), new Vector3(outputMax, outputMax, outputMax));
        }
        
        // UpdateAngle method in PID class
        public Vector3 UpdateAngle(float dt, Vector3 axis, float angularVelocity)
        {
            // Calculate the angular velocity vector
            Vector3 angularVelocityVector = axis * angularVelocity;

            // Apply the angular velocity to the rotation
            Vector3 result = angularVelocityVector * dt;

            return result;
        }
        Vector3 ClampVector(Vector3 toClamp, Vector3 first, Vector3 second)
        {
            
            Vector3 returned = new Vector3();

            returned.x = Mathf.Clamp(toClamp.x, first.x, second.x);
            returned.y = Mathf.Clamp(toClamp.y, first.y, second.y);
            returned.z = Mathf.Clamp(toClamp.z, first.z, second.z);

            return returned;
        }

        /*public void UpdateAngle(float dt, Vector3 currentAngle, Vector3 targetAngle)
        {
            Vector3 error = (Vector3.);
                angleErrorLast = error;
        }*/
        

        public void Reset()
        {
            dTermInitialized = false;
        }
        
    }
}