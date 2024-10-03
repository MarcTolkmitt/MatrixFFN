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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix2DimILGPU
{
    /// <summary>
    /// Beispielklasse, die ILGPU für die Matrix in 2D benutzt.
    /// Da das Beipiel der ILGPU-Macher vom GitHub vor Fehlern strotzt ( Matrix ~ speziellem Vektor ),
    /// muß das alles noch verstanden werden. So werden auch die Bezeichnungen überarbeitet werden müssen.
    /// <para/>
    /// Abzusehen ist, daß für jede vorgesehene Routine eine 'Action' für die GPU erstellt werden muß!
    /// </summary>
    public class MatIl
    {
        /// <summary>
        /// 2 Versionen: CPU und GPU
        /// </summary>
        public float[,] data;
        ArrayView2D<float, Stride2D.DenseX> dataIl;
        /// <summary>
        /// matrxi sizeX
        /// </summary>
        public int sizeX;
        /// <summary>
        /// matrix sizeY
        /// </summary>
        public int sizeY;

        Context context;
        Device device;
        Accelerator accelerator;
        /* 
         * In der Praxis wird die Aktion wie eine Funktion aufgerufen.
         * Schematisch wird der explizite Kernelaufruf im Feld verwaltet.
        */
        Action<Index2D,
                ArrayView2D<float, Stride2D.DenseX>,
                ArrayView2D<float, Stride2D.DenseX>,
                ArrayView2D<float, Stride2D.DenseX>> actionMatIlMultiply;


        /// <summary>
        /// Konstrujtor der Klasse, der alle GPU-Instanzen initialisiert.
        /// </summary>
        /// <param name="newSizeX">Matrixgröße X</param>
        /// <param name="newSizeY">Matrixgröße Y</param>
        public MatIl( int newSizeX, int newSizeY )
        {
            sizeX = newSizeX;
            sizeY = newSizeY;
            data = new float[ sizeX, sizeY ];
            
            context = Context.CreateDefault();
            device = context.GetPreferredDevice( false );
            accelerator = device.CreateAccelerator( context );

            // Definition der Aktion -> Laden des jew. Kernels
            actionMatIlMultiply = accelerator.LoadAutoGroupedStreamKernel<
                Index2D,
                ArrayView2D<float, Stride2D.DenseX>,
                ArrayView2D<float, Stride2D.DenseX>,
                ArrayView2D<float, Stride2D.DenseX>>(
                MatIlMultiplyKernel );

            // Anlegen des Datenobjektes auf der GPU
            dataIl = accelerator.Allocate2DDenseX<float>( new Index2D( sizeX, sizeY ) );

        }   // end: public MatIl

        /// <summary>
        /// Example-Function for the procedure
        /// </summary>
        /// <param name="data2"></param>
        /// <returns></returns>
        public float[,] MatIlMultiply( float[,] data2 )
        {
            int sizeX2 = data2.GetLength( 0 );
            int sizeY2 = data2.GetLength( 1 );
            ArrayView2D<float, Stride2D.DenseX> data2Il
                = accelerator.Allocate2DDenseX<float>( new Index2D( sizeX2, sizeY2 ) );
            ArrayView2D<float, Stride2D.DenseX> data3Il
                = accelerator.Allocate2DDenseX<float>( new Index2D( sizeX2, sizeY ) );

            dataIl.CopyFromCPU( data );
            data2Il.CopyFromCPU( data2 );

            // die Aktion führt den Kernel mit übergebenen Parametern aus ( Index, etc. )
            actionMatIlMultiply( data3Il.Extent.ToIntIndex(), dataIl, data2Il, data3Il );

            // Reads data from the GPU buffer into a new CPU array.
            // Implicitly calls accelerator.DefaultStream.Synchronize() to ensure
            // that the kernel and memory copy are completed first.
            return ( data3Il.GetAsArray2D() );

        }   // end: public float[,] MatIlMultiply

        /// <summary>
        /// Hier als Kernel bezeichnet, da im Threadfeld diese Funktion für alle Threads gilt 
        /// und über den 'index' lokalisiert wird.
        /// <para/>
        /// Hier darf ruhig mit einer Vereinfachung gearbeitet werden: die Länge der gleichen Kante
        /// für das Falksche-Schema für die Eleganz...
        /// </summary>
        /// <param name="index"></param>
        /// <param name="aView"></param>
        /// <param name="bView"></param>
        /// <param name="cView"></param>
        static void MatIlMultiplyKernel(
            Index2D index,
            ArrayView2D<float, Stride2D.DenseX> aView,
            ArrayView2D<float, Stride2D.DenseX> bView,
            ArrayView2D<float, Stride2D.DenseX> cView )
        {
            // Funktion auf der GPU, die ganz normal die Operation erledigt
            // Wie bei 'ParallelFor' wird im 'Index2D index' die Position im Threadfeld übergeben
            //index.X
            //index.Y

        }   // end: static void MatIlMultiplyKernel


    }   // end: class MatIl

}   // end: namespace Matrix2DimILGPU
/*
// nimmt nur einen String an
throw new Exception(

// praktischer, da 2 Strings benutzt werden können
throw new ArgumentException(
    "Matrix.SubtractMatrix: unterschiedliche Größen der beiden Matixen, Abbruch!",
        "shape( m1 ) != shape( m2 )" );


Action<
    Index2D,
    ArrayView2D<double, Stride2D.DenseX>,
    ArrayView2D<double, Stride2D.DenseX>,
    double 
    > actionAddScalar_Matrix;

actionAddScalar_Matrix = accelerator.LoadAutoGroupedStreamKernel<
    Index2D,
    ArrayView2D<double, Stride2D.DenseX>,
    ArrayView2D<double, Stride2D.DenseX>,
    double
    >( AddScalar_MatrixKernel );

matrix.actionAddScalar_Matrix(
    matrix.dataIl.Extent.ToIntIndex(),
    matrix.dataIl,
    temp.dataIl,
    value );
*/
