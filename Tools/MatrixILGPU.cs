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

using ILGPU.Runtime;
using ILGPU;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ILGPU.IR;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Windows.Media.Media3D;
using System.Security.Cryptography.X509Certificates;
using ILGPU.IR.Values;
using System.Windows;

namespace MatrixFFN.Tools
{
    /// <summary>
    /// This class implements the matrix calculations for the 
    /// neuronal net. Variant for ILGPU.
    /// But these functions can be used completely freely.
    /// <para>
    /// Most important here is the multiplication of two matrices
    /// using the Falk-scheme.
    /// </para>
    /// <para>ILGPU needs additionally to the given logic the new GPU-side, where kernels 
    /// are run by calling 'Action's. For the speed you have to synchronize
    /// the CPU-side to your convenience if you use these functions freely.
    /// </para>
    /// <para>The functions are done in three different ways:</para>
    /// <para>- operation on the internal 'MatrixILGPU'</para>
    /// <para>- static Matrix function: operation on returned 'MatrixILGPU'</para>
    /// <para>- static void function: operation on targeted 'MatrixILGPU' ( best speed ) </para>
    /// </summary>
    public class MatrixILGPU
    {
        /// <summary>
        /// created on: 19.08.2024
        /// last edit: 02.10.24
        /// </summary>
        public Version version = new("1.0.7");

        /// <summary>
        /// ILGPU: highest level i s the 'Context' ( GPU [ 0, ... ] )
        /// </summary>
        Context context;
        /// <summary>
        /// ILGPU: 'Context' is dependent of the 'Device' ( GPU, Emulation )
        /// </summary>
        Device device;
        /// <summary>
        /// ILGPU: working process on the device, lifetime is given from the 'Device' ( 'GarbageCollector' )
        /// </summary>
        Accelerator accelerator;

        /// <summary>
        /// data of the matrix
        /// </summary>
        public double[,] data;
        /// <summary>
        /// ILGPU: the GPU-side of 'data'
        /// </summary>
        ArrayView2D<double, Stride2D.DenseX> dataIl;
        /// <summary>
        /// X-size of the matrix
        /// </summary>
        public int sizeX;
        /// <summary>
        /// Y-size of the matrix
        /// </summary>
        public int sizeY;
        /// <summary>
        /// filename for the own saving
        /// </summary>
        public string fileName = "MatrixILGPU";

        /* 
         * Practically an 'Action' will be called like a function.
         * Logically it is serving the explicit kernel call in the field.
        */
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            double >
            actionAddScalar_instance;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            double,
            ArrayView2D<double, Stride2D.DenseX>
            > actionAddScalar_static;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            ArrayView2D<double, Stride2D.DenseX>
            > actionAddMatrix_instance;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            ArrayView2D<double, Stride2D.DenseX>,
            ArrayView2D<double, Stride2D.DenseX>
            > actionAddMatrix_static;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            ArrayView2D<double, Stride2D.DenseX>
            > actionSubtractMatrix_instance;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            ArrayView2D<double, Stride2D.DenseX>,
            ArrayView2D<double, Stride2D.DenseX>
            > actionSubtractMatrix_static;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            ArrayView2D<double, Stride2D.DenseX>
            > actionTranspose_static;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            double
            > actionMultiplyScalar_instance;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            double,
            ArrayView2D<double, Stride2D.DenseX>
            > actionMultiplyScalar_static;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            ArrayView2D<double, Stride2D.DenseX>
            > actionMultiplySameSize_instance;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            ArrayView2D<double, Stride2D.DenseX>,
            ArrayView2D<double, Stride2D.DenseX>
            > actionMultiplySameSize_static;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            ArrayView2D<double, Stride2D.DenseX>,
            int,
            ArrayView2D<double, Stride2D.DenseX>
            > actionMultiply_static;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>
            > actionToSigmoid_instance;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            ArrayView2D<double, Stride2D.DenseX>
            > actionToSigmoid_static;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            ArrayView2D<double, Stride2D.DenseX>
            > actionDeriveSigmoid_any;
        Action<
            Index2D,
            ArrayView2D<double, Stride2D.DenseX>,
            double
            > actionMeanSquare_any;

        // -----------------------------------        the constructors

        /// <summary>
        /// parameterless constructor 
        /// </summary>
        public MatrixILGPU( )
        {
            // ILGPU: starting the engine
            context = Context.CreateDefault();
            device = context.GetPreferredDevice( false );
            accelerator = device.CreateAccelerator( context );

            // minimal matrix [ 1 x 1 ]
            sizeX = 1;
            sizeY = 1;
            data = new double[ sizeX, sizeY ];

            // creation of the data object on the GPU
            dataIl = accelerator.Allocate2DDenseX<double>( new Index2D( sizeX, sizeY ) );
            dataIl.CopyFromCPU( data );
            // definition of the 'Action's -> loading of the kernel functions
            actionAddScalar_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double
                >( AddScalar_instance_Kernel );
            actionAddScalar_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double,
                ArrayView2D<double, Stride2D.DenseX>
                >( AddScalar_static_Kernel );
            actionAddMatrix_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( AddMatrix_instance_Kernel );
            actionAddMatrix_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( AddMatrix_static_Kernel );
            actionSubtractMatrix_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( SubtractMatrix_instance_Kernel );
            actionSubtractMatrix_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( SubtractMatrix_static_Kernel );
            actionTranspose_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( Transpose_static_Kernel );
            actionMultiplyScalar_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double
                >( MultiplyScalar_instance_Kernel );
            actionMultiplyScalar_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double,
                ArrayView2D<double, Stride2D.DenseX>
                >( MultiplyScalar_static_Kernel );
            actionMultiplySameSize_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( MultiplySameSize_instance_Kernel );
            actionMultiplySameSize_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( MultiplySameSize_static_Kernel );
            actionMultiply_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                int,
                ArrayView2D<double, Stride2D.DenseX>
                >( Multiply_static_Kernel );
            actionToSigmoid_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>
                >( ToSigmoid_instance_Kernel );
            actionToSigmoid_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( ToSigmoid_static_Kernel );
            actionDeriveSigmoid_any = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( DeriveSigmoid_any_Kernel );
            actionMeanSquare_any = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double
                >( MeanSquare_any_Kernel );

        }   // end: MatrixILGPU ( constructor )

        /// <summary>
        /// constructor, filling the 'MatrixILGPU' with random
        /// values between [ -1, 1 ].
        /// <para>You can use your own spans.</para>
        /// </summary>
        /// <param name="inX">X-size of the 'MatrixILGPU'</param>
        /// <param name="inY">Y-size of the 'MatrixILGPU'</param>
        /// <param name="min">lower threshold for the random numbers</param>
        /// <param name="max">upper threshold for the random numbers</param>
        public MatrixILGPU( int inX, int inY, double min, double max )
        {
            // ILGPU: starting the engine
            context = Context.CreateDefault();
            device = context.GetPreferredDevice( false );
            accelerator = device.CreateAccelerator( context );

            Random zufall = new();

            sizeX = inX;
            sizeY = inY;
            data = new double[ sizeX, sizeY ];
            for ( int posX = 0; posX < sizeX; posX++ )
            {
                for ( int posY = 0; posY < sizeY; posY++ )
                {
                    data[ posX, posY ] =
                        min + ( zufall.NextDouble() * ( max - min ) );

                }

            }
            // creation of the data object on the GPU
            dataIl = accelerator.Allocate2DDenseX<double>( new Index2D( sizeX, sizeY ) );
            dataIl.CopyFromCPU( data );
            // definition of the 'Action's -> loading of the kernel functions
            actionAddScalar_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double
                >( AddScalar_instance_Kernel );
            actionAddScalar_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double,
                ArrayView2D<double, Stride2D.DenseX>
                >( AddScalar_static_Kernel );
            actionAddMatrix_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( AddMatrix_instance_Kernel );
            actionAddMatrix_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( AddMatrix_static_Kernel );
            actionSubtractMatrix_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( SubtractMatrix_instance_Kernel );
            actionSubtractMatrix_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( SubtractMatrix_static_Kernel );
            actionTranspose_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( Transpose_static_Kernel );
            actionMultiplyScalar_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double
                >( MultiplyScalar_instance_Kernel );
            actionMultiplyScalar_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double,
                ArrayView2D<double, Stride2D.DenseX>
                >( MultiplyScalar_static_Kernel );
            actionMultiplySameSize_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( MultiplySameSize_instance_Kernel );
            actionMultiplySameSize_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( MultiplySameSize_static_Kernel );
            actionMultiply_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                int,
                ArrayView2D<double, Stride2D.DenseX>
                >( Multiply_static_Kernel );
            actionToSigmoid_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>
                >( ToSigmoid_instance_Kernel );
            actionToSigmoid_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( ToSigmoid_static_Kernel );
            actionDeriveSigmoid_any = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( DeriveSigmoid_any_Kernel );
            actionMeanSquare_any = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double
                >( MeanSquare_any_Kernel );

        }   // end: MatrixILGPU ( constructor )

        /// <summary>
        /// constructor setting a special value for all elements.
        /// <para>The fastest done is the zero-matrix.</para>
        /// </summary>
        /// <param name="inX">X-size of the 'MatrixILGPU'</param>
        /// <param name="inY">Y-size of the 'MatrixILGPU'</param>
        /// <param name="val">your element value</param>
        public MatrixILGPU( int inX, int inY, double val = 0 )
        {
            // ILGPU: starting the engine
            context = Context.CreateDefault();
            device = context.GetPreferredDevice( false );
            accelerator = device.CreateAccelerator( context );

            sizeX = inX;
            sizeY = inY;
            data = new double[ sizeX, sizeY ];
            if ( val != 0 )
                for ( int posX = 0; posX < sizeX; posX++ )
                {
                    for ( int posY = 0; posY < sizeY; posY++ )
                    {
                        data[ posX, posY ] = val;

                    }

                }
            // creation of the data object on the GPU
            dataIl = accelerator.Allocate2DDenseX<double>( new Index2D( sizeX, sizeY ) );
            dataIl.CopyFromCPU( data );
            // definition of the 'Action's -> loading of the kernel functions
            actionAddScalar_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double
                >( AddScalar_instance_Kernel );
            actionAddScalar_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double,
                ArrayView2D<double, Stride2D.DenseX>
                >( AddScalar_static_Kernel );
            actionAddMatrix_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( AddMatrix_instance_Kernel );
            actionAddMatrix_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( AddMatrix_static_Kernel );
            actionSubtractMatrix_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( SubtractMatrix_instance_Kernel );
            actionSubtractMatrix_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( SubtractMatrix_static_Kernel );
            actionTranspose_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( Transpose_static_Kernel );
            actionMultiplyScalar_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double
                >( MultiplyScalar_instance_Kernel );
            actionMultiplyScalar_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double,
                ArrayView2D<double, Stride2D.DenseX>
                >( MultiplyScalar_static_Kernel );
            actionMultiplySameSize_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( MultiplySameSize_instance_Kernel );
            actionMultiplySameSize_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( MultiplySameSize_static_Kernel );
            actionMultiply_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                int,
                ArrayView2D<double, Stride2D.DenseX>
                >( Multiply_static_Kernel );
            actionToSigmoid_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>
                >( ToSigmoid_instance_Kernel );
            actionToSigmoid_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( ToSigmoid_static_Kernel );
            actionDeriveSigmoid_any = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( DeriveSigmoid_any_Kernel );
            actionMeanSquare_any = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double
                >( MeanSquare_any_Kernel );

        }   // end: MatrixILGPU ( constructor )

        /// <summary>
        /// This constructor loads his data from a BinaryReader
        /// and initializes himself with them.
        /// </summary>
        /// <param name="reader">a 'BinaryReader'</param>
        public MatrixILGPU( BinaryReader reader )
        {
            // ILGPU: starting the engine
            context = Context.CreateDefault();
            device = context.GetPreferredDevice( false );
            accelerator = device.CreateAccelerator( context );

            sizeX = 1;
            sizeY = 1;
            data = new double[sizeX, sizeY];
            // creation of the data object on the GPU
            dataIl = accelerator.Allocate2DDenseX<double>( new Index2D( sizeX, sizeY ) );


            LoadDataFromReader( reader );

            // definition of the 'Action's -> loading of the kernel functions
            actionAddScalar_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double
                >( AddScalar_instance_Kernel );
            actionAddScalar_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double,
                ArrayView2D<double, Stride2D.DenseX>
                >( AddScalar_static_Kernel );
            actionAddMatrix_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( AddMatrix_instance_Kernel );
            actionAddMatrix_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( AddMatrix_static_Kernel );
            actionSubtractMatrix_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( SubtractMatrix_instance_Kernel );
            actionSubtractMatrix_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( SubtractMatrix_static_Kernel );
            actionTranspose_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( Transpose_static_Kernel );
            actionMultiplyScalar_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double
                >( MultiplyScalar_instance_Kernel );
            actionMultiplyScalar_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double,
                ArrayView2D<double, Stride2D.DenseX>
                >( MultiplyScalar_static_Kernel );
            actionMultiplySameSize_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( MultiplySameSize_instance_Kernel );
            actionMultiplySameSize_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( MultiplySameSize_static_Kernel );
            actionMultiply_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>,
                int,
                ArrayView2D<double, Stride2D.DenseX>
                >( Multiply_static_Kernel );
            actionToSigmoid_instance = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>
                >( ToSigmoid_instance_Kernel );
            actionToSigmoid_static = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( ToSigmoid_static_Kernel );
            actionDeriveSigmoid_any = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                ArrayView2D<double, Stride2D.DenseX>
                >( DeriveSigmoid_any_Kernel );
            actionMeanSquare_any = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<double, Stride2D.DenseX>,
                double
                >( MeanSquare_any_Kernel );

        }   // end: Matrix ( constructor )

        // -------------------------------------        output functions

        /// <summary>
        /// standard output of the 'MatrixILGPU'.
        /// </summary>
        /// <returns>string representation of the 'MatrixILGPU'</returns>
        override
        public string ToString( )
        {
            SynchronizeCPU();
            string meldung = $"Output of the 'MatrixILGPU' [ {sizeX}, {sizeY} ]\n";
            string text;
            for ( int posY = 0; posY < sizeY; posY++ )
            {
                text = "[ ";
                for ( int posX = 0; posX < sizeX; posX++ )
                    text += data[ posX, posY ] + " ";
                text += "]\n";
                meldung += text;

            }
            meldung += "--------------------------------------------------\n";
            return ( meldung );

        }   // end: ToString

        /// <summary>
        /// Gives the 'MatrixILGPU' via ToString() to standard output.
        /// </summary>
        public void Print( )
        {
            System.Console.WriteLine( ToString() );

        }   // end: Print

        // --------------------------------------------     the ADDS

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: add the 'value' to each element
        /// of the internal 'MatrixILGPU'.
        /// </summary>
        /// <param name="value">value to add</param>
        public void AddScalar( double value )
        {
            // the 'Action' runs the kernel with the given parameters ( index, ... ) on the GPU
            actionAddScalar_instance( dataIl.Extent.ToIntIndex(), dataIl, value );
            accelerator.Synchronize();

        }   // end: AddScalar

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">"2D-position in the 'MatrixILGPU'</param>
        /// <param name="gpuMat">GPU's dataIL</param>
        /// <param name="value">one Scalar</param>
        static void AddScalar_instance_Kernel(
            Index2D index,
            ArrayView2D<double, Stride2D.DenseX> gpuMat,
            double value )
        {
            gpuMat[ index.X, index.Y ] += value;

        }   // end: AddScalar_instance_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Add the 'value' to each element
        /// of the internal 'MatrixILGPU'.
        /// </summary>
        /// <param name="matrix">source</param>
        /// <param name="value">value to add</param>
        /// <returns>result 'MatrixILGPU'</returns>
        public static MatrixILGPU AddScalar( MatrixILGPU matrix, double value )
        {
            MatrixILGPU temp = new( matrix.sizeX, matrix.sizeY );

            matrix.actionAddScalar_static(
                matrix.dataIl.Extent.ToIntIndex(),
                matrix.dataIl,
                value,
                temp.dataIl );
            matrix.accelerator.Synchronize();

            temp.data = temp.dataIl.GetAsArray2D();

            return ( temp );

        }   // end: AddScalar

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">"2D-position in the 'MatrixILGPU'</param>
        /// <param name="gpuMat">GPU's dataIL</param>
        /// <param name="value">one scalar</param>
        /// <param name="resMat">result GPU's dataIL</param>
        static void AddScalar_static_Kernel(
            Index2D index,
            ArrayView2D<double, Stride2D.DenseX> gpuMat,
            double value,
            ArrayView2D<double, Stride2D.DenseX> resMat
            )
        {
            resMat[ index.X, index.Y ] =
                gpuMat[ index.X, index.Y ] + value;

        }   // end: AddScalar_static_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Add the 'value' to each element
        /// of the internal 'MatrixILGPU' and delivers the result
        /// to the target 'MatrixILGPU' ( best speed ).
        /// </summary>
        /// <param name="source">source</param>
        /// <param name="value">value to add</param>
        /// <param name="target">reference to the target</param>
        public static void AddScalar_Target( MatrixILGPU source, double value, MatrixILGPU target )
        {
            if ( ( source.sizeX != target.sizeX )
                    || ( source.sizeY != target.sizeY ) )
                throw new ArgumentException(
                    "MatrixILGPU.AddScalar_Target: different sizes of the matrices, Abort!",
                    "( ( source.sizeX != target.sizeX )\r\n" +
                    "|| ( source.sizeY != target.sizeY ) )" );

            source.actionAddScalar_static(
                source.dataIl.Extent.ToIntIndex(),
                source.dataIl,
                value,
                target.dataIl );
            source.accelerator.Synchronize();

        }   // end: AddScalar_Target

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Adds a same sized 'MatrixILGPU' to the internal.
        /// </summary>
        /// <param name="m">that to add one</param>
        public void AddMatrix( MatrixILGPU m )
        {
            if ( ( sizeX != m.sizeX ) || ( sizeY != m.sizeY ) )
                throw new ArgumentException(
                    "MatrixILGPU.AddMatrix: different sizes of the matrices, Abort!",
                    "( ( sizeX != m.sizeX ) || ( sizeY != m.sizeY ) )" );

            actionAddMatrix_instance(
                dataIl.Extent.ToIntIndex(),
                dataIl,
                m.dataIl );
            accelerator.Synchronize();

        }   // end: AddMatrix

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">"2D-position in the 'MatrixILGPU'</param>
        /// <param name="intern">the internal ones 'dataIL'</param>
        /// <param name="source">source 'dataIL'</param>
        public static void AddMatrix_instance_Kernel(
            Index2D index,
            ArrayView2D<double, Stride2D.DenseX> intern,
            ArrayView2D<double, Stride2D.DenseX> source
            )
        {
            intern[ index.X, index.Y ] += source[ index.X, index.Y ];

        }   // end: AddMatrix_instance_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Adds two same sized 'MatrixILGPU's.
        /// </summary>
        /// <param name="m1">'MatrixILGPU' 1</param>
        /// <param name="m2">'MatrixILGPU' 2</param>
        /// <returns>resulting 'MatrixILGPU'</returns>
        public static MatrixILGPU AddMatrix( MatrixILGPU m1, MatrixILGPU m2 )
        {
            if ( ( m1.sizeX != m2.sizeX ) || ( m1.sizeY != m2.sizeY ) )
                throw new ArgumentException(
                    "MatrixILGPU.AddMatrix: different sizes of the matrices, Abort!",
                        "( ( m1.sizeX != m2.sizeX ) || ( m1.sizeY != m2.sizeY ) )" );

            MatrixILGPU temp = new( m1.sizeX, m1.sizeY, 0 );

            m1.actionAddMatrix_static(
                m1.dataIl.Extent.ToIntIndex(),
                m1.dataIl,
                m2.dataIl,
                temp.dataIl );
            m1.accelerator.Synchronize();

            temp.data = temp.dataIl.GetAsArray2D();

            return ( temp );

        }   // end: AddMatrix

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">"2D-position in the 'MatrixILGPU'</param>
        /// <param name="source1">source 'MatrixILGPU' 1</param>
        /// <param name="source2">source 'MatrixILGPU' 2</param>
        /// <param name="target">target 'MatrixILGPU'</param>
        public static void AddMatrix_static_Kernel(
            Index2D index,
            ArrayView2D<double, Stride2D.DenseX> source1,
            ArrayView2D<double, Stride2D.DenseX> source2,
            ArrayView2D<double, Stride2D.DenseX> target
            )
        {

            target[ index.X, index.Y ] =
                  source1[ index.X, index.Y ] +
                  source2[ index.X, index.Y ];


        }   // end: AddMatrix_static_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Adds two same sized 'MatrixILGPU's.
        /// </summary>
        /// <param name="m1">'MatrixILGPU' 1</param>
        /// <param name="m2">'MatrixILGPU' 2</param>
        /// <param name="target">target 'MatrixILGPU'</param>

        public static void AddMatrix_target( MatrixILGPU m1, MatrixILGPU m2, MatrixILGPU target )
        {
            bool weiter = true;
            if ( ( m1.sizeX != m2.sizeX ) || ( m1.sizeY != m2.sizeY ) )
                weiter = false;
            if ( ( m1.sizeX != target.sizeX ) || ( m1.sizeY != target.sizeY ) )
                weiter = false;
            if ( ( target.sizeX != m2.sizeX ) || ( target.sizeY != m2.sizeY ) )
                weiter = false;
            if ( !weiter )
                throw new ArgumentException(
                    "MatrixILGPU.AddMatrix_target: different sizes of the matrices, Abort!",
                    "( ( m1.sizeX != m2.sizeX ) || ( m1.sizeY != m2.sizeY ) )\r\n" +
                    "|| ( ( m1.sizeX != target.sizeX ) || ( m1.sizeY != target.sizeY ) )\r\n" +
                    "|| ( ( target.sizeX != m2.sizeX ) || ( target.sizeY != m2.sizeY ) )" );

            m1.actionAddMatrix_static(
                m1.dataIl.Extent.ToIntIndex(),
                m1.dataIl,
                m2.dataIl,
                target.dataIl );
            m1.accelerator.Synchronize();

        }   // end: AddMatrix_target

        // ------------------------------------------------        the SUBTRACTS

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: subtracts the 'value' from each element
        /// of the internal 'MatrixILGPU'.
        /// </summary>
        /// <param name="value">value to subtract</param>
        public void SubtractScalar( double value )
        {
            actionAddScalar_instance( dataIl.Extent.ToIntIndex(), dataIl, -value );
            accelerator.Synchronize();

        }   // end: SubtractScalar

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Subtracts the 'value' from each element
        /// of the source 'MatrixILGPU'.
        /// </summary>
        /// <param name="matrix">source</param>
        /// <param name="value">value to subtract</param>
        /// <returns>result 'MatrixILGPU'</returns>
        public static MatrixILGPU SubtractScalar( MatrixILGPU matrix, double value )
        {
            MatrixILGPU temp = new( matrix.sizeX, matrix.sizeY );

            matrix.actionAddScalar_static(
                matrix.dataIl.Extent.ToIntIndex(),
                matrix.dataIl,
                -value,
                temp.dataIl );
            matrix.accelerator.Synchronize();

            temp.data = temp.dataIl.GetAsArray2D();

            return ( temp );

        }   // end: SubtractScalar

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Subtracts the 'value' from each element
        /// of the internal 'MatrixILGPU' and 
        /// delivers the target ( best speed ).
        /// </summary>
        /// <param name="source">source 'MatrixILGPU'</param>
        /// <param name="value">value to subtract</param>
        /// <param name="target">target 'MatrixILGPU'</param>
        public static void SubtractScalar_Target( MatrixILGPU source, double value, MatrixILGPU target )
        {
            if ( ( source.sizeX != target.sizeX )
                    || ( source.sizeY != target.sizeY ) )
                throw new ArgumentException(
                    "MatrixILGPU.SubtractScalar_Target: different sizes of the matrices, Abort!",
                    "( ( source.sizeX != target.sizeX )\r\n" +
                    "|| ( source.sizeY != target.sizeY ) )" );

            source.actionAddScalar_static(
                source.dataIl.Extent.ToIntIndex(),
                source.dataIl,
                -value,
                target.dataIl );
            source.accelerator.Synchronize();

        }   // end: SubtractScalar_Target

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: subtracts a even sized 'MatrixILGPU' from the internal one.
        /// </summary>
        /// <param name="m">the to subtract 'Matrix'</param>
        public void SubtractMatrix( MatrixILGPU m )
        {
            if ( ( sizeX != m.sizeX ) || ( sizeY != m.sizeY ) )
                throw new ArgumentException(
                    "MatrixILGPU.SubtractMatrix: different sizes of the matrices, Abort!",
                    "( ( sizeX != m.sizeX ) || ( sizeY != m.sizeY ) )" );

            actionSubtractMatrix_instance(
                dataIl.Extent.ToIntIndex(),
                dataIl,
                m.dataIl );
            accelerator.Synchronize();

        }   // end: SubtractMatrix

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">"2D-position in the 'MatrixILGPU'</param>
        /// <param name="intern">internal's 'dataIL'</param>
        /// <param name="source">source's 'dataIL'</param>
        public static void SubtractMatrix_instance_Kernel(
                Index2D index,
                ArrayView2D<double, Stride2D.DenseX> intern,
                ArrayView2D<double, Stride2D.DenseX> source
            )
        {
            intern[ index.X, index.Y ] -= source[ index.X, index.Y ];

        }   // end: SubtractMatrix_instance_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Subtraction of two 'MatrixILGPU's.
        /// </summary>
        /// <param name="m1">'MatrixILGPU' 1</param>
        /// <param name="m2">'MatrixILGPU' 2</param>
        /// <returns>result 'MatrixILGPU'</returns>
        public static MatrixILGPU SubtractMatrix( MatrixILGPU m1, MatrixILGPU m2 )
        {
            if ( ( m1.sizeX != m2.sizeX ) || ( m1.sizeY != m2.sizeY ) )
                throw new ArgumentException(
                    "MatrixILGPU.SubtractMatrix: different sizes of the matrices, Abort!",
                        "shape( m1 ) != shape( m2 )" );

            MatrixILGPU temp = new( m1.sizeX, m1.sizeY );

            m1.actionSubtractMatrix_static(
                m1.dataIl.Extent.ToIntIndex(),
                m1.dataIl,
                m2.dataIl,
                temp.dataIl );
            m1.accelerator.Synchronize();

            temp.data = temp.dataIl.GetAsArray2D();

            return ( temp );

        }   // end: SubtractMatrix


        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">"2D-position in the 'MatrixILGPU'</param>
        /// <param name="source1">'dataIL' 1</param>
        /// <param name="source2">dataIL' 2</param>
        /// <param name="target">target's 'dataIL'</param>
        public static void SubtractMatrix_static_Kernel(
                Index2D index,
                ArrayView2D<double, Stride2D.DenseX> source1,
                ArrayView2D<double, Stride2D.DenseX> source2,
                ArrayView2D<double, Stride2D.DenseX> target
            )
        {
            target[ index.X, index.Y ] =
                        source1[ index.X, index.Y ] -
                        source2[ index.X, index.Y ];

        }   // end:Subtract_MatrixKernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Subtraction of two 'MatrixILGPU's.
        /// </summary>
        /// <param name="m1">'dataIL' 1</param>
        /// <param name="m2">'dataIL' 2</param>
        /// <param name="target">target 'dataIL'</param>
        public static void SubtractMatrix( MatrixILGPU m1, MatrixILGPU m2, MatrixILGPU target )
        {
            bool weiter = true;
            if ( ( m1.sizeX != m2.sizeX ) || ( m1.sizeY != m2.sizeY ) )
                weiter = false;
            if ( ( m1.sizeX != target.sizeX ) || ( m1.sizeY != target.sizeY ) )
                weiter = false;
            if ( ( target.sizeX != m2.sizeX ) || ( target.sizeY != m2.sizeY ) )
                weiter = false;
            if ( !weiter )
                throw new ArgumentException(
                    "MatrixILGPU.SubtractMatrix: different sizes of the matrices, Abort!",
                    "( ( m1.sizeX != m2.sizeX ) || ( m1.sizeY != m2.sizeY ) )\r\n" +
                    "|| ( ( m1.sizeX != target.sizeX ) || ( m1.sizeY != target.sizeY ) )\r\n" +
                    "|| ( ( target.sizeX != m2.sizeX ) || ( target.sizeY != m2.sizeY ) )" );

            m1.actionSubtractMatrix_static(
                m1.dataIl.Extent.ToIntIndex(),
                m1.dataIl,
                m2.dataIl,
                target.dataIl );
            m1.accelerator.Synchronize();

        }   // end: SubtractMatrix

        // -------------------------------------------------       the TRANSPOSES

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">"2D-position in the 'MatrixILGPU'</param>
        /// <param name="source">´source 'dataIL'</param>
        /// <param name="target">target 'dataIL'</param>
        public static void Transpose_static_Kernel(
                Index2D index,
                ArrayView2D<double, Stride2D.DenseX> source,
                ArrayView2D<double, Stride2D.DenseX> target
            )
        {
            target[ index.Y, index.X ] = source[ index.X, index.Y ];

        }   // end: Transpose_static_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: transposes the 'MatrixILGPU'.
        /// </summary>
        /// <param name="m">input 'MatrixILGPU'</param>
        /// <returns>result 'MatrixILGPU' [ m.sizeY, m.sizeX ]</returns>
        public static MatrixILGPU Transpose( MatrixILGPU m )
        {
            MatrixILGPU temp = new( m.sizeY, m.sizeX );

            m.actionTranspose_static(
                m.dataIl.Extent.ToIntIndex(),
                m.dataIl,
                temp.dataIl );
            m.accelerator.Synchronize();

            return ( temp );

        }   // end: Transpose

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Transposes the 'MatrixILGPU'.
        /// </summary>
        /// <param name="source">input 'MatrixILGPU'</param>
        /// <param name="target">'MatrixILGPU' [ m.sizeY, m.sizeX ]</param>
        public static void Transpose( MatrixILGPU source, MatrixILGPU target )
        {
            if ( ( source.sizeX != target.sizeY )
                || ( source.sizeY != target.sizeX ) )
                throw new ArgumentException(
                    "MatrixILGPU.Transpose: incompatible sizes of the two matrices, Abort!",
                    "( ( source.sizeX != target.sizeY )\r\n" +
                    "|| ( source.sizeY != target.sizeX ) )" );

            source.actionTranspose_static(
                source.dataIl.Extent.ToIntIndex(),
                source.dataIl,
                target.dataIl );
            source.accelerator.Synchronize();

        }   // end: Transpose

        // ------------------------------------------------------    die MULTIPLYS

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: multiplies every matrix element with a value.
        /// </summary>
        /// <param name="value">the multiplication's value</param>
        public void MultiplyScalar( double value )
        {
            actionMultiplyScalar_instance(
                dataIl.Extent.ToIntIndex(),
                dataIl,
                value );
            accelerator.Synchronize();

        }   // end: MultiplyScalar

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">position in the 'MatrixILGPU'</param>
        /// <param name="intern">the internal 'dataIL'</param>
        /// <param name="value">scalar value for the multiplication</param>
        public static void MultiplyScalar_instance_Kernel(
                Index2D index,
                ArrayView2D<double, Stride2D.DenseX> intern,
                double value ) 
        {
            intern[ index.X, index.Y ] *= value;

        }   // end: MultiplyScalar_instance_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: multiplies every matrix element with a value.
        /// </summary>
        /// <param name="source">source 'MatrixILGPU'</param>
        /// <param name="value">the multiplication's value</param>
        /// <returns>result 'MatrixILGPU'</returns>
        public static MatrixILGPU MultiplyScalar( MatrixILGPU source, double value )
        {
            MatrixILGPU target = new(source.sizeX, source.sizeY, 0);

            source.actionMultiplyScalar_static(
                source.dataIl.Extent.ToIntIndex(),
                source.dataIl,
                value,
                target.dataIl );
            source.accelerator.Synchronize();

            target.data = target.dataIl.GetAsArray2D();

            return ( target );

        }   // end: MultiplyScalar

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">position in the 'MatrixILGPU'</param>
        /// <param name="source">source 'dataIL'</param>
        /// <param name="value">scalar value for the multiplication</param>
        /// <param name="target">target 'dataIL'</param>
        public static void MultiplyScalar_static_Kernel(
                Index2D index,
                ArrayView2D<double, Stride2D.DenseX> source,
                double value,
                ArrayView2D<double, Stride2D.DenseX> target
           )
        {
            target[ index.X, index.Y ] =
                source[ index.X, index.Y ] * value;

        }   // end: MultiplyScalar_static_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: multiplies every matrix element with a value and
        /// delivers it into the target'MatrixILGPU' ( best speed ).
        /// </summary>
        /// <param name="source">source 'MatrixILGPU'</param>
        /// <param name="value">the multiplication's value</param>
        /// <param name="target">target 'MatrixILGPU'</param>
        public static void MultiplyScalar( MatrixILGPU source, double value,
            MatrixILGPU target )
        {
            if ( ( source.sizeX != target.sizeX )
                || ( source.sizeY != target.sizeY ) )
                throw new ArgumentException(
                    "MatrixILGPU.MultiplyScalar: incompatible sizes of the two matrices, Abort!",
                    "( ( source.sizeX != target.sizeX )\r\n" +
                    "|| ( source.sizeY != target.sizeY ) )" );

            source.actionMultiplyScalar_static(
                source.dataIl.Extent.ToIntIndex(),
                source.dataIl,
                value,
                target.dataIl );
            source.accelerator.Synchronize();

        }   // end: MultiplyScalar

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: multiplies the input 'Matrix' to the internal if 
        /// they have the same size.
        /// </summary>
        /// <param name="m">input 'MatrixILGPU'</param>
        public void MultiplySameSize( MatrixILGPU m )
        {
            if ( ( sizeX != m.sizeX ) || ( sizeY != m.sizeY ) )
                throw new ArgumentException(
                    "MatrixILGPU.MultiplySameSize: incompatible sizes of the two matrices, Abort!",
                    "( ( sizeX != m.sizeX ) || ( sizeY != m.sizeY ) )" );

            actionMultiplySameSize_instance(
                dataIl.Extent.ToIntIndex(),
                dataIl,
                m.dataIl );
            accelerator.Synchronize();

        }   // end: MultiplySameSize

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">position in the 'MatrixILGPU'</param>
        /// <param name="intern">internal 'dataIL'</param>
        /// <param name="source">source 'dataIL'</param>
        public static void MultiplySameSize_instance_Kernel(
                Index2D index,
                ArrayView2D<double, Stride2D.DenseX> intern,
                ArrayView2D<double, Stride2D.DenseX> source
            )
        {
            intern[ index.X, index.Y ] *= source[ index.X, index.Y ];

        }   // end: MultiplySameSize_instance_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Multiplies the input 'MatrixILGPU' to the internal if 
        /// they have the same size.
        /// </summary>
        /// <param name="m1">'MatrixILGPU' 1</param>
        /// <param name="m2">'MatrixILGPU' 2</param>
        /// <returns>result 'MatrixILGPU'</returns>
        public static MatrixILGPU MultiplySameSize( MatrixILGPU m1, MatrixILGPU m2 )
        {
            if ( ( m1.sizeX != m2.sizeX ) || ( m1.sizeY != m2.sizeY ) )
                throw new ArgumentException(
                    "MatrixILGPU.MultiplySameSize: incompatible sizes of the two matrices, Abort!",
                        "shape( m1 ) != shape( m2 )" );

            MatrixILGPU target = new( m1.sizeX, m1.sizeY, 0 );

            m1.actionMultiplySameSize_static(
                m1.dataIl.Extent.ToIntIndex(),
                m1.dataIl,
                m2.dataIl,
                target.dataIl );
            m1.accelerator.Synchronize();

            target.data = target.dataIl.GetAsArray2D();

            return ( target );

        }   // end: MultiplySameSize

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">position in the 'MatrixILGPU'</param>
        /// <param name="source1">'dataIL' 1</param>
        /// <param name="source2">'dataIL' 2</param>
        /// <param name="target">target 'dataIL'</param>
        public static void MultiplySameSize_static_Kernel(
                Index2D index,
                ArrayView2D<double, Stride2D.DenseX> source1,
                ArrayView2D<double, Stride2D.DenseX> source2,
                ArrayView2D<double, Stride2D.DenseX> target
            )
        {
            target[ index.X, index.Y ] = source1[ index.X, index.Y ] 
                    * source2[ index.X, index.Y ];

        }   // end: MultiplySameSize_static_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Multiplies the input 'MatrixILGPU' to the internal if 
        /// they have the same size. Result will be written
        /// into the target 'MatrixILGPU' ( best speed ).
        /// </summary>
        /// <param name="m1">'MatrixILGPU' 1</param>
        /// <param name="m2">'MatrixILGPU' 2</param>
        /// <param name="target">target 'MatrixILGPU'</param>
        public static void MultiplySameSize( MatrixILGPU m1, MatrixILGPU m2,
                MatrixILGPU target )
        {
            bool weiter = true;
            if ( ( m1.sizeX != m2.sizeX ) || ( m1.sizeY != m2.sizeY ) )
                weiter = false;
            if ( ( m1.sizeX != target.sizeX ) || ( m1.sizeY != target.sizeY ) )
                weiter = false;
            if ( ( target.sizeX != m2.sizeX ) || ( target.sizeY != m2.sizeY ) )
                weiter = false;
            if ( !weiter )
                throw new ArgumentException(
                    "MatrixILGPU.MultiplySameSize: incompatible sizes of the three matrices, Abort!",
                        "( ( m1.sizeX != m2.sizeX ) || ( m1.sizeY != m2.sizeY ) )\r\n" +
                        "|| ( ( m1.sizeX != target.sizeX ) || ( m1.sizeY != target.sizeY ) )\r\n" +
                        "|| ( ( target.sizeX != m2.sizeX ) || ( target.sizeY != m2.sizeY ) )" );

            m1.actionMultiplySameSize_static(
                m1.dataIl.Extent.ToIntIndex(),
                m1.dataIl,
                m2.dataIl,
                target.dataIl );
            m1.accelerator.Synchronize();

        }   // end: MultiplySameSize

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: the heart piece: the 'crossproduct' using the Falk-scheme 
        /// on both 'MatrixILGPU's.
        /// <para>
        /// Info: ( m1.sizeX == m2.sizeY ) 'AND' target[ m2.sizeX, m1.sizeY ]
        /// </para>
        /// </summary>
        /// <param name="m1">'MatrixILGPU' 1</param>
        /// <param name="m2">'MatrixILGPU' 2</param>
        /// <returns>result 'MatrixILGPU'</returns>
        public static MatrixILGPU Multiply( MatrixILGPU m1, MatrixILGPU m2 )
        {
            if ( m1.sizeX != m2.sizeY )
                throw new ArgumentException(
                    "MatrixILGPU.Multiply: relational side of both 'MatrixILGPU's must be the same, Abort!",
                        "m1.sizeX != m2.sizeY" );
 
            MatrixILGPU target = new(m2.sizeX, m1.sizeY, 0);

            m1.actionMultiply_static(
                target.dataIl.Extent.ToIntIndex(),
                m1.dataIl,
                m2.dataIl,
                m1.sizeX,
                target.dataIl );
            m1.accelerator.Synchronize();

            target.data = target.dataIl.GetAsArray2D();

            return ( target );

        }   // end: Multiply

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">position in the 'MatrixILGPU'</param>
        /// <param name="m1">'dataIL' 1</param>
        /// <param name="m2">'dataIL' 2</param>
        /// <param name="kante">length of the relational side</param>
        /// <param name="target">target 'dataIL'</param>
        public static void Multiply_static_Kernel(
                Index2D index,
                ArrayView2D<double, Stride2D.DenseX> m1,
                ArrayView2D<double, Stride2D.DenseX> m2,
                int kante,
                ArrayView2D<double, Stride2D.DenseX> target
            )
        {
            // relational side's length must be given
            
            for ( int zeiger = 0; zeiger < kante; zeiger++ )
            {
                target[ index.X, index.Y ] +=
                    m1[ zeiger, index.Y ]
                    * m2[ index.X, zeiger ];


            }

        }   // end: Multiply_static_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: the heart piece: the 'crossproduct' using the Falk-scheme 
        /// on both 'MatrixILGPU's. Delivers to a target 'MatrixILGPU' ( best speed ).
        /// <para>
        /// Info: ( m1.sizeX == m2.sizeY ) 'AND' target[ m2.sizeX, m1.sizeY ]
        /// </para>
        /// </summary>
        /// <param name="m1">'MatrixILGPU' 1</param>
        /// <param name="m2">'MatrixILGPU' 2</param>
        /// <param name="target">result 'MatrixILGPU'</param>
        public static void Multiply( MatrixILGPU m1, MatrixILGPU m2,
                MatrixILGPU target )
        {
            bool weiter = true;
            if ( m1.sizeX != m2.sizeY )
                weiter = false;
            if ( target.sizeX != m2.sizeX )
                weiter = false;
            if ( target.sizeY != m1.sizeY )
                weiter = false;
            if ( !weiter )
                throw new ArgumentException(
                    "MatrixILGPU.Multiply: relational side of the three 'MatrixILGPU's must be the same, Abort!",
                        "( m1.sizeX != m2.sizeY ) || ( target.sizeX != m2.sizeX ) || ( target.sizeY != m1.sizeY )" );

            m1.actionMultiply_static(
                target.dataIl.Extent.ToIntIndex(),
                m1.dataIl,
                m2.dataIl,
                m1.sizeX,
                target.dataIl );
            m1.accelerator.Synchronize();

        }   // end: Multiply

        // ----------------------------------------------       the SIGMOIDS

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: activation function is the unsymmetrical logistic function - 
        /// the internal 'MatrixILGPU' will be altered.
        /// </summary>
        public void ToSigmoid( )
        {

            actionToSigmoid_instance(
                dataIl.Extent.ToIntIndex(),
                dataIl );
            accelerator.Synchronize();

        }   // end: ToSigmoid

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">position in the 'MatrixILGPU'</param>
        /// <param name="matrix">the 'dataIL'</param>
        public static void ToSigmoid_instance_Kernel(
                Index2D index,
                ArrayView2D<double, Stride2D.DenseX> matrix
            )
        {
            matrix[ index.X, index.Y ] =
                        1 / ( 1 + Math.Exp( -matrix[ index.X, index.Y ] ) );

        }   // end: ToSigmoid_instance_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: activation function is the unsymmetrical logistic function - 
        /// returns the result 'MatrixILGPU'.
        /// </summary>
        /// <param name="source">input 'MatrixILGPU'</param>
        /// <returns>the sigmoid result 'MatrixILGPU'</returns>
        public static MatrixILGPU ToSigmoid( MatrixILGPU source )
        {
            MatrixILGPU target = new( source.sizeX, source.sizeY, 0 );

            source.actionToSigmoid_static(
                source.dataIl.Extent.ToIntIndex(),
                source.dataIl,
                target.dataIl );
            source.accelerator.Synchronize();

            target.data = target.dataIl.GetAsArray2D();

            return ( target );

        }   // end: ToSigmoid

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">position in the 'MatrixILGPU'</param>
        /// <param name="source">source 'dataIL'</param>
        /// <param name="target">result 'dataIL'</param>
        public static void ToSigmoid_static_Kernel(
                Index2D index,
                ArrayView2D<double, Stride2D.DenseX> source,
                ArrayView2D<double, Stride2D.DenseX> target
            )
        {
            target[ index.X, index.Y ] =
                        1 / ( 1 + Math.Exp( -source[ index.X, index.Y ] ) );

        }   // end: ToSigmoid_static_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Activation function is the unsymmetrical logistic function - 
        /// returns the result 'Matrix' into target ( best speed ).
        /// </summary>
        /// <param name="source">input 'Matrix'</param>
        /// <param name="target">the sigmoid result 'Matrix'</param>
        public static void ToSigmoid( MatrixILGPU source, MatrixILGPU target )
        {
            if ( ( source.sizeX != target.sizeX )
                || ( source.sizeY != target.sizeY ) )
                throw new ArgumentException(
                    "MatrixILGPU.ToSigmoid: incompatible sizes of the two matrices, Abort!",
                    "( ( source.sizeX != target.sizeX )\r\n" +
                    "|| ( source.sizeY != target.sizeY ) )" );

            source.actionToSigmoid_static(
                source.dataIl.Extent.ToIntIndex(),
                source.dataIl,
                target.dataIl );
            source.accelerator.Synchronize();

        }   // end: ToSigmoid

        // ----------------------------------------------       the DERIVES

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: derives the internal 'MatrixILGPU'. Makes only sense 
        /// after 'ToSigmoid'.
        /// </summary>
        /// <returns>sigmoid derived 'MatrixILGPU'</returns>
        public MatrixILGPU DeriveSigmoid( )
        {
            MatrixILGPU target = new(sizeX, sizeY, 0);

            actionDeriveSigmoid_any(
                dataIl.Extent.ToIntIndex(),
                dataIl,
                target.dataIl );
            accelerator.Synchronize();

            target.data = target.dataIl.GetAsArray2D();

            return ( target );

        }   // end: DeriveSigmoid

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">position in the 'MatrixILGPU'</param>
        /// <param name="source">source 'dataIL'</param>
        /// <param name="target">target 'dataIL'</param>
        public static void DeriveSigmoid_any_Kernel(
                Index2D index,
                ArrayView2D<double, Stride2D.DenseX> source,
                ArrayView2D<double, Stride2D.DenseX> target
            )
        {
            target[ index.X, index.Y ] =
                        source[ index.X, index.Y ] *
                        ( 1 - source[ index.X, index.Y ] );

        }   // end: DeriveSigmoid_instance_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: derives the given 'MatrixILGPU'. Makes only sense after 'ToSigmoid'.
        /// </summary>
        /// <param name="sigMatrix">input 'MatrixILGPU'</param>
        /// <returns>sigmoid derived 'MatrixILGPU'</returns>
        public static MatrixILGPU DeriveSigmoid( MatrixILGPU sigMatrix )
        {
            MatrixILGPU target = new(sigMatrix.sizeX, sigMatrix.sizeY, 0);

            sigMatrix.actionDeriveSigmoid_any(
                sigMatrix.dataIl.Extent.ToIntIndex(),
                sigMatrix.dataIl,
                target.dataIl );
            sigMatrix.accelerator.Synchronize();

            target.data = target.dataIl.GetAsArray2D();

            return ( target );

        }   // end: DeriveSigmoid

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: derives the given 'MatrixILGPU'. 
        /// Makes only sense after 'ToSigmoid'.
        /// Delivers into target 'MatrixILGPU' ( best speed ).
        /// </summary>
        /// <param name="sigMatrix">input 'MatrixILGPU'</param>
        /// <param name="target">sigmoid derived 'MatrixILGPU'</param>
        public static void DeriveSigmoid( MatrixILGPU sigMatrix, MatrixILGPU target )
        {
            if ( ( sigMatrix.sizeX != target.sizeX )
                    || ( sigMatrix.sizeY != target.sizeY ) )
                throw new ArgumentException(
                    "MatrixILGPU.DeriveSigmoid: incompatible sizes of the two matrices, Abort!",
                    "( ( sigMatrix.sizeX != target.sizeX )\r\n" +
                    "|| ( sigMatrix.sizeY != target.sizeY ) )" );

            sigMatrix.actionDeriveSigmoid_any(
                sigMatrix.dataIl.Extent.ToIntIndex(),
                sigMatrix.dataIl,
                target.dataIl );
            sigMatrix.accelerator.Synchronize();

        }   // end: DeriveSigmoid

        // ----------------------------------------------       MS_SUM

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: root out of the elemental's sum ( mean square sum ) 
        /// for the error calculation.
        /// </summary>
        /// <returns>the absolute value of the error</returns>
        public double MS_Sum( )
        {
            double sum = 0;

            actionMeanSquare_any(
                dataIl.Extent.ToIntIndex(),
                dataIl,
                sum );
            accelerator.Synchronize();

            double sqrtsum = Math.Sqrt(sum);
            return ( sum );

        }   // end: MS_Sum

        /// <summary>
        /// ILGPU: kernel for the function
        /// </summary>
        /// <param name="index">position in 'MatrixILGPU'</param>
        /// <param name="matrix">source 'dataIL'</param>
        /// <param name="sum">mean square sum</param>
        public static void MeanSquare_any_Kernel(
                Index2D index,
                ArrayView2D<double, Stride2D.DenseX> matrix,
                double sum
            )
        {
            sum += matrix[ index.X, index.Y ] * matrix[ index.X, index.Y ];

        }   // end: MeanSquare_any_Kernel

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Root out of the elemental's sum ( mean square sum ) 
        /// for the error calculation.
        /// </summary>
        /// <param name="matrixILGPU">source 'MatrixILGPU'</param>
        /// <returns>the absolute value of the error</returns>

        public static double MS_Sum( MatrixILGPU matrixILGPU )
        {
            double sum = 0;

            matrixILGPU.actionMeanSquare_any(
                matrixILGPU.dataIl.Extent.ToIntIndex(),
                matrixILGPU.dataIl,
                sum );
            matrixILGPU.accelerator.Synchronize();

            sum = Math.Sqrt(sum);
            return ( sum );

        }   // end: MS_Sum

        /// <summary>
        /// <para>
        /// ILGPU: this function calls the 'Action' for the needed kernel.
        /// Special with 'static': you use the given instances from
        /// the parameters.
        /// </para>
        /// in terms of content: Root out of the elemental's sum ( mean square sum ) 
        /// for the error calculation.
        /// Delivered to the target ( best speed ).
        /// </summary>
        /// <param name="matrixILGPU">source 'MatrixILGPU'</param>
        /// <param name="sum">the result</param>
        public static void MS_Sum( MatrixILGPU matrixILGPU, double sum )
        {
            sum = 0;

            matrixILGPU.actionMeanSquare_any(
                matrixILGPU.dataIl.Extent.ToIntIndex(),
                matrixILGPU.dataIl,
                sum );
            matrixILGPU.accelerator.Synchronize();

            sum = Math.Sqrt(sum);

        }   // end: MS_Sum

        // ---------------------------------------      Synchronize's

        /// <summary>
        /// For the speed you have to synchronize manually.
        /// </summary>
        public void SynchronizeCPU()
        {
            dataIl.CopyToCPU( data );

        }   // end: SynchronizeCPU

        /// <summary>
        /// For the speed you have to synchronize manually.
        /// </summary>
        /// <param name="matrix">data to be treated</param>
        public static void SynchronizeCPU( MatrixILGPU matrix )
        {
            matrix.dataIl.CopyToCPU( matrix.data );

        }   // end: SynchronizeCPU

        /// <summary>
        /// For the speed you have to synchronize manually.
        /// </summary>
        public void SynchronizeGPU()
        {
            dataIl.CopyFromCPU( data );

        }   // end: SynchronizeGPU

        /// <summary>
        /// For the speed you have to synchronize manually.
        /// </summary>
        /// <param name="matrix">data to be treated</param>
        public static void SynchronizeGPU( MatrixILGPU matrix )
        {
            matrix.dataIl.CopyFromCPU( matrix.data );

        }   // end: SynchronizeGPU


        // ---------------------------------------      input/output helpers

        /// <summary>
        /// Helper function to for example convert input data into network data.
        /// <para>
        /// This function produces a [ doubles.Length, 1 ]-'MatrixILGPU'
        /// </para>
        /// </summary>
        /// <param name="doubles">input field</param>
        /// <returns>the new 'MatrixILGPU'</returns>
        public static MatrixILGPU FromArray( double[] doubles )
        {
            MatrixILGPU temp = new(doubles.Length, 1, 0);
            for ( int pos = 0; pos < doubles.Length; pos++ )
                temp.data[ pos, 0 ] = doubles[ pos ];
            temp.SynchronizeGPU();

            return ( temp );

        }   // end: FromArray

        /// <summary>
        /// Helper function to for example convert input data into network data.
        /// <para>
        /// This function targets a [ doubles.Length, 1 ]-'MatrixILGPU'
        /// </para>
        /// </summary>
        /// <param name="doubles">input field</param>
        /// <param name="target">target 'MatrixILGPU'</param>
        public static void FromArray( double[] doubles, MatrixILGPU target )
        {
            if ( doubles.Length != target.sizeX )
                throw new ArgumentException(
                    "MatrixILGPU:FromArray -> incompatible size of the target matrix, Abort!",
                    "( doubles.Length != target.sizeX )" );

            for ( int pos = 0; pos < doubles.Length; pos++ )
                target.data[ pos, 0 ] = doubles[ pos ];
            target.SynchronizeGPU();

        }   // end: FromArray

        /// <summary>
        /// Helper function to for example convert input data into network data.
        /// <para>
        /// This function produces a [ 1, doubles.Length ]-Transpose-'MatrixILGPU'
        /// </para>
        /// </summary>
        /// <param name="doubles">input field</param>
        /// <returns>the new 'MatrixILGPU'</returns>
        public static MatrixILGPU FromArrayTranspose( double[] doubles )
        {
            MatrixILGPU temp = new(1, doubles.Length, 0);
            for ( int pos = 0; pos < doubles.Length; pos++ )
                temp.data[ 0, pos ] = doubles[ pos ];
            temp.SynchronizeGPU( );


            return ( temp );

        }   // end: FromArrayTranspose

        /// <summary>
        /// Helper function to for example convert input data into network data.
        /// <para>
        /// This function targets a [ 1, doubles.Length ]-Transpose-'MatrixILGPU'
        /// </para>
        /// </summary>
        /// <param name="doubles">input field</param>
        /// <returns></returns>
        /// <param name="target">the target 'MatrixILGPU'</param>
        public static void FromArrayTranspose( double[] doubles, MatrixILGPU target )
        {
            if ( doubles.Length != target.sizeY )
                throw new ArgumentException(
                    "MatrixILGPU:FromArrayTranspose -> incompatible size of the target matrix, Abort!",
                    "( doubles.Length != target.sizeY )" );

            for ( int pos = 0; pos < doubles.Length; pos++ )
                target.data[ 0, pos ] = doubles[ pos ];
            target.SynchronizeGPU();

        }   // end: FromArrayTranspose

        /// <summary>
        /// Helper function to for example convert input data into network data.
        /// <para>
        /// Writes the data of the internal 'MatrixILGPU' into a field.
        /// </para>
        /// </summary>
        /// <returns>result field</returns>
        public double[] ToArray( )
        {
            SynchronizeCPU();
            
            double[] temp = new double[sizeX * sizeY];
            for ( int posY = 0; posY < sizeY; posY++ )
                for ( int posX = 0; posX < sizeX; posX++ )
                    temp[ ( posY * sizeX ) + posX ] =
                        data[ posX, posY ];

            return ( temp );

        }   // end: ToArray

        /// <summary>
        /// Helper function to for example convert input data into network data.
        /// <para>
        /// Writes the data of the given 'MatrixILGPU' into a field.
        /// </para>
        /// </summary>
        /// <param name="matrix">source 'MatrixILGPU'</param>
        /// <returns>result field</returns>
        public static double[] ToArray( MatrixILGPU matrix )
        {
            matrix.SynchronizeCPU();

            double[] temp = new double[ matrix.sizeX * matrix.sizeY ];
            for ( int posY = 0; posY < matrix.sizeY; posY++ )
                for ( int posX = 0; posX < matrix.sizeX; posX++ )
                    temp[ ( posY * matrix.sizeX ) + posX ] =
                        matrix.data[ posX, posY ];

            return ( temp );

        }   // end: ToArray

        /// <summary>
        /// Helper function to for example convert input data into network data.
        /// <para>
        /// Writes the data of the given 'MatrixILGPU' into a target field.
        /// </para>
        /// </summary>
        /// <param name="matrix">source 'MatrixILGPU'</param>
        /// <param name="target">target field</param>
        public static void ToArray( MatrixILGPU matrix, double[] target )
        {
            if ( ( matrix.sizeX * matrix.sizeY ) != target.Length )
                throw new ArgumentException(
                    "MatrixILGPU.ToArray: element count of Matrix and array must be equal, Abort!",
                    "( ( matrix.sizeX * matrix.sizeY ) != target.Length )" );

            matrix.SynchronizeCPU();

            double[] temp = new double[ matrix.sizeX * matrix.sizeY ];
            for ( int posY = 0; posY < matrix.sizeY; posY++ )
                for ( int posX = 0; posX < matrix.sizeX; posX++ )
                    temp[ ( posY * matrix.sizeX ) + posX ] =
                        matrix.data[ posX, posY ];

        }   // end: ToArray

        // ----------------------------------------------       file functions

        /// <summary>
        /// Saves the 'MatrixILGPU' in its own file ( 'public string fileName' !).
        /// </summary>
        public void SaveMatrixLocal()
        {
            using ( var stream = File.Open( fileName, FileMode.Create ) )
            {
                using ( var writer = new BinaryWriter( stream ) )
                    SaveDataToWriter( writer );
                stream.Flush();

            }

        }   // end: SaveMatrixLocal

        /// <summary>
        /// a traditional save routine ( binary )
        /// </summary>
        /// <param name="writer">given 'BinaryWriter'</param>
        public void SaveDataToWriter( BinaryWriter writer )
        {
            SynchronizeCPU( );

            writer.Write( sizeX );
            writer.Write( sizeY );
            for ( int posX = 0; posX < sizeX; posX++ )
            {
                for ( int posY = 0; posY < sizeY; posY++ )
                {
                    writer.Write( data[ posX, posY ] );

                }

            }

        }   // end: SaveDataToWriter

        /// <summary>
        /// Loads the 'MatrixILGPU' from its own file ( 'public string fileName' !).
        /// </summary>
        public void LoadMatrixLocal( )
        {
            using ( var stream = File.Open( fileName, FileMode.Open ) )
                using ( var reader = new BinaryReader( stream ) )
                    LoadDataFromReader( reader );

        }   // end: LoadMatrixLocal

        /// <summary>
        /// A traditional load routine ( binary ).
        /// </summary>
        /// <param name="reader">given 'BinaryReader'</param>
        public void LoadDataFromReader( BinaryReader reader )
        {
            sizeX = reader.ReadInt32();
            sizeY = reader.ReadInt32();
            data = new double[ sizeX, sizeY ];
            for ( int posX = 0; posX < sizeX; posX++ )
            {
                for ( int posY = 0; posY < sizeY; posY++ )
                {
                    data[ posX, posY ] = reader.ReadDouble();

                }

            }
            // allocate the dataobject on the GPU
            dataIl = accelerator.Allocate2DDenseX<double>( new Index2D( sizeX, sizeY ) );

            SynchronizeGPU( );

        }   // end: LoadDataFromReader

    }   // end: class MatrixILGPU

}   // end: namespace MatrixFFN.Tools

