/*
 * Realistic Sniper and Ballistics System
 * Copyright (c) 2021, Inan Evin, Inc. All rights reserved.
 * Author: Inan Evin
 * https://www.inanevin.com/
 * 
 * Documentation: https://rsb.inanevin.com
 * Contact: inanevin@gmail.com
 * Support Discord: https://discord.gg/RCzpSAmBAb
 *
 * Feel free to ask about RSB, send feature recommendations or any other feedback!
 */

using UnityEngine;

namespace IE.RSB
{
    /// <summary>
    /// Contains utility methods to handle common calculations of ballistics properties.
    /// </summary>
    public static class BallisticsUtility
    {
        public enum DragGModel { G1, G2, G5, G6, G7, G8 };

        /// <summary>
        /// Given a wind vector in kilometers/hour unit, calculates & returns a wind vector in meters/second.
        /// </summary>
        /// <param name="windSpeedInMetrics">Kilometer per hour</param>
        /// <param name="delta">Step time</param>
        /// <returns></returns>
        public static Vector3 GetWindVector(Vector3 windSpeedInMetrics, float delta)
        {
            return windSpeedInMetrics * 0.1f * ConversionConstants.s_kmhToMS * delta;
        }

        /// <summary>
        /// Returns a simple vector that gravity acceleration & step time is incorporated into.
        /// </summary>
        /// <param name="gravityAcceleration">Gravity, -9.81 default</param>
        /// <param name="delta">Step time</param>
        /// <returns></returns>
        public static Vector3 GetGravity(float gravityAcceleration, float delta)
        {
            return new Vector3(0, gravityAcceleration * delta, 0);
        }


        /// <summary>
        /// Calculates & returns kinetic energy based on object velocity & mass.
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="massInMetric"></param>
        /// <returns></returns>
        public static float GetKineticEnergy(Vector3 velocity, float massInMetric)
        {
            float ke = velocity.magnitude * velocity.magnitude * 0.5f * massInMetric * 0.001f;
            return ke;
        }

        /// <summary>
        /// Given a new kinetic energy, calculates a new velocity & sets the velocity & old kinetic energy accordingly.
        /// </summary>
        /// <param name="velocity">Current velocity of the object.</param>
        /// <param name="kineticEnergy">Current kinetic energy of the object.</param>
        /// <param name="mass">Mass of the object.</param>
        /// <param name="newKE">Desired kinetic energy of the object.</param>
        public static void UpdateKEAndVelocity(ref Vector3 velocity, ref float kineticEnergy, float mass, float newKE)
        {
            float velocityMagnitudeForNewKE = newKE <= 0.0f ? 0.0f : Mathf.Sqrt(newKE / (0.5f * mass * 0.001f));
            velocity = Vector3.ClampMagnitude(velocity, velocityMagnitudeForNewKE);
            kineticEnergy = GetKineticEnergy(velocity, mass);
        }

        /// <summary>
        /// Given current velocity & ballistic coefficient, returns a drag vector.
        /// </summary>
        /// <param name="currentVelocity"></param>
        /// <param name="gModel"></param>
        /// <param name="ballisticCoef"></param>
        /// <param name="delta">Step time.</param>
        /// <returns></returns>
        public static Vector3 GetDragVector(Vector3 currentVelocity, DragGModel gModel, float ballisticCoef, float delta)
        {
            double velocityFPS = currentVelocity.magnitude * ConversionConstants.s_meterToFeet;
            double dragCoefficient = GetRetardation(velocityFPS, ballisticCoef, gModel);
            return currentVelocity.normalized * (float)dragCoefficient * delta;
        }


        /* 
            Drag Models & Spin Drift Func: C919,

            THIS SOFTWARE IS PROVIDED BY THE AUTHOR ''AS IS'' AND ANY EXPRESS
            OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
            WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
            ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE
            LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
            CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
            SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
            BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
            WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE
            OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
            EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
        */

        /// <summary>
        /// Calculates & returns the spin drift vector.
        /// </summary>
        /// <param name="previousDrift"></param>
        /// <param name="stabilityFactor"></param>
        /// <param name="delta"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 GetSpinDrift(ref Vector3 previousDrift, double stabilityFactor, float delta, float t)
        {
            //double spinDrift = 0.0254 * 1.25 * (stabilityFactor + 1.2) * System.Math.Pow(t, 1.83);
            double spinDrift = 1.02 * (stabilityFactor + 1.2) * System.Math.Pow(t, 1.09);
            float spinX = (float)spinDrift;
            Vector3 drift = new Vector3(spinX, 0, 0);
            drift -= previousDrift;
            previousDrift = new Vector3(spinX, 0, 0);
            return drift * delta;
        }

        /// <summary>
        /// Calculates & returns the stability factor of the bullet.
        /// </summary>
        /// <param name="bulletLengthInMetrics"></param>
        /// <param name="bulletDiameterInMetrics"></param>
        /// <param name="barrelTwistInMetrics"></param>
        /// <param name="bulletMassInMetrics"></param>
        /// <param name="muzzleVelocityInMetrics"></param>
        /// <param name="airPressureInMetrics"></param>
        /// <param name="temperatureInMetrics"></param>
        /// <returns></returns>
        public static double GetStability(float bulletLengthInMetrics, float bulletDiameterInMetrics, float barrelTwistInMetrics, float bulletMassInMetrics, float muzzleVelocityInMetrics, float airPressureInMetrics, float temperatureInMetrics)
        {
            double bulletMass = bulletMassInMetrics * ConversionConstants.s_gramToPound;
            double airPressure = airPressureInMetrics;
            double temperature = ConversionConstants.CelciusToFahrenheit(temperatureInMetrics);
            double muzzleVelocity = muzzleVelocityInMetrics * ConversionConstants.s_meterToFeet;
            double bulletLength = bulletLengthInMetrics * ConversionConstants.s_mmToInch;
            double bulletDiameter = bulletDiameterInMetrics * ConversionConstants.s_mmToInch;
            double barrelTwist = barrelTwistInMetrics * ConversionConstants.s_mmToInch;
            double cals = bulletLength / bulletDiameter;
            double twistCals = barrelTwist / bulletDiameter;
            double stabilityFactor = 30 * bulletMass / (System.Math.Pow(twistCals, 2) * System.Math.Pow(bulletDiameter, 3) * cals * (1 + System.Math.Pow(cals, 2)));
            stabilityFactor *= System.Math.Pow(((muzzleVelocity / 0.3048) / 2800f), (1f / 3f));
            stabilityFactor *= (temperature + 273.15) / (15 + 273.15) * 1013.25 / airPressure;
            return stabilityFactor;
        }

        /// <summary>
        /// Calculates & returns how much variance (retardation) would the bullet have in given velocity & drag model.
        /// </summary>
        /// <param name="velocityFPS"></param>
        /// <param name="ballisticCoef"></param>
        /// <param name="gModel"></param>
        /// <returns></returns>
        public static double GetRetardation(double velocityFPS, double ballisticCoef, DragGModel gModel)
        {
            double A = -1;
            double M = -1;

            if (gModel == DragGModel.G1)
            {
                if (velocityFPS > 4230) { A = 1.477404177730177e-04; M = 1.9565; }
                else if (velocityFPS > 3680) { A = 1.920339268755614e-04; M = 1.925; }
                else if (velocityFPS > 3450) { A = 2.894751026819746e-04; M = 1.875; }
                else if (velocityFPS > 3295) { A = 4.349905111115636e-04; M = 1.825; }
                else if (velocityFPS > 3130) { A = 6.520421871892662e-04; M = 1.775; }
                else if (velocityFPS > 2960) { A = 9.748073694078696e-04; M = 1.725; }
                else if (velocityFPS > 2830) { A = 1.453721560187286e-03; M = 1.675; }
                else if (velocityFPS > 2680) { A = 2.162887202930376e-03; M = 1.625; }
                else if (velocityFPS > 2460) { A = 3.209559783129881e-03; M = 1.575; }
                else if (velocityFPS > 2225) { A = 3.904368218691249e-03; M = 1.55; }
                else if (velocityFPS > 2015) { A = 3.222942271262336e-03; M = 1.575; }
                else if (velocityFPS > 1890) { A = 2.203329542297809e-03; M = 1.625; }
                else if (velocityFPS > 1810) { A = 1.511001028891904e-03; M = 1.675; }
                else if (velocityFPS > 1730) { A = 8.609957592468259e-04; M = 1.75; }
                else if (velocityFPS > 1595) { A = 4.086146797305117e-04; M = 1.85; }
                else if (velocityFPS > 1520) { A = 1.954473210037398e-04; M = 1.95; }
                else if (velocityFPS > 1420) { A = 5.431896266462351e-05; M = 2.125; }
                else if (velocityFPS > 1360) { A = 8.847742581674416e-06; M = 2.375; }
                else if (velocityFPS > 1315) { A = 1.456922328720298e-06; M = 2.625; }
                else if (velocityFPS > 1280) { A = 2.419485191895565e-07; M = 2.875; }
                else if (velocityFPS > 1220) { A = 1.657956321067612e-08; M = 3.25; }
                else if (velocityFPS > 1185) { A = 4.745469537157371e-10; M = 3.75; }
                else if (velocityFPS > 1150) { A = 1.379746590025088e-11; M = 4.25; }
                else if (velocityFPS > 1100) { A = 4.070157961147882e-13; M = 4.75; }
                else if (velocityFPS > 1060) { A = 2.938236954847331e-14; M = 5.125; }
                else if (velocityFPS > 1025) { A = 1.228597370774746e-14; M = 5.25; }
                else if (velocityFPS > 980) { A = 2.916938264100495e-14; M = 5.125; }
                else if (velocityFPS > 945) { A = 3.855099424807451e-13; M = 4.75; }
                else if (velocityFPS > 905) { A = 1.185097045689854e-11; M = 4.25; }
                else if (velocityFPS > 860) { A = 3.566129470974951e-10; M = 3.75; }
                else if (velocityFPS > 810) { A = 1.045513263966272e-08; M = 3.25; }
                else if (velocityFPS > 780) { A = 1.291159200846216e-07; M = 2.875; }
                else if (velocityFPS > 750) { A = 6.824429329105383e-07; M = 2.625; }
                else if (velocityFPS > 700) { A = 3.569169672385163e-06; M = 2.375; }
                else if (velocityFPS > 640) { A = 1.839015095899579e-05; M = 2.125; }
                else if (velocityFPS > 600) { A = 5.71117468873424e-05; M = 1.950; }
                else if (velocityFPS > 550) { A = 9.226557091973427e-05; M = 1.875; }
                else if (velocityFPS > 250) { A = 9.337991957131389e-05; M = 1.875; }
                else if (velocityFPS > 100) { A = 7.225247327590413e-05; M = 1.925; }
                else if (velocityFPS > 65) { A = 5.792684957074546e-05; M = 1.975; }
                else if (velocityFPS > 0) { A = 5.206214107320588e-05; M = 2.000; }
            }

            if (gModel == DragGModel.G2)
            {
                if (velocityFPS > 1674) { A = .0079470052136733; M = 1.36999902851493; }
                else if (velocityFPS > 1172) { A = 1.00419763721974e-03; M = 1.65392237010294; }
                else if (velocityFPS > 1060) { A = 7.15571228255369e-23; M = 7.91913562392361; }
                else if (velocityFPS > 949) { A = 1.39589807205091e-10; M = 3.81439537623717; }
                else if (velocityFPS > 670) { A = 2.34364342818625e-04; M = 1.71869536324748; }
                else if (velocityFPS > 335) { A = 1.77962438921838e-04; M = 1.76877550388679; }
                else if (velocityFPS > 0) { A = 5.18033561289704e-05; M = 1.98160270524632; }
            }

            if (gModel == DragGModel.G5)
            {
                if (velocityFPS > 1730) { A = 7.24854775171929e-03; M = 1.41538574492812; }
                else if (velocityFPS > 1228) { A = 3.50563361516117e-05; M = 2.13077307854948; }
                else if (velocityFPS > 1116) { A = 1.84029481181151e-13; M = 4.81927320350395; }
                else if (velocityFPS > 1004) { A = 1.34713064017409e-22; M = 7.8100555281422; }
                else if (velocityFPS > 837) { A = 1.03965974081168e-07; M = 2.84204791809926; }
                else if (velocityFPS > 335) { A = 1.09301593869823e-04; M = 1.81096361579504; }
                else if (velocityFPS > 0) { A = 3.51963178524273e-05; M = 2.00477856801111; }
            }

            if (gModel == DragGModel.G6)
            {
                if (velocityFPS > 3236) { A = 0.0455384883480781; M = 1.15997674041274; }
                else if (velocityFPS > 2065) { A = 7.167261849653769e-02; M = 1.10704436538885; }
                else if (velocityFPS > 1311) { A = 1.66676386084348e-03; M = 1.60085100195952; }
                else if (velocityFPS > 1144) { A = 1.01482730119215e-07; M = 2.9569674731838; }
                else if (velocityFPS > 1004) { A = 4.31542773103552e-18; M = 6.34106317069757; }
                else if (velocityFPS > 670) { A = 2.04835650496866e-05; M = 2.11688446325998; }
                else if (velocityFPS > 0) { A = 7.50912466084823e-05; M = 1.92031057847052; }
            }

            if (gModel == DragGModel.G7)
            {
                if (velocityFPS > 4200) { A = 1.29081656775919e-09; M = 3.24121295355962; }
                else if (velocityFPS > 3000) { A = 0.0171422231434847; M = 1.27907168025204; }
                else if (velocityFPS > 1470) { A = 2.33355948302505e-03; M = 1.52693913274526; }
                else if (velocityFPS > 1260) { A = 7.97592111627665e-04; M = 1.67688974440324; }
                else if (velocityFPS > 1110) { A = 5.71086414289273e-12; M = 4.3212826264889; }
                else if (velocityFPS > 960) { A = 3.02865108244904e-17; M = 5.99074203776707; }
                else if (velocityFPS > 670) { A = 7.52285155782535e-06; M = 2.1738019851075; }
                else if (velocityFPS > 540) { A = 1.31766281225189e-05; M = 2.08774690257991; }
                else if (velocityFPS > 0) { A = 1.34504843776525e-05; M = 2.08702306738884; }
            }

            if (gModel == DragGModel.G8)
            {
                if (velocityFPS > 3571) { A = .0112263766252305; M = 1.33207346655961; }
                else if (velocityFPS > 1841) { A = .0167252613732636; M = 1.28662041261785; }
                else if (velocityFPS > 1120) { A = 2.20172456619625e-03; M = 1.55636358091189; }
                else if (velocityFPS > 1088) { A = 2.0538037167098e-16; M = 5.80410776994789; }
                else if (velocityFPS > 976) { A = 5.92182174254121e-12; M = 4.29275576134191; }
                else if (velocityFPS > 0) { A = 4.3917343795117e-05; M = 1.99978116283334; }
            }

            double retardation = 0.0f;

            if (A != -1 && M != -1 && velocityFPS > 0 && velocityFPS < 100000)
            {
                if (ballisticCoef != 1.0)
                {
                    retardation = A * System.Math.Pow(velocityFPS, M) / ballisticCoef;
                    retardation /= ConversionConstants.s_meterToFeet;
                }
            }

            return retardation;
        }
    }

}
