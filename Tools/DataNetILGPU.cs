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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MatrixFFN.Tools
{
    /// <summary>
    /// The class 'DataNet' normalizes real data forwards and back.
    /// I use [ 0.25, 0.75 ] as range towards the sigmoid function
    /// and that way error correction
    /// can work on any mistake.
    /// </summary>
    public class DataNetILGPU
    {
        /// <summary>
        /// created on: 06.07.2023
        /// last edit: 02.10.24
        /// </summary>
        public Version version = new("1.0.12");
        /// <summary>
        /// one pattern for every feature ( node )
        /// </summary>
        public List<Pattern> data;
        /// <summary>
        /// real values
        /// </summary>
        public List<double> valuesReal;
        /// <summary>
        /// normalized value
        /// </summary>
        public List<double> valuesNormed;
        /// <summary>
        /// stores every learn value from the patterns
        /// </summary>
        public List<double> valuesAlpha;
        /// <summary>
        /// positions name: 'input' or 'output'
        /// </summary>
        public string name = "input";

        /// <summary>
        /// constructor
        /// </summary>
        public DataNetILGPU( string inName = "input" )
        {
            data = new List<Pattern>();
            valuesReal = new List<double>();
            valuesNormed = new List<double>();
            valuesAlpha = new List<double>();

            if ( !inName.Equals( "input" ) )
                name = "output";

        }   // end: DataNet ( constructor )

        /// <summary>
        /// constructor that loads itself from a save file
        /// </summary>
        /// <param name="reader">given 'BinaryReader'</param>
        public DataNetILGPU( BinaryReader reader )
        {
            data = new List<Pattern>();
            valuesReal = new List<double>();
            valuesNormed = new List<double>();
            valuesAlpha = new List<double>();

            LoadDataFromReader( reader );

        }   // end: DataNet ( constructor ) 

        /// <summary>
        /// calculates from the given data the parameters
        /// for the 'DataNet' ( normalization boundaries )
        /// </summary>
        /// <param name="dataArray">dataset for evaluation</param>
        public MatrixILGPU DataNetInit( double[][] dataArray )
        {

            Clear();

            int batchSize = dataArray.Length;
            int featureSize = dataArray[0].Length;
            MatrixILGPU lernRateT = new(1, featureSize, 0);

            // for every feature it will be calculated...
            for ( int feature = 0; feature < featureSize; feature++ )
            {
                // finding the limits

                double tempMin = ( from feld in dataArray select feld[ feature ] ).Min();
                double tempMax = ( from feld in dataArray select feld[ feature ] ).Max();
                double tempAbstand = (from feld1 in dataArray
                                      from feld2 in dataArray
                                      where Math.Abs(feld1[feature] - feld2[feature] ) > 0.0
                                      select Math.Abs(feld1[feature] - feld2[feature]) ).Min();

                double tempSchritte = (tempMax - tempMin) / tempAbstand;

                // keep the results

                Pattern muster = new( dataArray[0][ feature ],
                    tempMin, tempMax, tempSchritte);
                Add( muster );
                lernRateT.data[ 0, feature ] = muster.learnValue;

            }

            return ( lernRateT );

        }   // end: DataNetInit

        /// <summary>
        /// To make the 'ToString' pretty there is a name for the 'DataNet'.
        /// </summary>
        /// <param name="inName">input or output layer</param>
        public void SetName( string inName = "input" )
        {
            if ( !inName.Equals( "input" ) )
                name = "output";

        }   // end: SetName

        /// <summary>
        /// resets the data lists
        /// </summary>
        public void Clear( )
        {
            data.Clear();
            valuesAlpha.Clear();
            valuesNormed.Clear();
            valuesReal.Clear();

        }   // end: Clear

        /// <summary>
        /// Adds a 'Pattern' to the list.
        /// </summary>
        /// <param name="inPattern">to add 'Pattern'</param>
        public void Add( Pattern inPattern )
        {
            data.Add( inPattern );

        }   // end: Add

        /// <summary>
        /// String representation of the class.
        /// </summary>
        /// <returns>description of the 'DataNet'</returns>
        override
        public string ToString( )
        {
            string text = $"Position of the 'DataNet': {name}\n";
            if ( data.Count > 0 )
            {
                foreach ( var pattern in data )
                {
                    text += pattern.ToString() + "\n";

                }

            }
            else
                text += "No data yet.\n";
            return ( text );

        }   // end: ToString

        /// <summary>
        /// Gives the size of the data list.
        /// </summary>
        /// <returns>the size</returns>
        public int Size( )
        {
            return data.Count;

        }   // end: Size

        /// <summary>
        /// Delivers the special learn values derived from
        /// the variance of the 'Pattern'. You use it for
        /// the en-valued learn rate for each feature from
        /// input and output layer.
        /// </summary>
        /// <returns>the alpha list</returns>
        public List<double> GetValuesAlpha( )
        {
            if ( data.Count < 1 )
                throw new ArgumentException(
                    "DatenNetz.DatenAlpha: data list is empty, abort!",
                    "( data.Count < 1 )" );

            valuesAlpha.Clear();

            for ( int pos = 0; pos < data.Count; pos++ )
            {
                Pattern pattern = data[ pos ];
                valuesAlpha.Add( pattern.learnValue );
            }

            return ( valuesAlpha );

        }   // end: GetValuesAlpha

        /// <summary>
        /// a traditional binary save routine
        /// </summary>
        /// <param name="writer">given 'BinaryWriter'</param>
        public void SaveDataToWriter( BinaryWriter writer )
        {
            writer.Write( name );
            writer.Write( data.Count );
            for ( int pos = 0; pos < data.Count; pos++ )
                data[ pos ].SaveDataToWriter( writer );

        }   // end: SaveDataToWriter

        /// <summary>
        /// a traditional binary load routine
        /// </summary>
        /// <param name="reader">given 'BinaryReader'</param>
        public void LoadDataFromReader( BinaryReader reader )
        {
            name = reader.ReadString();
            data.Clear();
            int no = reader.ReadInt32();
            for ( int pos = 0; pos < no; pos++ )
                data.Add( new Pattern( reader ) );

        }   // end: LoadDataFromReader

    }   // end: class DataNetILGPU

}   // end: namespace MatrixFFN.Tools

