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
    /// This class implements the matrixcalculations for the 
    /// neuronal net. But these functions can be used completly freely.
    /// <para>
    /// Most important here is the multiplication of two matrices
    /// using the Falk-scheme.
    /// </para>
    /// <para>The functions are done in three different ways:</para>
    /// <para>- operation on the interal 'Matrix'</para>
    /// <para>- static Matrix function: operation on returned 'Matrix'</para>
    /// <para>- static void function: operation on targeted 'Matrix' ( best speed ) </para>
    /// </summary>
    public class Matrix
    {
        /// <summary>
        /// created on: 07.07.2023
        /// last edit: 02.10.24
        /// </summary>
        public Version version = new Version("1.1.8");

        /// <summary>
        /// data of the matrix
        /// </summary>
        public double[,] data;
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
        public string fileName = "Matrix";

        // -----------------------------------        the constructors

        /// <summary>
        /// parameterless construktor 
        /// </summary>
        public Matrix()
        {
            // minimalmatrix [ 1 x 1 ]
            sizeX = 1;
            sizeY = 1;
            data = new double[ sizeX, sizeY ];

        }   // end: Matrix ( constructor )

        /// <summary>
        /// constructor, filling the 'Matrix' with random
        /// values between [ -1, 1 ].
        /// <para>You can use your own spans.</para>
        /// </summary>
        /// <param name="inX">X-size of the 'Matrix'</param>
        /// <param name="inY">Y-size of the 'Matrix'</param>
        /// <param name="min">lower threshold for the random numbers</param>
        /// <param name="max">upper threshold for the random numbers</param>
        public Matrix(int inX, int inY, double min = -1, double max = 1)
        {
            Random zufall = new Random();

            sizeX = inX;
            sizeY = inY;
            data = new double[sizeX, sizeY];
            for (int posX = 0; posX < sizeX; posX++)
            {
                for (int posY = 0; posY < sizeY; posY++)
                {
                    data[posX, posY] =
                        min + (zufall.NextDouble() * (max - min));

                }

            }

        }   // end: Matrix ( constructor )

        /// <summary>
        /// constructor setting a special value for all elements.
        /// <para>The fastest done is the zero-matrix.</para>
        /// </summary>
        /// <param name="inX">X-size of the 'Matrix'</param>
        /// <param name="inY">Y-size of the 'Matrix'</param>
        /// <param name="val">your element value</param>
        public Matrix(int inX, int inY, double val)
        {
            sizeX = inX;
            sizeY = inY;
            data = new double[sizeX, sizeY];
            if (val == 0)
                return;
            for (int posX = 0; posX < sizeX; posX++)
            {
                for (int posY = 0; posY < sizeY; posY++)
                {
                    data[posX, posY] = val;

                }

            }

        }   // end: Matrix ( constructor )

        /// <summary>
        /// This constructor loads his data from a BinaryReader
        /// and initialisates himself with them.
        /// </summary>
        /// <param name="reader">a 'BinaryReader'</param>
        public Matrix(BinaryReader reader)
        {
            sizeX = 1;
            sizeY = 1;
            data = new double[ sizeX, sizeY ];

            LoadDataFromReader( reader );

        }   // end: Matrix ( constructor )

        // -------------------------------------        output functions

        /// <summary>
        /// standard output of the 'Matrix'.
        /// </summary>
        /// <returns>stringrepräsentation of the 'Matrix'</returns>
        override
        public string ToString()
        {
            string meldung = $"Output of the 'Matrix' [ {sizeX}, {sizeY} ]\n";
            string text;
            for (int posY = 0; posY < sizeY; posY++)
            {
                text = "[ ";
                for (int posX = 0; posX < sizeX; posX++)
                    text += data[posX, posY] + " ";
                text += "]\n";
                meldung += text;

            }
            meldung += "--------------------------------------------------\n";
            return (meldung);

        }   // end: ToString

        /// <summary>
        /// Gives the 'Matrix' via ToString() to standard output.
        /// </summary>
        public void Print()
        {
            System.Console.WriteLine(ToString());

        }   // end: Print

        // --------------------------------------------     the ADDS

        /// <summary>
        /// Add the 'value' to each element
        /// of the internal 'Matrix'.
        /// </summary>
        /// <param name="value">value to add</param>
        public void AddScalar(double value)
        {
            for (int posX = 0; posX < sizeX; posX++)
            {
                for (int posY = 0; posY < sizeY; posY++)
                {
                    data[posX, posY] += value;

                }

            }

        }   // end: AddScalar

        /// <summary>
        /// Add the 'value' to each element
        /// of the internal 'Matrix'.
        /// </summary>
        /// <param name="matrix">source</param>
        /// <param name="value">value to add</param>
        /// <returns>result 'Matrix'</returns>
        public static Matrix AddScalar(Matrix matrix, double value)
        {
            Matrix temp = (Matrix)matrix;
            for (int posX = 0; posX < temp.sizeX; posX++)
            {
                for (int posY = 0; posY < temp.sizeY; posY++)
                {
                    temp.data[posX, posY] =
                        matrix.data[posX, posY] + value;

                }

            }

            return (temp);

        }   // end: AddScalar

        /// <summary>
        /// Add the 'value' to each element
        /// of the internal 'Matrix' and delivers the result
        /// to the target 'Matrix' ( best speed ).
        /// </summary>
        /// <param name="source">source</param>
        /// <param name="value">value to add</param>
        /// <param name="target">reference to the target</param>
        public static void AddScalar_Target(Matrix source, double value, Matrix target)
        {
            if ( ( source.sizeX != target.sizeX )
                    || ( source.sizeY != target.sizeY ) )
                throw new ArgumentException(
                    "Matrix.AddScalar_Target: different sizes of the matrices, Abort!",
                    "( ( source.sizeX != target.sizeX )\r\n" +
                    "|| ( source.sizeY != target.sizeY ) )" );
            for ( int posX = 0; posX < target.sizeX; posX++)
            {
                for (int posY = 0; posY < target.sizeY; posY++)
                {
                    target.data[posX, posY] =
                        source.data[posX, posY] + value;

                }

            }

        }   // end: AddScalar_Target

        /// <summary>
        /// Adds a samesized 'Matrix' to the internal.
        /// </summary>
        /// <param name="m">that to add one</param>
        public void AddMatrix(Matrix m)
        {
            if ((sizeX != m.sizeX) || (sizeY != m.sizeY))
                throw new ArgumentException(
                    "Matrix.AddMatrix: different sizes of the matrices, Abort!",
                    "( (sizeX != m.sizeX) || (sizeY != m.sizeY) )" );

            for (int posX = 0; posX < sizeX; posX++)
                for (int posY = 0; posY < sizeY; posY++)
                    data[posX, posY] += m.data[posX, posY];

        }   // end: AddMatrix

        /// <summary>
        /// Adds two samesized 'Matrix's.
        /// </summary>
        /// <param name="m1">'Matrix' 1</param>
        /// <param name="m2">'Matrix' 2</param>
        /// <returns>resulting 'Matrix'</returns>
        public static Matrix AddMatrix(Matrix m1, Matrix m2)
        {
            if ((m1.sizeX != m2.sizeX) || (m1.sizeY != m2.sizeY))
                throw new ArgumentException(
                    "Matrix.AddMatrix: different sizes of the matrices, Abort!",
                        "( (sizeX != m.sizeX) || (sizeY != m.sizeY) )" );

            Matrix temp = new Matrix(m1.sizeX, m1.sizeY, 0);

            for (int posX = 0; posX < temp.sizeX; posX++)
                for (int posY = 0; posY < temp.sizeY; posY++)
                    temp.data[posX, posY] =
                        m1.data[posX, posY] + m2.data[posX, posY];

            return (temp);

        }   // end: AddMatrix

        /// <summary>
        /// Adds two samesized 'Matrix's.
        /// </summary>
        /// <param name="m1">'Matrix' 1</param>
        /// <param name="m2">'Matrix' 2</param>
        /// <param name="target">target 'Matrix'</param>
        public static void AddMatrix(Matrix m1, Matrix m2, Matrix target)
        {
            bool weiter = true;
            if ((m1.sizeX != m2.sizeX) || (m1.sizeY != m2.sizeY))
                weiter = false;
            if ((m1.sizeX != target.sizeX) || (m1.sizeY != target.sizeY))
                weiter = false;
            if ((target.sizeX != m2.sizeX) || (target.sizeY != m2.sizeY))
                weiter = false;
            if (!weiter)
                throw new ArgumentException(
                    "Matrix.AddMatrix: different sizes of the matrices, Abort!",
                    "((m1.sizeX != m2.sizeX) || (m1.sizeY != m2.sizeY))\r\n" +
                    "|| ((m1.sizeX != target.sizeX) || (m1.sizeY != target.sizeY))\r\n" +
                    "|| ((target.sizeX != m2.sizeX) || (target.sizeY != m2.sizeY))" );

            for (int posX = 0; posX < target.sizeX; posX++)
                for (int posY = 0; posY < target.sizeY; posY++)
                    target.data[posX, posY] =
                        m1.data[posX, posY] + m2.data[posX, posY];

        }   // end: AddMatrix

        // ------------------------------------------------        the SUBTRACTS

        /// <summary>
        /// Subtracts the 'value' from each element
        /// of the internal 'Matrix'.
        /// </summary>
        /// <param name="value">value to subtract</param>
        public void SubtractScalar(double value)
        {
            for (int posX = 0; posX < sizeX; posX++)
            {
                for (int posY = 0; posY < sizeY; posY++)
                {
                    data[posX, posY] -= value;

                }

            }

        }   // end: SubtractScalar

        /// <summary>
        /// Subtracts the 'value' from each element
        /// of the source 'Matrix'.
        /// </summary>
        /// <param name="matrix">source</param>
        /// <param name="value">value to subtract</param>
        /// <returns>result 'Matrix'</returns>
        public static Matrix SubtractScalar(Matrix matrix, double value)
        {
            Matrix temp = (Matrix)matrix;
            for (int posX = 0; posX < temp.sizeX; posX++)
            {
                for (int posY = 0; posY < temp.sizeY; posY++)
                {
                    temp.data[posX, posY] =
                        matrix.data[posX, posY] - value;

                }

            }

            return (temp);

        }   // end: SubtractScalar

        /// <summary>
        /// Subtracts the 'value' from each element
        /// of the internal 'Matrix' and 
        /// delivers the target ( best speed ).
        /// </summary>
        /// <param name="source">source 'Matrix'</param>
        /// <param name="value">value to subtract</param>
        /// <param name="target">target 'Matrix'</param>
        public static void SubtractScalar_Target(Matrix source, double value, Matrix target)
        {
            if ((source.sizeX != target.sizeX)
                    || (source.sizeY != target.sizeY))
                throw new ArgumentException(
                    "Matrix.SubtractScalar_Target: different sizes of the matrices, Abort!",
                    "( ( source.sizeX != target.sizeX )\r\n" +
                    "|| ( source.sizeY != target.sizeY ) )" );

            for ( int posX = 0; posX < target.sizeX; posX++)
            {
                for (int posY = 0; posY < target.sizeY; posY++)
                {
                    target.data[posX, posY] =
                        source.data[posX, posY] - value;

                }

            }

        }   // end: SubtractScalar_Target

        /// <summary>
        /// Subtracts a even sized 'Matrix' from the internal one.
        /// </summary>
        /// <param name="m">the to subtract 'Matrix'</param>
        public void SubtractMatrix(Matrix m)
        {
            if ((sizeX != m.sizeX) || (sizeY != m.sizeY))
                throw new ArgumentException(
                    "Matrix.Add: different sizes of the matrices, Abort!",
                    "( (sizeX != m.sizeX) || (sizeY != m.sizeY) )" );

            for (int posX = 0; posX < sizeX; posX++)
                for (int posY = 0; posY < sizeY; posY++)
                    data[posX, posY] -= m.data[posX, posY];

        }   // end: SubtractMatrix

        /// <summary>
        /// Subtraction of two 'Matrix's.
        /// </summary>
        /// <param name="m1">'Matrix' 1</param>
        /// <param name="m2">'Matrix' 2</param>
        /// <returns>result 'Matrix'</returns>
        public static Matrix SubtractMatrix(Matrix m1, Matrix m2)
        {
            if ((m1.sizeX != m2.sizeX) || (m1.sizeY != m2.sizeY))
                throw new ArgumentException(
                    "Matrix.SubtractMatrix: different sizes of the matrices, Abort!",
                        "( (m1.sizeX != m2.sizeX) || (m1.sizeY != m2.sizeY) )" );

            Matrix temp = new Matrix(m1.sizeX, m1.sizeY, 0);

            for (int posX = 0; posX < temp.sizeX; posX++)
                for (int posY = 0; posY < temp.sizeY; posY++)
                    temp.data[posX, posY] =
                        m1.data[posX, posY] - m2.data[posX, posY];

            return (temp);

        }   // end: SubtractMatrix

        /// <summary>
        /// Subtraction of two 'Matrix's.
        /// </summary>
        /// <param name="m1">'Matrix' 1</param>
        /// <param name="m2">'Matrix' 2</param>
        /// <param name="target">target 'Matrix'</param>
        public static void SubtractMatrix(Matrix m1, Matrix m2, Matrix target)
        {
            bool weiter = true;
            if ((m1.sizeX != m2.sizeX) || (m1.sizeY != m2.sizeY))
                weiter = false;
            if ((m1.sizeX != target.sizeX) || (m1.sizeY != target.sizeY))
                weiter = false;
            if ((target.sizeX != m2.sizeX) || (target.sizeY != m2.sizeY))
                weiter = false;
            if (!weiter)
                throw new ArgumentException(
                    "Matrix.SubtractMatrix:  different sizes of the matrices, Abort!",
                    "( (m1.sizeX != m2.sizeX) || (m1.sizeY != m2.sizeY))\r\n" +
                    "|| ((m1.sizeX != target.sizeX) || (m1.sizeY != target.sizeY))\r\n                weiter = false;\r\n            if ((target.sizeX != m2.sizeX) || (target.sizeY != m2.sizeY))" );

            for (int posX = 0; posX < target.sizeX; posX++)
                for (int posY = 0; posY < target.sizeY; posY++)
                    target.data[posX, posY] =
                        m1.data[posX, posY] - m2.data[posX, posY];

        }   // end: SubtractMatrix

        // -------------------------------------------------       the TRANSPOSES

        /// <summary>
        /// Transpones the 'Matrix'.
        /// </summary>
        /// <param name="m">input 'Matrix'</param>
        /// <returns>result 'Matrix' [ m.sizeY, m.sizeX ]</returns>
        public static Matrix Transpose(Matrix m)
        {
            Matrix temp = new Matrix(m.sizeY, m.sizeX, 0);
            for (int posX = 0; posX < m.sizeX; posX++)
                for (int posY = 0; posY < m.sizeY; posY++)
                    temp.data[posY, posX] = m.data[posX, posY];

            return (temp);

        }   // end: Transpose

        /// <summary>
        /// Transpones the 'Matrix'.
        /// </summary>
        /// <param name="source">source 'Matrix'</param>
        /// <param name="target">target 'Matrix' [ m.sizeY, m.sizeX ]</param>
        public static void Transpose(Matrix source, Matrix target)
        {
            if ((source.sizeX != target.sizeY)
                || (source.sizeY != target.sizeX))
                throw new ArgumentException(
                    "Matrix.Transpose: incompatible sizes of the two matrices, Abort!",
                    "( (source.sizeX != target.sizeY)\r\n" +
                    "|| (source.sizeY != target.sizeX) )" );

            for (int posX = 0; posX < source.sizeX; posX++)
                for (int posY = 0; posY < source.sizeY; posY++)
                    target.data[posY, posX] = source.data[posX, posY];

        }   // end: Transpose

        // ------------------------------------------------------    die MULTIPLYS

        /// <summary>
        /// Multiplies every matrixelement with a value.
        /// </summary>
        /// <param name="value">the multiplication's value</param>
        public void MultiplyScalar(double value)
        {
            for (int posX = 0; posX < sizeX; posX++)
                for (int posY = 0; posY < sizeY; posY++)
                    data[posX, posY] *= value;

        }   // end: MultiplyScalar

        /// <summary>
        /// Multiplies every matrixelement with a value.
        /// </summary>
        /// <param name="source">source 'Matrix'</param>
        /// <param name="value">the multiplication's value</param>
        /// <returns>result 'Matrix'</returns>
        public static Matrix MultiplyScalar(Matrix source, double value)
        {
            Matrix target = new Matrix(source.sizeX, source.sizeY, 0);

            for (int posX = 0; posX < target.sizeX; posX++)
                for (int posY = 0; posY < target.sizeY; posY++)
                    target.data[posX, posY] =
                        source.data[posX, posY] * value;

            return (target);

        }   // end: MultiplyScalar

        /// <summary>
        /// Multiplies every matrixelement with a value and
        /// delivers it into the target'Matrix' ( best speed ).
        /// </summary>
        /// <param name="source">source 'Matrix'</param>
        /// <param name="value">the multiplication's value</param>
        /// <param name="target">target 'Matrix'</param>
        public static void MultiplyScalar(Matrix source, double value,
            Matrix target)
        {
            if ((source.sizeX != target.sizeX)
                || (source.sizeY != target.sizeY))
                throw new ArgumentException(
                    "Matrix.MultiplyScalar: incompatible sizes of the two matrices, Abort!",
                    "( (source.sizeX != target.sizeX)\r\n" +
                    "|| (source.sizeY != target.sizeY) )" );


            for (int posX = 0; posX < target.sizeX; posX++)
                for (int posY = 0; posY < target.sizeY; posY++)
                    target.data[posX, posY] =
                        source.data[posX, posY] * value;

        }   // end: MultiplyScalar

        /// <summary>
        /// Multiplies the input 'Matrix' to the internal if 
        /// they have the same size.
        /// </summary>
        /// <param name="m">input 'Matrix'</param>
        public void MultiplySameSize(Matrix m)
        {
            if ((sizeX != m.sizeX) || (sizeY != m.sizeY))
                throw new ArgumentException(
                    "Matrix.MultiplySameSize: incompatible sizes of the two matrices, Abort!",
                    "( (sizeX != m.sizeX) || (sizeY != m.sizeY) )" );

            for (int posX = 0; posX < m.sizeX; posX++)
                for (int posY = 0; posY < m.sizeY; posY++)
                    data[posX, posY] *= m.data[posX, posY];

        }   // end: MultiplySameSize

        /// <summary>
        /// Multiplies the input 'Matrix' to the internal if 
        /// they have the same size.
        /// </summary>
        /// <param name="m1">'Matrix' 1</param>
        /// <param name="m2">'Matrix' 2</param>
        /// <returns>result 'Matrix'</returns>
        public static Matrix MultiplySameSize(Matrix m1, Matrix m2)
        {
            if ((m1.sizeX != m2.sizeX) || (m1.sizeY != m2.sizeY))
                throw new ArgumentException(
                    "Matrix.MultiplySameSize: incompatible sizes of the two matrices, Abort!",
                        "( (m1.sizeX != m2.sizeX) || (m1.sizeY != m2.sizeY) )" );

            Matrix target = (Matrix)m1;
            for (int posX = 0; posX < target.sizeX; posX++)
                for (int posY = 0; posY < target.sizeY; posY++)
                    target.data[posX, posY] *= m2.data[posX, posY];

            return (target);

        }   // end: MultiplySameSize

        /// <summary>
        /// Multiplies the input 'Matrix' to the internal if 
        /// they have the same size. result will be written
        /// into the target 'Matrix' ( best speed ).
        /// </summary>
        /// <param name="m1">'Matrix' 1</param>
        /// <param name="m2">'Matrix' 2</param>
        /// <param name="target">target 'Matrix'</param>
        public static void MultiplySameSize(Matrix m1, Matrix m2,
                Matrix target)
        {
            bool weiter = true;
            if ((m1.sizeX != m2.sizeX) || (m1.sizeY != m2.sizeY))
                weiter = false;
            if ((m1.sizeX != target.sizeX) || (m1.sizeY != target.sizeY))
                weiter = false;
            if ((target.sizeX != m2.sizeX) || (target.sizeY != m2.sizeY))
                weiter = false;
            if (!weiter)
                throw new ArgumentException(
                    "Matrix.MultiplySameSize: incompatible sizes of the three matrices, Abort!",
                    "( (m1.sizeX != m2.sizeX) || (m1.sizeY != m2.sizeY) )\r\n" +
                    "|| ( (m1.sizeX != target.sizeX) || (m1.sizeY != target.sizeY) )\r\n" +
                    "|| ( (target.sizeX != m2.sizeX) || (target.sizeY != m2.sizeY) )" );

            for (int posX = 0; posX < target.sizeX; posX++)
                for (int posY = 0; posY < target.sizeY; posY++)
                    target.data[posX, posY] =
                        m1.data[posX, posY] * m2.data[posX, posY];

        }   // end: MultiplySameSize

        /// <summary>
        /// the heartpiece: the 'crossprodukt' using the Falk-scheme on both 'Matrix's.
        /// <para>
        /// Info: ( m1.sizeX == m2.sizeY ) 'AND' target[ m2.sizeX, m1.sizeY ]
        /// </para>
        /// </summary>
        /// <param name="m1">'Matrix' 1</param>
        /// <param name="m2">'Matrix' 2</param>
        /// <returns>result 'Matrix'</returns>
        public static Matrix Multiply(Matrix m1, Matrix m2)
        {
            if (m1.sizeX != m2.sizeY)
                throw new ArgumentException(
                    "Matrix.Multiply: relational side of both 'Matrix's must be the same, Abort!",
                        "(m1.sizeX != m2.sizeY)" );

            Matrix target = new Matrix(m2.sizeX, m1.sizeY, 0);
            for (int posX = 0; posX < target.sizeX; posX++)
                for (int posY = 0; posY < target.sizeY; posY++)
                {
                    double sum = 0;
                    for (int zeiger = 0; zeiger < m1.sizeX; zeiger++)
                        sum +=
                            m1.data[zeiger, posY]
                            * m2.data[posX, zeiger];
                    target.data[posX, posY] = sum;

                }

            return (target);

        }   // end: Multiply

        /// <summary>
        /// the heartpiece: the 'crossprodukt' using the Falk-scheme on both 'Matrix's.
        /// Delivers to a target 'Matrix' ( best speed ).
        /// <para>
        /// Info: ( m1.sizeX == m2.sizeY ) 'AND' target[ m2.sizeX, m1.sizeY ]
        /// </para>
        /// </summary>
        /// <param name="m1">'Matrix' 1</param>
        /// <param name="m2">'Matrix' 2</param>
        /// <param name="target">result 'Matrix'</param>
        public static void Multiply(Matrix m1, Matrix m2,
                Matrix target)
        {
            bool weiter = true;
            if (m1.sizeX != m2.sizeY)
                weiter = false;
            if (target.sizeX != m2.sizeX)
                weiter = false;
            if (target.sizeY != m1.sizeY)
                weiter = false;
            if (!weiter)
                throw new ArgumentException(
                    "Matrix.Multiply: relational side of the three 'Matrix's must be the same, Abort!",
                    "(m1.sizeX != m2.sizeY)\r\n" +
                    "|| (target.sizeX != m2.sizeX)\r\n" +
                    "|| (target.sizeY != m1.sizeY)" );

            for (int posX = 0; posX < target.sizeX; posX++)
                for (int posY = 0; posY < target.sizeY; posY++)
                {
                    double sum = 0;
                    for (int zeiger = 0; zeiger < m1.sizeX; zeiger++)
                        sum +=
                            m1.data[zeiger, posY]
                            * m2.data[posX, zeiger];
                    target.data[posX, posY] = sum;

                }

        }   // end: Multiply

        // ----------------------------------------------       the SIGMOIDS

        /// <summary>
        /// Activationfunction is the unsymetric logistic function - 
        /// the internal 'Matrix' will be altered.
        /// </summary>
        public void ToSigmoid()
        {
            for (int posX = 0; posX < sizeX; posX++)
                for (int posY = 0; posY < sizeY; posY++)
                {
                    double temp = data[posX, posY];
                    data[posX, posY] =
                        1 / (1 + Math.Exp(-temp));

                }

        }   // end: ToSigmoid

        /// <summary>
        /// Activationfunction is the unsymetric logistic function - 
        /// returns the result 'Matrix'.
        /// </summary>
        /// <param name="source">input 'Matrix'</param>
        /// <returns>the sigmoid result 'Matrix'</returns>
        public static Matrix ToSigmoid(Matrix source)
        {
            Matrix target = (Matrix)source;
            for (int posX = 0; posX < target.sizeX; posX++)
                for (int posY = 0; posY < target.sizeY; posY++)
                {
                    double temp = target.data[posX, posY];
                    target.data[posX, posY] =
                        1 / (1 + Math.Exp(-temp));
                }

            return (target);

        }   // end: ToSigmoid

        /// <summary>
        /// Activationfunction is the unsymetric logistic function - 
        /// returns the result 'Matrix' into target ( best speed ).
        /// </summary>
        /// <param name="source">input 'Matrix'</param>
        /// <param name="target">the sigmoid result 'Matrix'</param>
        public static void ToSigmoid(Matrix source, Matrix target)
        {
            if ((source.sizeX != target.sizeX)
                || (source.sizeY != target.sizeY))
                throw new ArgumentException(
                    "Matrix.ToSigmoid: incompatible sizes of the two matrices, Abort!",
                    "( (source.sizeX != target.sizeX)\r\n" +
                    "|| (source.sizeY != target.sizeY) )" );

            for (int posX = 0; posX < target.sizeX; posX++)
                for (int posY = 0; posY < target.sizeY; posY++)
                {
                    double temp = source.data[posX, posY];
                    target.data[posX, posY] =
                        1 / (1 + Math.Exp(-temp));
                }

        }   // end: ToSigmoid

        // ----------------------------------------------       the DERIVES

        /// <summary>
        /// Derives the internal 'Matrix'. Makes only sense after 'ToSigmoid'.
        /// </summary>
        /// <returns>sigmoid derived 'Matrix'</returns>
        public Matrix DeriveSigmoid()
        {
            Matrix temp = new Matrix(sizeX, sizeY, 0);
            for (int posX = 0; posX < sizeX; posX++)
                for (int posY = 0; posY < sizeY; posY++)
                    temp.data[posX, posY] =
                        data[posX, posY] *
                        (1 - data[posX, posY]);

            return (temp);

        }   // end: DeriveSigmoid

        /// <summary>
        /// Derives the given 'Matrix'. Makes only sense after 'ToSigmoid'.
        /// </summary>
        /// <param name="sigMatrix">input 'Matrix'</param>
        /// <returns>sigmoid derived 'Matrix'</returns>
        public static Matrix DeriveSigmoid(Matrix sigMatrix)
        {
            Matrix target = new Matrix(sigMatrix.sizeX, sigMatrix.sizeY, 0);
            for (int posX = 0; posX < sigMatrix.sizeX; posX++)
                for (int posY = 0; posY < sigMatrix.sizeY; posY++)
                    target.data[posX, posY] =
                        sigMatrix.data[posX, posY] *
                        (1 - sigMatrix.data[posX, posY]);

            return (target);

        }   // end: DeriveSigmoid

        /// <summary>
        /// Derives the given 'Matrix'. Makes only sense after 'ToSigmoid'.
        /// Delivers into target 'Matrix' ( best speed ).
        /// </summary>
        /// <param name="sigMatrix">input 'Matrix'</param>
        /// <param name="target">sigmoid derived 'Matrix'</param>
        public static void DeriveSigmoid(Matrix sigMatrix, Matrix target)
        {
            if ((sigMatrix.sizeX != target.sizeX)
                || (sigMatrix.sizeY != target.sizeY))
                throw new ArgumentException(
                    "Matrix.DeriveSigmoid: incompatible sizes of the two matrices, Abort!",
                    "( (sigMatrix.sizeX != target.sizeX)\r\n" +
                    "|| (sigMatrix.sizeY != target.sizeY) )" );

            for (int posX = 0; posX < sigMatrix.sizeX; posX++)
                for (int posY = 0; posY < sigMatrix.sizeY; posY++)
                    target.data[posX, posY] =
                        sigMatrix.data[posX, posY] *
                        (1 - sigMatrix.data[posX, posY]);

        }   // end: DeriveSigmoid

        // ----------------------------------------------       MS_SUM

        /// <summary>
        /// Root out of the elemental's sum ( mean square sum ) 
        /// for the error calculation.
        /// </summary>
        /// <returns>the absolute value of the error</returns>
        public double MS_Sum()
        {
            double sum = 0;
            for (int posX = 0; posX < sizeX; posX++)
                for (int posY = 0; posY < sizeY; posY++)
                    sum += data[posX, posY] * data[posX, posY];

            double sqrtsum = Math.Sqrt(sum);
            return (sum);

        }   // end: MS_Sum

        /// <summary>
        /// Root out of the elemental's sum ( mean square sum ) 
        /// for the error calculation.
        /// </summary>
        /// <param name="matrix">source 'Matrix'</param>
        /// <returns>the absolute value of the error</returns>
        public static double MS_Sum( Matrix matrix )
        {
            double sum = 0;
            for ( int posX = 0; posX < matrix.sizeX; posX++ )
                for ( int posY = 0; posY < matrix.sizeY; posY++ )
                    sum += matrix.data[ posX, posY ] * matrix.data[ posX, posY ];

            double sqrtsum = Math.Sqrt(sum);
            return ( sum );

        }   // end: MS_Sum

        /// <summary>
        /// Root out of the elemental's sum ( mean square sum ) 
        /// for the error calculation.
        /// Delivered to the target ( best speed ).
        /// </summary>
        /// <param name="matrix">source 'Matrix'</param>
        /// <param name="sum">the result</param>
        public static void MS_Sum( Matrix matrix, double sum )
        {
            sum = 0;
            for ( int posX = 0; posX < matrix.sizeX; posX++ )
                for ( int posY = 0; posY < matrix.sizeY; posY++ )
                    sum += matrix.data[ posX, posY ] * matrix.data[ posX, posY ];

            sum = Math.Sqrt(sum);

        }   // end: MS_Sum

        // ---------------------------------------      input/output helpers

        /// <summary>
        /// Helper function to for example convert input data into network data.
        /// <para>
        /// This function produces a [ doubles.Length, 1 ]-'Matrix'
        /// </para>
        /// </summary>
        /// <param name="doubles">input field</param>
        /// <returns>the new 'Matrix'</returns>
        public static Matrix FromArray(double[] doubles)
        {
            Matrix temp = new Matrix(doubles.Length, 1, 0);
            for (int pos = 0; pos < doubles.Length; pos++)
                temp.data[pos, 0] = doubles[pos];

            return (temp);

        }   // end: FromArray

        /// <summary>
        /// Helper function to for example convert input data into network data.
        /// <para>
        /// This function targets a [ doubles.Length, 1 ]-'Matrix'
        /// </para>
        /// </summary>
        /// <param name="doubles">input field</param>
        /// <param name="target">target 'Matrix'</param>
        public static void FromArray(double[] doubles, Matrix target)
        {
            if (doubles.Length != target.sizeX)
                throw new ArgumentException(
                    "Matrix:FromArray -> incompatible size of the target matrix, Abort!",
                    "( doubles.Length != target.sizeX )" );

            for (int pos = 0; pos < doubles.Length; pos++)
                target.data[pos, 0] = doubles[pos];

        }   // end: FromArray

        /// <summary>
        /// Helper function to for example convert input data into network data.
        /// <para>
        /// This function produces a [ 1, doubles.Length ]-Transpose-'Matrix'
        /// </para>
        /// </summary>
        /// <param name="doubles">input field</param>
        /// <returns>the new 'Matrix'</returns>
        public static Matrix FromArrayTranspose(double[] doubles)
        {
            Matrix temp = new Matrix(1, doubles.Length, 0);
            for (int pos = 0; pos < doubles.Length; pos++)
                temp.data[0, pos] = doubles[pos];

            return (temp);

        }   // end: FromArrayTranspose

        /// <summary>
        /// Helper function to for example convert input data into network data.
        /// <para>
        /// This function targets a [ 1, doubles.Length ]-Transpose-'Matrix'
        /// </para>
        /// </summary>
        /// <param name="doubles">input field</param>
        /// <returns></returns>
        /// <param name="target">the target 'Matrix'</param>
        public static void FromArrayTranspose(double[] doubles, Matrix target)
        {
            if (doubles.Length != target.sizeY)
                throw new ArgumentException(
                    "Matrix:FromArray -> incompatible size of the target matrix, Abort!",
                    "( doubles.Length != target.sizeY )" );

            for (int pos = 0; pos < doubles.Length; pos++)
                target.data[0, pos] = doubles[pos];

        }   // end: FromArrayTranspose

        /// <summary>
        /// Helper function to for example convert input data into network data.
        /// <para>
        /// Writes the data of the internal 'Matrix' into a field.
        /// </para>
        /// </summary>
        /// <returns>result field</returns>
        public double[] ToArray()
        {
            double[] temp = new double[sizeX * sizeY];
            for (int posY = 0; posY < sizeY; posY++)
                for (int posX = 0; posX < sizeX; posX++)
                    temp[(posY * sizeX) + posX] =
                        data[posX, posY];

            return (temp);

        }   // end: ToArray

        /// <summary>
        /// Helper function to for example convert input data into network data.
        /// <para>
        /// Writes the data of the given 'Matrix' into a field.
        /// </para>
        /// </summary>
        /// <param name="matrix">source 'Matrix'</param>
        /// <returns>result field</returns>
        public static double[] ToArray( Matrix matrix )
        {

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
        /// Writes the data of the given 'Matrix' into a target field.
        /// </para>
        /// </summary>
        /// <param name="matrix">source 'Matrix'</param>
        /// <param name="target">target field</param>
        public static void ToArray( Matrix matrix, double[] target )
        {
            if ( ( matrix.sizeX * matrix.sizeY ) != target.Length )
                throw new ArgumentException(
                    "Matrix.ToArray: element count of Matrix and array must be equal, Abort!",
                    "( ( matrix.sizeX * matrix.sizeY ) != target.Length )" );

            double[] temp = new double[ matrix.sizeX * matrix.sizeY ];
            for ( int posY = 0; posY < matrix.sizeY; posY++ )
                for ( int posX = 0; posX < matrix.sizeX; posX++ )
                    temp[ ( posY * matrix.sizeX ) + posX ] =
                        matrix.data[ posX, posY ];

        }   // end: ToArray

        // ----------------------------------------------       file functions

        /// <summary>
        /// Saves the 'Matrix' in its own file ( 'public string fileName' !).
        /// </summary>
        public void SaveMatrixLocal( )
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
        public void SaveDataToWriter(BinaryWriter writer)
        {
            writer.Write(sizeX);
            writer.Write(sizeY);
            for (int posX = 0; posX < sizeX; posX++)
            {
                for (int posY = 0; posY < sizeY; posY++)
                {
                    writer.Write(data[posX, posY]);

                }

            }

        }   // end: SaveDataToWriter

        /// <summary>
        /// Loads the 'Matrix' from its own file ( 'public string fileName' !).
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
        public void LoadDataFromReader( BinaryReader reader)
        {
            sizeX = reader.ReadInt32();
            sizeY = reader.ReadInt32();
            data = new double[sizeX, sizeY];
            for (int posX = 0; posX < sizeX; posX++)
            {
                for (int posY = 0; posY < sizeY; posY++)
                {
                    data[posX, posY] = reader.ReadDouble();

                }

            }

        }   // end: LoadDataFromReader

    }   // end: class Matrix

}   // end: namespace MatrixFFN.Tools
