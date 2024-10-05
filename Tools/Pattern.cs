/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

using System;
using System.IO;


namespace MatrixFFN.Tools
{
    /// <summary>
    /// This class is used to normalize data onto the sigmoid range.
    /// I use [ 0.25, 0.75 ] - this way you still can
    /// correct errors that are missing the field on the outsides.
    /// <para>
    /// This class can be used for any data. But categoric values are senseless
    /// as they have to be placed in an environment - nothing to be
    /// coped with here.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Important is the projection of the data onto the special interval.
    /// </remarks>
    public class Pattern
    {
        /// <summary>
        /// created on: 05.07.2023
        /// <para>
        /// last edit: 05.10.24
        /// </para>
        /// </summary>
        public Version version = new("1.0.11");
        /// <summary>
        /// the 'real' side of the values
        /// </summary>
        public double minReal;
        /// <summary>
        /// the 'real' side of the values
        /// </summary>
        public double maxReal;
        /// <summary>
        /// the 'normalized' side of the values
        /// </summary>
        public double minNorm;
        /// <summary>
        /// the 'normalized' side of the values
        /// </summary>
        public double maxNorm;
        /// <summary>
        /// local ( last ) value - 'real'
        /// </summary>
        public double wertReal;
        /// <summary>
        /// local ( last ) value - 'normalized'
        /// </summary>
        public double wertNorm;
        /// <summary>
        /// count of smallest steps between the limits
        /// </summary>
        public double steps;
        /// <summary>
        /// smallest step for the 'real' side
        /// </summary>
        public double stepDistanceReal;
        /// <summary>
        /// smallest step for the 'normalized' side
        /// </summary>
        public double stepDistanceNorm;
        /// <summary>
        /// the special alpha value is ( 1 / count of steps )
        /// </summary>
        public double learnValue = 1;

        /// <summary>
        /// Constructor gets a example value from the data field 
        /// and the limits for the normalization.
        /// </summary>
        /// <param name="inValue">example value</param>
        /// <param name="inMin">minimal value</param>
        /// <param name="inMax">maximal value</param>
        /// <param name="inSteps">count of wanted steps in the limits</param>
        public Pattern(double inValue, double inMin, double inMax, double inSteps)
        {
            Init(inValue, inMin, inMax, inSteps);

        }   // end: Pattern ( constructor )

        /// <summary>
        /// Constructor that load his data
        /// from the 'BinaryReader' and initializes
        /// himself with them.
        /// </summary>
        /// <param name="reader">a 'BinaryReader'</param>
        public Pattern(BinaryReader reader)
        {
            LoadDataFromReader( reader );

        }   // end: Pattern ( constructor )

        /// <summary>
        /// parameterless constructor
        /// </summary>
        public Pattern()
        {
            wertReal = 0;
            minReal = 0;
            maxReal = 0;
            steps = 1;
            minNorm = 0.25;
            maxNorm = 0.75;

            stepDistanceReal = (maxReal - minReal) / steps;
            stepDistanceNorm = (maxNorm - minNorm) / steps;
            learnValue = 1 / steps;

            double posReal = (wertReal - minReal) / stepDistanceReal;
            wertNorm = minNorm + (posReal * stepDistanceNorm);

        }   // end: Pattern ( constructor )

        /// <summary>
        /// Convert a 'normalized' value into its 'real' twin.
        /// </summary>
        /// <param name="inValue">normalized value to convert</param>
        /// <returns>the  'real' value</returns>
        public double GetRealValue(double inValue)
        {
            double realWertTemp = 0;

            double posNorm = (inValue - minNorm) / stepDistanceNorm;
            realWertTemp = minReal + (posNorm * stepDistanceReal);

            wertNorm = inValue;
            wertReal = realWertTemp;
            return (realWertTemp);

        }   // end: GetRealValue

        /// <summary>
        /// To normalize a number.
        /// </summary>
        /// <param name="inValue">'real' value'</param>
        /// <returns>converted twin</returns>
        public double GetNormedValue(double inValue)
        {
            double normWertTemp = 0;

            double posReal = (inValue - minReal) / stepDistanceReal;
            normWertTemp = minNorm + (posReal * stepDistanceNorm);

            wertReal = inValue;
            wertNorm = normWertTemp;
            return (normWertTemp);

        }   // end: GetNormedValue

        /// <summary>
        /// last value twins to string
        /// </summary>
        /// <returns>the message string</returns>
        override
        public string ToString()
        {
            string text = $"Coding of double number {wertReal} is {wertNorm}.";
            return (text);

        }   // end: ToString

        /// <summary>
        /// Delivers the complete info string.
        /// </summary>
        /// <returns>info string</returns>
        public string InfoString()
        {
            string text = $"MinReal: {minReal}, MaxReal: {maxReal}\n";
            text += $"MinNorm: {minNorm}, MaxNorm: {maxNorm}\n";
            text += $"Steps: {steps}\n";
            text += $"Learning value: {learnValue}\n";
            text += "Example: " + ToString();
            return (text);

        }   // end: InfoString


        /// <summary>
        /// identifies the class
        /// </summary>
        /// <returns>class name</returns>
        public string TypeOf()
        {
            return ("Pattern");

        }   // end: TypeOf

        /// <summary>
        /// a traditional binary save routine 
        /// </summary>
        /// <param name="writer">'BinaryWriter'</param>
        public void SaveDataToWriter(BinaryWriter writer)
        {
            writer.Write(wertReal);
            writer.Write(minReal);
            writer.Write(maxReal);
            writer.Write(steps);

        }   // end: SaveDataToWriter

        /// <summary>
        /// a traditional binary load routine
        /// </summary>
        /// <param name="reader">'BinaryReader'</param>
        public void LoadDataFromReader(BinaryReader reader)
        {
            wertReal = reader.ReadDouble();
            minReal = reader.ReadDouble();
            maxReal = reader.ReadDouble();
            steps = reader.ReadDouble();
            Init(wertReal, minReal, maxReal, steps);

        }   // end: LoadDataFromReader

        /// <summary>
        /// Calculates from the boundary values the internal
        /// normalization coding.
        /// </summary>
        /// <param name="inValue">example value</param>
        /// <param name="inMin">lower limit</param>
        /// <param name="inMax">higher limit</param>
        /// <param name="inSteps">wanted number of steps in between</param>
        public void Init(double inValue, double inMin, double inMax, double inSteps)
        {
            wertReal = inValue;
            minReal = inMin;
            maxReal = inMax;
            steps = inSteps;
            minNorm = 0.25;
            maxNorm = 0.75;

            stepDistanceReal = (maxReal - minReal) / steps;
            stepDistanceNorm = (maxNorm - minNorm) / steps;
            learnValue = 1 / steps;

            double posReal = (wertReal - minReal) / stepDistanceReal;
            wertNorm = minNorm + (posReal * stepDistanceNorm);

        }   // end: Init


    }   // end: class Pattern

}   // end: namespace MatrixFFN.Tools

