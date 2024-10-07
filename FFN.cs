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


// Ignore Spelling: FFN Nums

using MatrixFFN.Tools;
using NPOI.SS.Formula.Functions;
using NPOIwrap;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace MatrixFFN
{
    /// <summary>
    /// FFN = feed-forward-network. A classic sigmoid network with
    /// automatic data normalization.
    /// <para>
    /// I though I will do it all with 'decimal' - bad try. It's still a
    /// construction site and thus i will make it the dirty way
    /// with double.
    /// </para>
    /// </summary>
    public class FFN
    {
        /// <summary>
        /// created on: 08.07.2023
        /// last edit: 07.10.24
        /// </summary>
        public Version version = new("1.0.20");
        /// <summary>
        /// number of the layers.
        /// </summary>
        int layersNo;
        /// <summary>
        /// weight matrices
        /// </summary>
        Matrix[] weights;
        /// <summary>
        /// weight matrices transposed
        /// </summary>
        Matrix[] weightsT;
        /// <summary>
        /// special matrix for the learn rates to deliver the alphas
        /// </summary>
        Matrix[] weightsLearnrates;
        /// <summary>
        /// for the error correction: delta weight matrices
        /// </summary>
        Matrix[] deltaWeights;
        /// <summary>
        /// for the error correction: delta weight matrices transposed
        /// </summary>
        Matrix[] deltaWeightsT;
        /// <summary>
        /// error value in the error correction transposed
        /// </summary>
        Matrix[] layersErrorT;
        /// <summary>
        /// derived activation transposed
        /// </summary>
        Matrix[] gradientT;
        /// <summary>
        /// bias matrix transposed
        /// </summary>
        Matrix[] biasT;
        /// <summary>
        /// target pattern for the error reflection ( transposed )
        /// </summary>
        Matrix targetT;
        /// <summary>
        /// network matrices of the layers used for learning
        /// </summary>
        Matrix[] netLayers;
        /// <summary>
        /// network matrices of the layers used for learning ( transposed )
        /// /// </summary>
        Matrix[] netLayersT;
        /// <summary>
        /// Matrix for the learning rates. Normalization influences
        /// them and this can happen soft in the hidden layers ( transposed ). 
        /// </summary>
        Matrix[] learnRateT;
        /// <summary>
        /// Additional matrix for the faster learning. Is giving the
        /// learn rate of the previous layer ( transposed ).
        /// </summary>
        Matrix[] learnRateTprev;
        /// <summary>
        /// Normalization with the 'DataNet' ?
        /// </summary>
        bool normalizeData;
        /// <summary>
        /// Parameter from the constructor.
        /// <para>
        /// Ex.: 3 layers { 2, 4, 3 }  or 4 layers { 3, 4, 5, 2 }
        /// </para>
        /// <para>
        /// Important: first value is input layer, 
        /// last value is output layer
        /// </para>
        /// </summary>
        public int[] layersTopic;
        /// <summary>
        /// You gain it from 'Fit' and is the sum about all epochs.
        /// </summary>
        public double errorSum;
        /// <summary>
        /// Here the error sum is parted through all epochs.
        /// </summary>
        public double errorMedian;
        /// <summary>
        /// This 'DataNet' normalizes the input data and delivers
        /// feature specific learning rates.
        /// </summary>
        DataNet dNetInput;
        /// <summary>
        /// This 'DataNet' normalizes the output data and delivers
        /// feature specific learning rates.
        /// </summary>
        DataNet dNetOutput;
        /// <summary>
        /// Filename for the file functions.
        /// </summary>
        public string fileName;
        /// <summary>
        /// Counter for the learn epochs.
        /// </summary>
        public long epochsNumber;
        /// <summary>
        /// 'Time-string' from 'stopWatchPredict'.
        /// </summary>
        public string timePredict;
        /// <summary>
        /// Time stopper for 'Predict'
        /// </summary>
        public StopWatch stopWatchPredict;
        /// <summary>
        /// 'Time-string' from 'stopWatchTrain'.
        /// </summary>
        public string timeTrain;
        /// <summary>
        /// Time stopper for 'Train'
        /// </summary>
        public StopWatch stopWatchTrain;
        /// <summary>
        /// 'Time-string' from 'stopWatchFit'.
        /// </summary>
        public string timeFit = "";
        /// <summary>
        /// Time stopper for 'Fit'
        /// </summary>
        public StopWatch stopWatchFit;
        /// <summary>
        /// Error list out of the lifetime of training
        /// ( twin to 'listErrorEpochs' ).
        /// </summary>
        public List<double> listErrorAmount;
        /// <summary>
        /// Epochs list out of the lifetime of training 
        /// ( twin to 'listErrorAmount' ).
        /// </summary>
        public List<long> listEpochs;
        /// <summary>
        /// Font end is 'CanvasTopic' using this string.
        /// </summary>
        public string workingTopic = "";
        /// <summary>
        /// A bool for the 'SetLearningRate'.
        /// </summary>
        bool adaptLernRate = false;
        /// <summary>
        /// Saved scaling factor of the learn rates.
        /// </summary>
        double adaptLernRateOld = 1;
        /// <summary>
        /// can be loaded with 'LoadDataFromExcel()'
        /// </summary>
        public double[][] localInputArrayField;
        /// <summary>
        /// can be loaded with 'LoadDataFromExcel()'
        /// </summary>
        public double[][] localOutputArrayField;
        /// <summary>
        /// normed version of
        /// </summary>
        double[][] localInputArrayFieldNormed;
        /// <summary>
        /// normed version of
        /// </summary>
        double[][] localOutputArrayFieldNormed;
        /// <summary>
        /// input data field for the speed
        /// </summary>
        Matrix[] localNetLayers0;
        /// <summary>
        /// output data field for the speed
        /// </summary>
        Matrix[] localtargetT;
        /// <summary>
        /// NPOI-wrapper to read/write Excel-files
        /// </summary>
        public NPOIexcel myData = new();
        /// <summary>
        /// local data dimension input
        /// </summary>
        public int localIns = 0;
        /// <summary>
        /// local data dimension output
        /// </summary>
        public int localOuts = 0;
        /// <summary>
        /// result text from the Fit-function
        /// </summary>
        public string fitText = "";

        /// <summary>
        /// The constructor is the only way to init a new network. Including
        /// every shape of every matrix used. No shape changing at a different
        /// spot is possible.
        /// <para>
        /// There must be always one hidden layer minimum with this network.
        /// </para>
        /// </summary>
        /// <param name="layersIn">Ex.: layers = { 2, 3, 1 } or = { 3, 15, 7, 2 }</param>
        /// <param name="normalize">New: data normalization by 'DataNet'ing.</param>
        /// <param name="name">filename for the net</param>
        public FFN(int[] layersIn, bool normalize, string name = "FFN.netz")
        {
            // simple initiation
            layersTopic = (int[])layersIn;
            layersNo = layersTopic.Length;
            fileName = (string)name;
            targetT = new Matrix(1, layersTopic[layersNo - 1], 0);
            normalizeData = normalize;
            dNetInput = new DataNet();
            dNetOutput = new DataNet("out");
            epochsNumber = 0;
            errorSum = 0;
            errorMedian = 0;
            timePredict = "";
            timeTrain = "";
            stopWatchPredict = new StopWatch();
            stopWatchTrain = new StopWatch();
            stopWatchFit = new StopWatch();
            localInputArrayField = new double[1][];
            localOutputArrayField = new double[1][];
            localInputArrayFieldNormed = new double[1][];
            localOutputArrayFieldNormed = new double[1][];
            localNetLayers0 = new Matrix[1];
            localtargetT = new Matrix[1];

            ParseTopic();

            // init the in-between layers ( 1 less than layers )
            weights = new Matrix[layersNo - 1];
            weightsT = new Matrix[layersNo - 1];
            weightsLearnrates = new Matrix[layersNo - 1];
            deltaWeights = new Matrix[layersNo - 1];
            deltaWeightsT = new Matrix[layersNo - 1];
            for (int pos = 0; pos < (layersNo - 1); pos++)
            {   // Info: the 'vectors' are transposed
                weights[pos] = new Matrix(
                    layersTopic[pos], layersTopic[pos + 1], -1, 1);
                weightsT[pos] = Matrix.Transpose(weights[pos]);
                weightsLearnrates[pos] = new Matrix(
                    layersTopic[pos], layersTopic[pos + 1], 1);
                deltaWeights[pos] = new Matrix(
                    layersTopic[pos], layersTopic[pos + 1], 0);
                deltaWeightsT[pos] = Matrix.Transpose(deltaWeights[pos]);

            }

            // init the layers
            biasT = new Matrix[layersNo];
            learnRateT = new Matrix[layersNo];
            learnRateTprev = new Matrix[layersNo];
            netLayers = new Matrix[layersNo];
            netLayersT = new Matrix[layersNo];
            layersErrorT = new Matrix[layersNo];
            gradientT = new Matrix[layersNo];
            for (int pos = 0; pos < layersNo; pos++)
            {
                biasT[pos] = new Matrix(
                    1, layersTopic[pos], -1, 1);
                learnRateT[pos] = new Matrix(1, layersTopic[pos], 0.1);
                learnRateTprev[pos] = new Matrix(1, layersTopic[pos], 0.1);
                netLayers[pos] = new Matrix(layersTopic[pos], 1, 0.0);
                netLayersT[pos] = Matrix.Transpose(netLayers[pos]);
                //gradientT[pos] = new Matrix(1, layersTopic[pos], 0);
                gradientT[ pos ] = Matrix.Transpose( netLayers[ pos ] );
                layersErrorT[ pos] = new Matrix(1, layersTopic[pos], 0);

            }
            // lists for errors and epochs
            listEpochs = new List<long>( );
            listErrorAmount = new List<double> ( );

        }   // end: FFN ( constructor )

        /// <summary>
        /// This constructor loads an already saved network.
        /// </summary>
        /// <param name="name">the files name</param>
        public FFN(string name)
        {
            // simple initiation
            layersTopic = new int[ 1 ];
            layersNo = layersTopic.Length;
            fileName = (string)name;
            targetT = new Matrix( 1, layersTopic[ layersNo - 1 ], 0 );
            normalizeData = true;
            dNetInput = new DataNet();
            dNetOutput = new DataNet( "out" );
            epochsNumber = 0;
            errorSum = 0;
            errorMedian = 0;
            timePredict = "";
            timeTrain = "";
            stopWatchPredict = new StopWatch();
            stopWatchTrain = new StopWatch();
            stopWatchFit = new StopWatch();
            localInputArrayField = new double[ 1 ][];
            localOutputArrayField = new double[ 1 ][];
            localInputArrayFieldNormed = new double[ 1 ][];
            localOutputArrayFieldNormed = new double[ 1 ][];
            localNetLayers0 = new Matrix[ 1 ];
            localtargetT = new Matrix[ 1 ];

            ParseTopic();

            // init the in-between layers ( 1 less than layers )
            weights = new Matrix[ layersNo - 1 ];
            weightsT = new Matrix[ layersNo - 1 ];
            weightsLearnrates = new Matrix[ layersNo - 1 ];
            deltaWeights = new Matrix[ layersNo - 1 ];
            deltaWeightsT = new Matrix[ layersNo - 1 ];

            // init the layers
            biasT = new Matrix[ layersNo ];
            learnRateT = new Matrix[ layersNo ];
            learnRateTprev = new Matrix[ layersNo ];
            netLayers = new Matrix[ layersNo ];
            netLayersT = new Matrix[ layersNo ];
            layersErrorT = new Matrix[ layersNo ];
            gradientT = new Matrix[ layersNo ];

            // lists for errors and epochs
            listEpochs = new List<long>();
            listErrorAmount = new List<double>();

            fileName = (string)name;
            LoadData( fileName);

        }   // end: FFN ( constructor )

        // -------------------------------------        output functions

        /// <summary>
        /// standard output of the 'Matrix'.
        /// </summary>
        /// <returns>string representation of the 'Matrix'</returns>
        override
        public string ToString( )
        {
            string meldung = "--------------------------------------------------\n";
            meldung = "Network has the topic of "
                + ArrayToString( layersTopic ) + "\n";
            meldung += "--------------------------------------------------\n";
            return ( meldung );

        }   // end: ToString

        /// <summary>
        /// Gives the 'Matrix' via ToString() to standard output.
        /// </summary>
        public void Print( )
        {
            System.Console.WriteLine( ToString() );

        }   // end: Print

        /// <summary>
        /// Helper function for writing arrays into a string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>the data as 'string'</returns>
        public string ArrayToString( int[] data )
        {
            string text = "";

            text += $" [ {string.Join( ", ", data )} ] ";
            text += "\n";

            return ( text );

        }   // end: ArrayToString

        // -----------------------------------------      SaveData

        /// <summary>
        /// Saves the network ( every variable ). 
        /// <para>Info: you need to train minimum once to have something to save!</para>
        /// </summary>
        /// <param name="name">chosen filename</param>
        public void SaveData(string name)
        {
            fileName = name;
            if (epochsNumber == 0)
                return;
            using var writer = new BinaryWriter( File.OpenWrite( fileName ) );
            writer.Write( biasT.Length );
            for ( int pos = 0; pos < biasT.Length; pos++ )
                biasT[ pos ].SaveDataToWriter( writer );
            writer.Write( fileName );
            writer.Write( deltaWeights.Length );
            for ( int pos = 0; pos < deltaWeights.Length; pos++ )
                deltaWeights[ pos ].SaveDataToWriter( writer );
            writer.Write( deltaWeightsT.Length );
            for ( int pos = 0; pos < deltaWeightsT.Length; pos++ )
                deltaWeightsT[ pos ].SaveDataToWriter( writer );
            dNetOutput.SaveDataToWriter( writer );
            dNetInput.SaveDataToWriter( writer );
            writer.Write( epochsNumber );
            writer.Write( errorMedian );
            writer.Write( errorSum );
            writer.Write( weights.Length );
            for ( int pos = 0; pos < weights.Length; pos++ )
                weights[ pos ].SaveDataToWriter( writer );
            writer.Write( weightsT.Length );
            for ( int pos = 0; pos < weightsT.Length; pos++ )
                weightsT[ pos ].SaveDataToWriter( writer );
            writer.Write( gradientT.Length );
            for ( int pos = 0; pos < gradientT.Length; pos++ )
                gradientT[ pos ].SaveDataToWriter( writer );
            writer.Write( learnRateT.Length );
            for ( int pos = 0; pos < learnRateT.Length; pos++ )
                learnRateT[ pos ].SaveDataToWriter( writer );
            writer.Write( netLayers.Length );
            for ( int pos = 0; pos < netLayers.Length; pos++ )
                netLayers[ pos ].SaveDataToWriter( writer );
            writer.Write( netLayersT.Length );
            for ( int pos = 0; pos < netLayersT.Length; pos++ )
                netLayersT[ pos ].SaveDataToWriter( writer );
            writer.Write( normalizeData );
            writer.Write( layersNo );
            writer.Write( layersTopic.Length );
            for ( int pos = 0; pos < layersTopic.Length; pos++ )
                writer.Write( layersTopic[ pos ] );
            writer.Write( layersErrorT.Length );
            for ( int pos = 0; pos < layersErrorT.Length; pos++ )
                layersErrorT[ pos ].SaveDataToWriter( writer );
            targetT.SaveDataToWriter( writer );
            writer.Write( timePredict );
            writer.Write( timeTrain );
            writer.Write( timeFit );
            writer.Write( workingTopic );
            // the two lists
            writer.Write( listEpochs.Count );
            for ( int pos = 0; pos < listEpochs.Count; pos++ )
                writer.Write( listEpochs[ pos ] );
            writer.Write( listErrorAmount.Count );
            for ( int pos = 0; pos < listErrorAmount.Count; pos++ )
                writer.Write( listErrorAmount[ pos ] );
            writer.Write( adaptLernRate );
            writer.Write( adaptLernRateOld );
            writer.Write( fitText );

        }   // end: SaveData

        // ------------------------------------      LoadData

        /// <summary>
        /// Loads a saved network. You can use it from the constructor or in between
        /// loosing your old network. A complete init will happen to all data fields.
        /// </summary>
        /// <param name="name">filename</param>
        public void LoadData(string name)
        {
            if (!name.Equals(fileName))
                fileName = name;

            using var reader = new BinaryReader( File.OpenRead( fileName ) );
            int tempInt;
            tempInt = reader.ReadInt32( );
            biasT = new Matrix[ tempInt ];
            for ( int pos = 0; pos < biasT.Length; pos++ )
                biasT[ pos ] = new Matrix( reader );
            fileName = reader.ReadString( );
            tempInt = reader.ReadInt32( );
            deltaWeights = new Matrix[ tempInt ];
            for ( int pos = 0; pos < deltaWeights.Length; pos++ )
                deltaWeights[ pos ] = new Matrix( reader );
            tempInt = reader.ReadInt32( );
            deltaWeightsT = new Matrix[ tempInt ];
            for ( int pos = 0; pos < deltaWeightsT.Length; pos++ )
                deltaWeightsT[ pos ] = new Matrix( reader );
            dNetOutput = new DataNet( reader );
            dNetInput = new DataNet( reader );
            epochsNumber = reader.ReadInt64( );
            errorMedian = reader.ReadInt64( );
            errorSum = reader.ReadInt64( );
            tempInt = reader.ReadInt32( );
            weights = new Matrix[ tempInt ];
            for ( int pos = 0; pos < weights.Length; pos++ )
                weights[ pos ] = new Matrix( reader );
            tempInt = reader.ReadInt32( );
            weightsT = new Matrix[ tempInt ];
            for ( int pos = 0; pos < weightsT.Length; pos++ )
                weightsT[ pos ] = new Matrix( reader );
            tempInt = reader.ReadInt32( );
            gradientT = new Matrix[ tempInt ];
            for ( int pos = 0; pos < gradientT.Length; pos++ )
                gradientT[ pos ] = new Matrix( reader );
            tempInt = reader.ReadInt32( );
            learnRateT = new Matrix[ tempInt ];
            for ( int pos = 0; pos < tempInt; pos++ )
                learnRateT[ pos ] = new Matrix( reader );
            tempInt = reader.ReadInt32( );
            netLayers = new Matrix[ tempInt ];
            for ( int pos = 0; pos < netLayers.Length; pos++ )
                netLayers[ pos ] = new Matrix( reader );
            tempInt = reader.ReadInt32( );
            netLayersT = new Matrix[ tempInt ];
            for ( int pos = 0; pos < netLayersT.Length; pos++ )
                netLayersT[ pos ] = new Matrix( reader );
            normalizeData = reader.ReadBoolean( );
            layersNo = reader.ReadInt32( );
            tempInt = reader.ReadInt32( );
            layersTopic = new int[ tempInt ];
            for ( int pos = 0; pos < layersTopic.Length; pos++ )
                layersTopic[ pos ] = reader.ReadInt32( );
            tempInt = reader.ReadInt32( );
            layersErrorT = new Matrix[ tempInt ];
            for ( int pos = 0; pos < layersErrorT.Length; pos++ )
                layersErrorT[ pos ] = new Matrix( reader );
            targetT = new Matrix( reader );
            timePredict = reader.ReadString( );
            timeTrain = reader.ReadString( );
            timeFit = reader.ReadString( );
            workingTopic = reader.ReadString( );
            // the two lists
            tempInt = reader.ReadInt32( );
            listEpochs = new List<long>( );
            if ( tempInt > 0 )
                for ( int pos = 0; pos < tempInt; pos++ )
                    listEpochs.Add( reader.ReadInt64( ) );
            tempInt = reader.ReadInt32( );
            listErrorAmount = new List<double>( );
            if ( tempInt > 0 )
                for ( int pos = 0; pos < tempInt; pos++ )
                    listErrorAmount.Add( reader.ReadDouble( ) );

            stopWatchPredict = new StopWatch( );
            stopWatchTrain = new StopWatch( );
            stopWatchFit = new StopWatch( );
            adaptLernRate = reader.ReadBoolean( );
            adaptLernRateOld = reader.ReadDouble( );
            fitText = reader.ReadString( );
            // end: using

        }   // end: LoadData

        // ----------------------------       NORMALIZATIONS

        /// <summary>
        /// Delivers via the 'DataNet' ( input ) the converted values.
        /// </summary>
        /// <param name="realData">field input numbers ( whole layer )</param>
        /// <returns>field of normalized values</returns>
        public double[] NormInputArray(double[] realData)
        {
            if ( realData.Length != layersTopic[ 0 ] )
                throw new ArgumentException(    
                    "FFN.NormInputArray: data has to fit to the network layer, Abort!",
                        "( realData.Length != layersTopic[ 0 ] )" );

            double[] doubles = new double[realData.Length];
            for (int pos = 0; pos < realData.Length; pos++)
            {
                Pattern pattern = (Pattern)dNetInput.data[pos];
                doubles[pos] =
                    pattern.GetNormedValue(realData[pos]);

            }
            return (doubles);

        }   // end: NormInputArray

        /// <summary>
        /// Delivers via the 'DataNet' ( output ) the converted values.
        /// </summary>
        /// <param name="realData">field output numbers ( whole layer )</param>
        /// <returns>field of normalized values</returns>
        public double[] NormOutputArray(double[] realData)
        {
            if ( realData.Length != layersTopic[ layersNo - 1 ] )
                throw new ArgumentException(
                    "FFN.NormOutputArray: data has to fit to the network layer, Abort!",
                        "( realData.Length != layersTopic[ layersNo - 1 ] )" );

            double[] doubles = new double[realData.Length];
            for (int pos = 0; pos < realData.Length; pos++)
            {
                Pattern pattern = (Pattern)dNetOutput.data[pos];
                doubles[pos] =
                    pattern.GetNormedValue(realData[pos]);

            }
            return (doubles);

        }   // end: NormOutputArray

        /// <summary>
        /// Delivers via the 'DataNet' ( input ) the converted numbers.
        /// </summary>
        /// <param name="normedData">field of numbers ( whole layer )</param>
        /// <returns>field with denormalized values</returns>
        public double[] DeNormInputArray(double[] normedData)
        {
            if ( normedData.Length != layersTopic[ 0 ] )
                throw new ArgumentException(
                    "FFN.DeNormInputArray: data has to fit to the network layer, Abort!",
                        "( normedData.Length != layersTopic[ 0 ] )" );

            double[] doubles = new double[normedData.Length];
            for (int pos = 0; pos < normedData.Length; pos++)
            {
                Pattern pattern = (Pattern)dNetInput.data[pos];
                doubles[pos] =
                    pattern.GetRealValue(normedData[pos]);

            }
            return (doubles);

        }   // end: DeNormInputArray

        /// <summary>
        /// Delivers via the 'DataNet' ( output ) the converted numbers.
        /// </summary>
        /// <param name="normedData">field of numbers ( whole layer )</param>
        /// <returns>field with denormalised values</returns>
        public double[] DeNormOutputArray(double[] normedData)
        {
            if ( normedData.Length != layersTopic[ layersNo - 1 ] )
                throw new ArgumentException(
                    "FFN.DeNormOutputArray: data has to fit to the network layer, Abort!",
                        "( normedData.Length != layersTopic[ layersNo - 1 ] )" );

            double[] doubles = new double[normedData.Length];
            for (int pos = 0; pos < normedData.Length; pos++)
            {
                Pattern pattern = (Pattern)dNetOutput.data[pos];
                doubles[pos] =
                    pattern.GetRealValue(normedData[pos]);

            }
            return (doubles);

        }   // end: DeNormOutputArray

        // -------------------------------       Predict

        /// <summary>
        /// This function makes a 'feed forward predict' 
        /// on the given data.
        /// <para>Normalized stays normalized. 
        /// The flag 'normalizeData' will take care of that.</para>
        /// </summary>
        /// <param name="inputLayer">input data into the net</param>
        /// <returns>predicted output values</returns>
        public double[] Predict(double[] inputLayer)
        {
            if ( inputLayer.Length != layersTopic[ 0 ] )
                throw new ArgumentException(
                    "FFN.Predict: data has to fit to the network layer, Abort!",
                        "( inputLayer.Length != layersTopic[ 0 ] )" );

            stopWatchPredict.Start();
            double[] inputData = (double[])inputLayer;
            if (normalizeData)
                inputData = NormInputArray(inputData);
            Matrix[] layersT = new Matrix[layersNo];
            layersT[0] =
                Matrix.FromArrayTranspose(inputData);
            // info: the layers are not initialized on a 'Predict'
            for (int pos = 0; pos < (layersT.Length - 1); pos++)
            {
                layersT[pos + 1] =
                    Matrix.Multiply(weights[pos], layersT[pos]);
                layersT[pos + 1].AddMatrix(biasT[pos + 1]);
                layersT[pos + 1].ToSigmoid();

            }
            double[] outputLayer = layersT[layersT.Length - 1].ToArray();
            if (normalizeData)
                outputLayer = DeNormOutputArray(outputLayer);
            stopWatchPredict.Stop();
            timePredict = stopWatchPredict.ToString();

            return (outputLayer);

        }   // end: Predict

        /// <summary>
        /// This function makes a 'feed forward predict' 
        /// on the given local data.
        /// <para>Normalized stays normalized. 
        /// The flag 'normalizeData' will take care of that.</para>
        /// </summary>
        /// <param name="patternNumber">local input data into the net</param>
        /// <returns>predicted output values</returns>
        public double[] Predict_LocalData( int patternNumber )
        {
            if ( localInputArrayField[ patternNumber ].Length != layersTopic[ 0 ] )
                throw new ArgumentException(
                    "FFN.Predict_LocalData: data has to fit to the network layer, Abort!",
                        "( localInputArrayField[ patternNumber ].Length != layersTopic[ 0 ] )" );

            stopWatchPredict.Start();
            double[] inputData = (double[])localInputArrayField[ patternNumber ];
            if ( normalizeData )
                inputData = NormInputArray( inputData );
            Matrix[] layersT = new Matrix[layersNo];
            layersT[ 0 ] =
                Matrix.FromArrayTranspose( inputData );
            // info: the layers are not initialized on a 'Predict'
            for ( int pos = 0; pos < ( layersT.Length - 1 ); pos++ )
            {
                layersT[ pos + 1 ] =
                    Matrix.Multiply( weights[ pos ], layersT[ pos ] );
                layersT[ pos + 1 ].AddMatrix( biasT[ pos + 1 ] );
                layersT[ pos + 1 ].ToSigmoid();

            }
            double[] outputLayer = layersT[layersT.Length - 1].ToArray();
            if ( normalizeData )
                outputLayer = DeNormOutputArray( outputLayer );
            stopWatchPredict.Stop();
            timePredict = stopWatchPredict.ToString();

            return ( outputLayer );

        }   // end: Predict_LocalData

        // -----------------------------------     Train

        /// <summary>
        /// Trains the network with the given input/output pattern.
        /// <para>The 'mean square sum'-error is used.</para>
        /// </summary>
        /// <param name="inputLayer">input layer</param>
        /// <param name="outputLayer">output layer</param>
        /// <returns>delivers the error value</returns>
        public double Train(double[] inputLayer, double[] outputLayer)
        {
            if ( ( inputLayer.Length != layersTopic[ 0 ] )
                || ( outputLayer.Length != layersTopic[ layersNo -1 ] ) )
                throw new ArgumentException(
                    "FFN.Train: data has to fit to the network layers, Abort!",
                        "( ( inputLayer.Length != layersTopic[ 0 ] )" + 
                        "\r\n|| ( outputLayer.Length != layersTopic[ layersNo -1 ] ) )" );

            stopWatchTrain.Start();
            double[] inputData = (double[])inputLayer;
            double[] outputData = (double[])outputLayer;
            if (normalizeData)
            {
                inputData = NormInputArray(inputData);
                outputData = NormOutputArray(outputData);
            }
            // forward the data into the input layer
            netLayers[0] = Matrix.FromArray(inputData);
            Matrix.Transpose(netLayers[0], netLayersT[0]);
            // now the forward step as loop over the layers starts
            for (int pos = 0; pos < (layersNo - 1); pos++)
            {   // from here we can target for the speed
                Matrix.Multiply( weights[ pos ], netLayersT[ pos ], 
                    netLayersT[ pos + 1 ] );
                netLayersT[pos + 1].AddMatrix(biasT[pos + 1]);
                netLayersT[pos + 1].ToSigmoid();
                Matrix.Transpose(netLayersT[pos + 1], netLayers[pos + 1]);

            }
            // the output data for the error correction
            targetT = Matrix.FromArrayTranspose(outputData);

            // first loop is writing the errors into 'gradientT'
            for (int pos = (layersNo - 1); pos > 0; pos--)
            {   // here again target for the speed
                if (pos == (layersNo - 1))
                    Matrix.SubtractMatrix( targetT, netLayersT[ pos ], 
                        layersErrorT[ pos ] );
                else
                    Matrix.Multiply( weightsT[ pos ], layersErrorT[ pos + 1 ], 
                        layersErrorT[ pos ] );
                // 'gradientT' will have it all
                Matrix.DeriveSigmoid( netLayersT[ pos ], gradientT[ pos ] );
                gradientT[pos].MultiplySameSize(layersErrorT[pos]);
                // concerning 'DataNet': it is changing the learn rates and is
                // coming evaluated from both sides ( input/output layer )
                // towards the hidden layers
                gradientT[ pos].MultiplySameSize(learnRateT[pos]);
                if (normalizeData)
                    gradientT[pos].MultiplySameSize(learnRateTprev[pos]);

            }
            // now the errors will be corrected
            for (int pos = (layersNo - 1); pos > 0; pos--)
            {
                Matrix.Multiply(gradientT[pos], netLayers[pos - 1], 
                    deltaWeights[ pos - 1 ] );
                weights[pos - 1].AddMatrix(deltaWeights[pos - 1]);
                Matrix.Transpose(weights[pos - 1], weightsT[pos - 1]);
                // a detail to question: Multiply/Add ?
                biasT[pos].MultiplySameSize(gradientT[pos]);

            }
            double errorSum = layersErrorT[layersNo - 1].MS_Sum();
            stopWatchTrain.Stop();
            timeTrain = stopWatchTrain.ToString();

            return (errorSum);

        }   // end: Train

        /// <summary>
        /// Trains the network with the given input/output pattern.
        /// This version works on the loaded data ( from Excel ).
        /// <para>The 'mean square sum'-error is used.</para>
        /// </summary>
        /// <param name="sampleN">number of the local data row</param>
        /// <returns>delivers the error value</returns>
        public double Train_LocalData( int sampleN )
        {
            if ( ( localInputArrayField[ sampleN ].Length != layersTopic[ 0 ] )
                || ( localOutputArrayField[ sampleN ].Length != layersTopic[ layersNo - 1 ] ) )
                throw new ArgumentException(
                    "FFN.Train_LocalData: data has to fit to the network layers, Abort!",
                        "( ( localInputArrayField[ sampleN ].Length != layersTopic[ 0 ] )" +
                        "\r\n|| ( localOutputArrayField[ sampleN ].Length != layersTopic[ layersNo -1 ] ) )" );

            stopWatchTrain.Start();

            // forward the data into the input layer
            // netLayers[ 0 ] = (Matrix)localNetLayers0[ sampleN ];
            // no clone needed for the speed
            Matrix.Transpose( localNetLayers0[ sampleN ], netLayersT[ 0 ] );
            // now the forward step as loop over the layers starts
            for ( int pos = 0; pos < ( layersNo - 1 ); pos++ )
            {   // from here we can target for the speed
                Matrix.Multiply( weights[ pos ], netLayersT[ pos ], 
                    netLayersT[ pos + 1 ] );
                netLayersT[ pos + 1 ].AddMatrix( biasT[ pos + 1 ] );
                netLayersT[ pos + 1 ].ToSigmoid();
                Matrix.Transpose( netLayersT[ pos + 1 ], netLayers[ pos + 1 ] );

            }
            // the output data for the error correction
            // targetT = (Matrix)localtargetT[ sampleN ];
            // no clone needed for the speed

            // first loop is writing the errors into 'gradientT'
            for ( int pos = ( layersNo - 1 ); pos > 0; pos-- )
            {   // here again target for the speed
                if ( pos == ( layersNo - 1 ) )
                    Matrix.SubtractMatrix( localtargetT[ sampleN ], netLayersT[ pos ],
                        layersErrorT[ pos ] );
                else
                    Matrix.Multiply( weightsT[ pos ], layersErrorT[ pos + 1 ],
                        layersErrorT[ pos ] );
                // 'gradientT' will have it all
                Matrix.DeriveSigmoid( netLayersT[ pos ], gradientT[ pos ] );
                gradientT[ pos ].MultiplySameSize( layersErrorT[ pos ] );
                // concerning 'DataNet': it is changing the learn rates and is
                // coming evaluated from both sides ( input/output layer )
                // towards the hidden layers
                gradientT[ pos ].MultiplySameSize( learnRateT[ pos ] );
                if ( normalizeData )
                    gradientT[ pos ].MultiplySameSize( learnRateTprev[ pos ] );

            }
            // now the errors will be corrected
            for ( int pos = ( layersNo - 1 ); pos > 0; pos-- )
            {
                Matrix.Multiply( gradientT[ pos ], netLayers[ pos - 1 ],
                    deltaWeights[ pos - 1 ] );
                weights[ pos - 1 ].AddMatrix( deltaWeights[ pos - 1 ] );
                Matrix.Transpose( weights[ pos - 1 ], weightsT[ pos - 1 ] );
                // a detail to question: Multiply/Add ?
                biasT[ pos ].MultiplySameSize( gradientT[ pos ] );

            }
            double errorSum = layersErrorT[layersNo - 1].MS_Sum();
            stopWatchTrain.Stop();
            timeTrain = stopWatchTrain.ToString();

            return ( errorSum );

        }   // end: Train_LocalData

        // -------------------------------------     Fit

        /// <summary>
        /// Trains full epochs ( complete datasets ) and presents every data pair once to
        /// 'Train' randomly.
        /// <para>
        /// Normalizes the data automatic ( recommended ).
        /// </para>
        /// </summary>
        /// <param name="inputArrayField">input layers</param>
        /// <param name="outputArrayField">output layers</param>
        /// <param name="epochsIn">epochs to train</param>
        /// <returns>result of the training</returns>
        public string Fit(double[][] inputArrayField,
            double[][] outputArrayField, long epochsIn)
        {
            bool okData = true;
            foreach ( double[] inputArray in inputArrayField )
                okData &= ( inputArray.Length == layersTopic[ 0 ] );
            foreach ( double[] outputArray in outputArrayField )
                okData &= ( outputArray.Length == layersTopic[ layersNo - 1 ] );
            if ( !okData )
                throw new ArgumentException(
                    "FFN.Train: data has to fit to the network layers, Abort!",
                        "( 'any' ( inputLayer.Length != layersTopic[ 0 ] )" +
                        "\r\n|| 'any' ( outputLayer.Length != layersTopic[ layersNo -1 ] ) )" );

            stopWatchFit.Start();

            if ((normalizeData) && (epochsNumber == 0))
                DataNetInit(inputArrayField, outputArrayField);
            string text = "";
            errorSum = 0;

            Random zufall = new();
            for (int epochsLoop = 0; epochsLoop < epochsIn; epochsLoop++)
            {
                List<int> drawPool = new();
                for (int drawPoolIndex = 0; drawPoolIndex < inputArrayField.Length; drawPoolIndex++)
                {
                    drawPool.Add(drawPoolIndex);

                }
                for (int drawPoolIndex = 0; drawPoolIndex < drawPool.Count; drawPoolIndex++)
                {
                    int pos = zufall.Next(0, drawPool.Count);
                    int sampleN = drawPool[pos];
                    drawPool.RemoveAt(pos);
                    errorSum += Train(
                        inputArrayField[sampleN], outputArrayField[sampleN]);

                }
                epochsNumber++;

            }
            stopWatchFit.Stop( );
            timeFit = stopWatchFit.ToString( );
            errorMedian = errorSum / epochsIn;
            text = $"Fit for {epochsIn} epochs complete:\n"
                + $"epochs sum: {epochsNumber.ToString()}\n"
                + $"error sum: \t\t\t{errorSum.ToString()}\n"
                + $"medium error: \t\t{errorMedian.ToString()}\n"
                + $"error sum / epochs: \t\t{(errorSum / epochsIn).ToString()}\n"
                + $"medium error / epochs: \t{(errorMedian / epochsIn).ToString()}\n"
                + $"duration of this 'Fit': {timeFit}.\n"
                + "------------------------------------\n";
            // fill the lists
            listEpochs.Add( epochsNumber );
            listErrorAmount.Add( errorMedian );
            fitText = text;

            return (text);

        }   // end: Fit

        /// <summary>
        /// Trains full epochs ( complete datasets ) and presents every data pair once to
        /// 'Train_LocalData' randomly.
        /// <para>Uses the dataset loaded with 'LoadDataFromExcel()'</para>
        /// <para>
        /// Normalizes the data automatic ( recommended ).
        /// </para>
        /// </summary>
        /// <param name="epochsIn">epochs to train</param>
        /// <returns>result of the training</returns>
        public string Fit_LocalData( long epochsIn )
        {
            bool okData = true;
            foreach ( double[] localInputArray in localInputArrayField )
                okData &= ( localInputArray.Length == layersTopic[ 0 ] );
            foreach ( double[] localOutputArray in localOutputArrayField )
                okData &= ( localOutputArray.Length == layersTopic[ layersNo - 1 ] );
            if ( !okData )
                throw new ArgumentException(
                    "FFN.Train_LocalData: data has to fit to the network layers, Abort!",
                        "( 'any' ( localInputArray[0].Length != layersTopic[ 0 ] )" +
                        "\r\n|| 'any' ( localOutputArray[0].Length != layersTopic[ layersNo -1 ] ) )" );

            stopWatchFit.Start();
            InitLocalData();        // the special call to 'DataNetInit'
            string text = "";
            errorSum = 0;

            Random zufall = new();
            for ( int epochsLoop = 0; epochsLoop < epochsIn; epochsLoop++ )
            {
                List<int> drawPool = new();
                for ( int drawPoolIndex = 0; drawPoolIndex < localInputArrayField.Length; 
                        drawPoolIndex++ )
                {
                    drawPool.Add( drawPoolIndex );

                }
                for ( int drawPoolIndex = 0; drawPoolIndex < drawPool.Count; 
                        drawPoolIndex++ )
                {
                    int pos = zufall.Next(0, drawPool.Count);
                    int sampleN = drawPool[pos];
                    drawPool.RemoveAt( pos );
                    errorSum += Train_LocalData( sampleN );

                }
                epochsNumber++;

            }
            stopWatchFit.Stop();
            timeFit = stopWatchFit.ToString();
            errorMedian = errorSum / epochsIn;
            text = $"Fit for {epochsIn} epochs complete:\n"
                + $"epochs sum: {epochsNumber.ToString()}\n"
                + $"error sum: \t\t\t{errorSum.ToString()}\n"
                + $"medium error: \t\t{errorMedian.ToString()}\n"
                + $"error sum / epochs: \t\t{( errorSum / epochsIn ).ToString()}\n"
                + $"medium error / epochs: \t{( errorMedian / epochsIn ).ToString()}\n"
                + $"duration of this 'Fit': {timeFit}.\n"
                + "------------------------------------\n";
            // fill the lists
            listEpochs.Add( epochsNumber );
            listErrorAmount.Add( errorMedian );
            fitText = text;

            return ( text );

        }   // end: Fit_LocalData

        // -------------------------------------      DataNetInit

        /// <summary>
        /// Here the given data will be analyzed to init
        /// the 'DataNet's.
        /// </summary>
        /// <param name="dataInputArray">the input dataset</param>
        /// <param name="dataOutputArray">the output dataset</param>
        public void DataNetInit(double[][] dataInputArray,
                double[][] dataOutputArray)
        {
            bool okData = true;
            foreach ( double[] inputArray in dataInputArray )
                okData &= ( inputArray.Length == layersTopic[ 0 ] );
            foreach ( double[] outputArray in dataOutputArray )
                okData &= ( outputArray.Length == layersTopic[ layersNo - 1 ] );
            if ( !okData )
                throw new ArgumentException(
                    "FFN.DataNetInit: data has to fit to the network layers, Abort!",
                        "( 'any' ( inputArray.Length != layersTopic[ 0 ] )" +
                        "\r\n|| 'any' ( outputArray.Length != layersTopic[ layersNo -1 ] ) )" );

            learnRateT[ 0] = dNetInput.DataNetInit( dataInputArray );
            learnRateT[(layersNo - 1)] = dNetOutput.DataNetInit( dataOutputArray );

            // learning values field is written for the speed
            for (int pos = 0; pos < (learnRateT.Length - 1); pos++)
            {
                learnRateTprev[pos + 1] =
                    Matrix.Multiply(weightsLearnrates[pos], learnRateT[pos]);

            }

        }   // end: DataNetInit

        /// <summary>
        /// Introduces a flexible way to add/change learning
        /// values manually or even algorithmically. A new value leads
        /// to the removal of the old one and the coming active
        /// of the new one. It's always additional to internal management.
        /// <para>
        /// Possible use could be the adaption via the nets error. Program start
        /// takes a 1 as init. Value bigger than 1 is bigger correction and vice versa.
        /// </para>
        /// </summary>
        /// <param name="target">wanted manual learning value</param>
        public void SetLearningRate(double target)
        {

            if ( adaptLernRate )
            {
                if (target != adaptLernRateOld)
                {
                    // learning rate field for the speed reset
                    for (int pos = 0; pos < (learnRateT.Length - 1); pos++)
                    {
                        Matrix.MultiplyScalar(weightsLearnrates[pos], 
                            ( 1 / adaptLernRateOld ), learnRateTprev[ pos + 1 ] );

                    }
                    // learning rate field for the speed set
                   for ( int pos = 0; pos < (learnRateT.Length - 1); pos++)
                    {
                        Matrix.MultiplyScalar(weightsLearnrates[pos], 
                            target, learnRateTprev[ pos + 1 ] );

                    }
                    adaptLernRateOld = target;

                }
                else return;

            }
            else
            {
                // learning rate field for the speed set
                for ( int pos = 0; pos < (learnRateT.Length - 1); pos++)
                {
                    Matrix.MultiplyScalar(weightsLearnrates[pos], 
                        target, learnRateTprev[ pos + 1 ] );

                }
                adaptLernRateOld = target;
                adaptLernRate = true;

            }

        }   // end: SetLearningRate

        /// <summary>
        /// Helper function for the 'font end' ( 'CanvasChart' ) to make the data
        /// seen after loading .
        /// <para>
        /// From the layers field ( int[] ) a string representation
        /// is formed.
        /// </para>
        /// </summary>
        public void ParseTopic()
        {
            int topics = layersTopic.Length;
            string topicsText = "";
            for ( int pos = 0; pos < ( topics - 1 ); pos++) 
            {
                topicsText += layersTopic[pos].ToString() + ", ";

            }
            topicsText += layersTopic[topics - 1].ToString() + ".";
            workingTopic = topicsText;

        }   // end: ParseTopic

        /// <summary>
        /// Counts the headers in-/outputs.
        /// </summary>
        /// <param name="headers">the loaded headers from the excel file</param>
        /// <param name="inputs">number of them</param>
        /// <param name="outputs">number of them</param>
        public static void PartsNums( string[] headers,
            ref int inputs, ref int outputs )
        {
            inputs = headers.Count( ind => ind == "input" );
            outputs = headers.Count( ind => ind == "output" );

        }   // end: PartsNums

        /// <summary>
        /// Splits the data field into two parts. Needed for
        /// the splitting of the input data read from file ( Excel ).
        /// <para>Best used with PartsNums().</para>
        /// </summary>
        /// <param name="fullData">read data´block</param>
        /// <param name="firstSize">counted inputs from headers</param>
        /// <param name="secondSize">counted outputs from headers</param>
        /// <param name="firstPart">split field beginning</param>
        /// <param name="secondPart">split field continuing</param>
        public static void PartArray( double[] fullData,
            int firstSize, int secondSize,
            ref double[] firstPart, ref double[] secondPart )
        {
            firstPart = new double[ firstSize ];
            secondPart = new double[ secondSize ];
            for ( int run = 0; run < firstSize; run++ )
                firstPart[ run ] = fullData[ run ];
            for ( int run = 0; run < secondSize; run++ )
                secondPart[ run ] = fullData[ firstSize + run ];

        }   // end: PartArray

        /// <summary>
        /// Loads the training data from an Excel-file. Always asks
        /// for the filename with dialog.
        /// </summary>
        /// <param name="fileName">if given silent mode will be used</param>
        /// <param name="sheetNumber">the sheets number in the workbook</param>
        /// <param name="useHeaders">using a header line of cells?</param>
        /// <param name="noOfInputs">optional you give it manually</param>
        /// <param name="noOfOutputs">optional you give it manually</param>
        /// <returns>success of operation</returns>
        public bool LoadDataFromExcel( string fileName,
            int sheetNumber = 0, bool useHeaders = true,
            int noOfInputs = 0, int noOfOutputs = 0 )
        {
            bool ok = true;
            if ( fileName != "" )
                ok = myData.ReadWorkbook( fileName, true );
            else
                ok = myData.ReadWorkbook();
            if ( !ok )
            {
                MessageBox.Show( "dialog not successful...",
                    "Maybe because of cancel",
                    MessageBoxButton.OK, MessageBoxImage.Warning );
                return ( false );

            }
            string[] headers = ["input", "output"];
            myData.ReadSheets();
            myData.ReadSheetAsListDouble( sheetNumber , useHeaders );
            if ( useHeaders )
                headers = myData.GetHeaderNo( sheetNumber );
            double[][] doubles = myData.DataListDoubleAsArrayJagged();
            /*
            string message = $"doubles.Length: {doubles.Length}, "
                + $"doubles[0].Length: {doubles[0].Length}, "
                + $"doubles[0]: {doubles[..^1].ToString()}, "
                + $"doubles[0][0]: {doubles[0][0]}, ";
            //Message.Show( message );
            */
            int ins = 0;
            int outs = 0;
            if ( useHeaders )
                PartsNums( headers, ref ins, ref outs );
            else
            {
                ins = noOfInputs;
                outs = noOfOutputs; 
            }
            if ( ( ins != layersTopic[ 0 ] )
                    || ( outs != layersTopic[ layersNo - 1 ] ) )
                throw new ArgumentException(
                    "FFN.LoadDataFromExcel: data has to fit to the network layers, Abort!",
                        "( ( ins != layersTopic[ 0 ] )" +
                        "\n|| ( outs != layersTopic[ layersNo - 1 ] ) )" );

            localInputArrayField = new double[ doubles.Length ][];
            localOutputArrayField = new double[ doubles.Length ][];
            for ( int row = 0; row < doubles.Length; row++ )
            {
                Console.WriteLine( doubles[ row ] );
                PartArray( doubles[ row ], ins, outs,
                    ref localInputArrayField[ row ],
                    ref localOutputArrayField[ row ] );

            }
            localIns = ins;
            localOuts = outs;

            return ( true );

        }   // end: LoadDataFromExcel


        /// <summary>
        /// For convenience and speed the local
        /// data can now be prepared. 'Fit_LocalData'
        /// and 'Train_LocalData' can benefit from it.
        /// </summary>
        public void InitLocalData()
        {
            int dataLength = localInputArrayField.Length;
            if ( ( normalizeData ) && ( epochsNumber == 0 ) )
                DataNetInit( localInputArrayField, localOutputArrayField );
            localNetLayers0 = new Matrix[ dataLength ]; 
            localtargetT = new Matrix[ dataLength ];

            if ( normalizeData )
            {   // prepare the normalized speed data
                for ( int sampleN = 0; sampleN < dataLength; sampleN++ )
                {
                    localNetLayers0[ sampleN ] =
                        Matrix.FromArray( 
                            NormInputArray( localInputArrayField[ sampleN ] ) );
                    localtargetT[ sampleN ] =
                        Matrix.FromArrayTranspose( 
                            NormOutputArray( localOutputArrayField[ sampleN ] ) );

                }

            }   // end: prepare the normalized speed data

            if ( !normalizeData )
            {   // prepare the speed data
                for ( int sampleN = 0; sampleN < dataLength; sampleN++ )
                {
                    localNetLayers0[ sampleN ] =
                        Matrix.FromArray( localInputArrayField[ sampleN ] );
                    localtargetT[ sampleN ] =
                        Matrix.FromArrayTranspose( localOutputArrayField[ sampleN ] );

                }

            }   // end: prepare the speed data

        }   // end: InitLocalData

    }   // end: public class FFN

}   // end: namespace MatrixFFN

