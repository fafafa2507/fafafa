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

namespace IE.RSB
{
    /// <summary>
    /// RSB offers settings in both metric & imperial units, these constants are used to convert between back & forth internally.
    /// </summary>
    public static class ConversionConstants
    {
        public static float s_gramToPound = 0.00220462f;
        public static float s_kgM3ToPoundF3 = 0.062428f;
        public static float s_meterToFeet = 3.2808398950131f;
        public static float s_meterToYard = 1.09361f;
        public static float s_kmhToMph = 0.621371f;
        public static float s_kmhToFts = 0.911344f;
        public static float s_kmhToMS = 0.277778f;
        public static float s_gramToGrain = 15.4324f;
        public static float s_mmToInch = 0.0393701f;
        public static float s_pascalToInchMercury = 0.2953f;
        public static float s_joulesToFootPound = 0.737562f;

        public static float FahrenheitToCelcius(float F)
        {
            return (F - 32.0f) * 5.0f / 9.0f;
        }

        public static float CelciusToFahrenheit(float C)
        {
            return (C * 9.0f / 5.0f) + 32.0f;
        }
    }
}
