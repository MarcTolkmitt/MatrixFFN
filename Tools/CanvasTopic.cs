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


// Ignore Spelling: Ints

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace MatrixFFN.Tools
{
    /// <summary>
    /// Shows the nets structure graphical in
    /// the given 'Canvas'..
    /// <para>Additionally you can open the 'CanvasWindow'
    /// to see a bigger 'Canvas' with the diagram.</para>
    /// </summary>
    public class CanvasTopic : CanvasWindow
    {
        /// <summary>
        /// created on: 08.07.2023
        /// <para>last edit: 05.10.24</para>
        /// </summary>
        public new Version version = new("1.0.12");
        /// <summary>
        /// Basic topic string - minimum for an FFN.
        /// </summary>
        public string basicTopic = "2,2,1.";
        /// <summary>
        /// Topic in the course of the program.
        /// </summary>
        public string workingTopic;
        /// <summary>
        /// The layers as int[] - 'parsed' from the
        /// topic string.
        /// </summary>
        public int[] topicField = new int[ 1 ];
        /// <summary>
        /// The 'Canvas' in the FFN-window.
        /// </summary>
        Canvas drawArea;

        /// <summary>
        /// Draws the nets structure onto a given 'Canvas'.
        /// <para>Additionally you can open a 'CanvasWindow' with the diagram.</para>
        /// </summary>
        /// <param name="windowTitle">title of the window</param>
        /// <param name="inCanvas">the to used 'Canvas'</param>
        public CanvasTopic( string windowTitle, ref Canvas inCanvas )
        {
            this.drawArea = inCanvas;
            if ( windowTitle != "" ) 
                Title = windowTitle;
            else
                Title = "A view of the net.";
            workingTopic = basicTopic;
            ParseTopic( workingTopic, ref topicField );
            ShowTopic( );

        }   // end: CanvasTopic ( Constructor )

        /// <summary>
        /// Shows the nets graph in the 'canvasWindow' - 
        /// a zoomed window.
        /// </summary>
        public void ShowWindow( )
        {
            Show( );
            _canvasWindowCanvas.Children.Clear( );
            // relations based on the 'Canvas'
            double xSize = _canvasWindowCanvas.Width;
            double ySize = _canvasWindowCanvas.Height;


            if ( ( xSize == 0 ) || ( ySize == 0 ) || ( xSize == double.NaN ) || ( ySize == double.NaN ) )
                return;
            double xStep = xSize / ( topicField.Length );
            // the x-distributions
            double[] xPos = new double[topicField.Length];
            for ( int xNum = 0; xNum < xPos.Length; xNum++ )
            {
                xPos[ xNum ] = ( xStep / 2 ) + ( xNum * xStep );

            }
            // the y-distributions
            double[][] yPos = new double[3][]
                { new double[] { ySize / 2 },
                    new double[] { ( ySize / 3 ), ySize - ( ySize / 3 ) },
                    new double[] { ( ySize / 4 ), ySize - ( ySize / 4 ) } };
            // the frame
            PutCanvasWindowLine( 0, 0, xSize, 0, 1, Brushes.Gray );
            PutCanvasWindowLine( 0, 0, 0, ySize, 1, Brushes.Gray );
            PutCanvasWindowLine( xSize, 0, xSize, ySize, 1, Brushes.Gray );
            PutCanvasWindowLine( 0, ySize, xSize, ySize, 1, Brushes.Gray );

            // lines in 9 possibilities
            for ( int line = 0; line < ( xPos.Length - 1 ); line++ )
            {
                if ( topicField[ line ] == 1 )
                {   // lines from one knot
                    if ( topicField[ line + 1 ] == 1 )
                    {   // lines to one knot - 1 line
                        PutCanvasWindowLine(
                            xPos[ line ], yPos[ 0 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 0 ][ 0 ],
                            2, Brushes.Black );

                    }
                    else if ( topicField[ line + 1 ] == 2 )
                    {   // lines to two knots - 2 lines
                        PutCanvasWindowLine(
                            xPos[ line ], yPos[ 0 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 0 ],
                            2, Brushes.Black );
                        PutCanvasWindowLine(
                            xPos[ line ], yPos[ 0 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 1 ],
                            2, Brushes.Black );

                    }
                    else
                    {   // lines to many knots - 2 stroked lines
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 0 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 0 ],
                            2, Brushes.Black );
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 0 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 1 ],
                            2, Brushes.Black );

                    }
                }
                else if ( topicField[ line ] == 2 )
                {   // lines from two knots
                    if ( topicField[ line + 1 ] == 1 )
                    {   // lines to one knot - 2 lines
                        PutCanvasWindowLine(
                            xPos[ line ], yPos[ 1 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 0 ][ 0 ],
                            2, Brushes.Black );
                        PutCanvasWindowLine(
                            xPos[ line ], yPos[ 1 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 0 ][ 0 ],
                            2, Brushes.Black );

                    }
                    else if ( topicField[ line + 1 ] == 2 )
                    {   // lines to two knots - 4 lines
                        PutCanvasWindowLine(
                            xPos[ line ], yPos[ 1 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 0 ],
                            2, Brushes.Black );
                        PutCanvasWindowLine(
                            xPos[ line ], yPos[ 1 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 1 ],
                            2, Brushes.Black );
                        PutCanvasWindowLine(
                            xPos[ line ], yPos[ 1 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 0 ],
                            2, Brushes.Black );
                        PutCanvasWindowLine(
                            xPos[ line ], yPos[ 1 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 1 ],
                            2, Brushes.Black );

                    }
                    else
                    {   // lines to many knots - 4 stroked lines
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 1 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 0 ],
                            2, Brushes.Black );
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 1 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 1 ],
                            2, Brushes.Black );
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 1 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 0 ],
                            2, Brushes.Black );
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 1 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 1 ],
                            2, Brushes.Black );

                    }
                }
                else
                {   // lines from many knots
                    if ( topicField[ line + 1 ] == 1 )
                    {   // lines to one knot - 2 lines
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 2 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 0 ][ 0 ],
                            2, Brushes.Black );
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 2 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 0 ][ 0 ],
                            2, Brushes.Black );

                    }
                    else if ( topicField[ line + 1 ] == 2 )
                    {   // lines to two knots - 4 lines
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 2 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 0 ],
                            2, Brushes.Black );
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 2 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 1 ],
                            2, Brushes.Black );
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 2 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 0 ],
                            2, Brushes.Black );
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 2 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 1 ],
                            2, Brushes.Black );

                    }
                    else
                    {   // lines to many knots - 4 stroked lines
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 2 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 0 ],
                            2, Brushes.Black );
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 2 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 1 ],
                            2, Brushes.Black );
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 2 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 0 ],
                            2, Brushes.Black );
                        PutCanvasWindowLineStroked(
                            xPos[ line ], yPos[ 2 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 1 ],
                            2, Brushes.Black );

                    }
                }
            }   // end: lines in 9 possibilities
            // 3 different layers
            for ( int kreis = 0; kreis < topicField.Length; kreis++ )
            {
                if ( topicField[ kreis ] == 1 )
                {   // one knot + number
                    PutCanvasWindowEllipse( xPos[ kreis ], yPos[ 0 ][ 0 ] );
                    PutCanvasWindowLabel( xPos[ kreis ],
                        yPos[ 0 ][ 0 ], topicField[ kreis ].ToString( ) );

                }
                else if ( topicField[ kreis ] == 2 )
                {   // two knots + number
                    PutCanvasWindowEllipse( xPos[ kreis ], yPos[ 1 ][ 0 ] );
                    PutCanvasWindowEllipse( xPos[ kreis ], yPos[ 1 ][ 1 ] );
                    PutCanvasWindowLabel( xPos[ kreis ],
                        yPos[ 1 ][ 0 ], topicField[ kreis ].ToString( ) );

                }
                else
                {   // many knots + number
                    // first the stroked line in the middle
                    PutCanvasWindowLineStroked( xPos[ kreis ], yPos[ 2 ][ 0 ],
                        xPos[ kreis ], yPos[ 2 ][ 1 ],
                            2, Brushes.Black );
                    // now the knots and the number
                    PutCanvasWindowEllipse( xPos[ kreis ], yPos[ 2 ][ 0 ] );
                    PutCanvasWindowEllipse( xPos[ kreis ], yPos[ 2 ][ 1 ] );
                    PutCanvasWindowLabel( xPos[ kreis ],
                        yPos[ 2 ][ 0 ], topicField[ kreis ].ToString( ) );

                }

            }   // end: 3 different layers

        }   // end: ShowWindow

        /// <summary>
        /// Shows the nets diagram in the 
        /// 'drawArea'.
        /// </summary>
        public void ShowTopic()
        {
            drawArea.Children.Clear( );
            // the 'Canvas' defines the relations
            double xSize = drawArea.Width;
            double ySize = drawArea.Height;
            

            if ( ( xSize == 0 ) || ( ySize == 0 ) || ( xSize == double.NaN ) || ( ySize == double.NaN ) )
                return;
            double xStep = xSize / ( topicField.Length );
            // the x-distributions
            double[] xPos = new double[topicField.Length];
            for ( int xNum = 0; xNum < xPos.Length; xNum++ )
            {
                xPos[ xNum ] = ( xStep / 2 )  + ( xNum * xStep );

            }
            // the y-distributions
            double[][] yPos = new double[3][]
                { new double[] { ySize / 2 },
                    new double[] { ( ySize / 3 ), ySize - ( ySize / 3 ) },
                    new double[] { ( ySize / 4 ), ySize - ( ySize / 4 ) } };
            // the frame
            PutDrawAreaLine( 0, 0, xSize, 0, 1, Brushes.Gray );
            PutDrawAreaLine( 0, 0, 0, ySize, 1, Brushes.Gray );
            PutDrawAreaLine( xSize, 0, xSize, ySize, 1, Brushes.Gray );
            PutDrawAreaLine( 0, ySize, xSize, ySize, 1, Brushes.Gray );

            // lines in 9 possibilities
            for ( int line = 0; line < ( xPos.Length - 1 ); line++ )
            {
                if ( topicField[ line ] == 1 )
                {   // lines from one knot
                    if ( topicField[ line + 1 ] == 1 )
                    {   // lines to one knot - 1 line
                        PutDrawAreaLine(
                            xPos[ line ], yPos[ 0 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 0 ][ 0 ],
                            2, Brushes.Black );

                    }
                    else if ( topicField[ line + 1 ] == 2 )
                    {   // lines to two knots - 2 lines
                        PutDrawAreaLine(
                            xPos[ line ], yPos[ 0 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 0 ],
                            2, Brushes.Black );
                        PutDrawAreaLine(
                            xPos[ line ], yPos[ 0 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 1 ],
                            2, Brushes.Black );

                    }
                    else
                    {   // lines to many knots - 2 stroked lines
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 0 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 0 ],
                            2, Brushes.Black );
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 0 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 1 ],
                            2, Brushes.Black );

                    }
                }
                else if ( topicField[ line ] == 2 )
                {   // lines from two knots
                    if ( topicField[ line + 1 ] == 1 )
                    {   // lines to one knot - 2 lines
                        PutDrawAreaLine(
                            xPos[ line ], yPos[ 1 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 0 ][ 0 ],
                            2, Brushes.Black );
                        PutDrawAreaLine(
                            xPos[ line ], yPos[ 1 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 0 ][ 0 ],
                            2, Brushes.Black );

                    }
                    else if ( topicField[ line + 1 ] == 2 )
                    {   // lines to two knots - 4 lines
                        PutDrawAreaLine(
                            xPos[ line ], yPos[ 1 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 0 ],
                            2, Brushes.Black );
                        PutDrawAreaLine(
                            xPos[ line ], yPos[ 1 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 1 ],
                            2, Brushes.Black );
                        PutDrawAreaLine(
                            xPos[ line ], yPos[ 1 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 0 ],
                            2, Brushes.Black );
                        PutDrawAreaLine(
                            xPos[ line ], yPos[ 1 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 1 ],
                            2, Brushes.Black );

                    }
                    else
                    {   // lines to many knots - 4 stroked lines
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 1 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 0 ],
                            2, Brushes.Black );
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 1 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 1 ],
                            2, Brushes.Black );
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 1 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 0 ],
                            2, Brushes.Black );
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 1 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 1 ],
                            2, Brushes.Black );

                    }
                }
                else
                {   // lines from many knots
                    if ( topicField[ line + 1 ] == 1 )
                    {   // lines to one knot - 2 stroked lines
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 2 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 0 ][ 0 ],
                            2, Brushes.Black );
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 2 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 0 ][ 0 ],
                            2, Brushes.Black );

                    }
                    else if ( topicField[ line + 1 ] == 2 )
                    {   // lines to two knots - 4 stroked lines
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 2 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 0 ],
                            2, Brushes.Black );
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 2 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 1 ],
                            2, Brushes.Black );
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 2 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 0 ],
                            2, Brushes.Black );
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 2 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 1 ][ 1 ],
                            2, Brushes.Black );

                    }
                    else
                    {   // lines to many knots - 4 stroked lines
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 2 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 0 ],
                            2, Brushes.Black );
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 2 ][ 0 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 1 ],
                            2, Brushes.Black );
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 2 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 0 ],
                            2, Brushes.Black );
                        PutDrawAreaLineStroked(
                            xPos[ line ], yPos[ 2 ][ 1 ],
                            xPos[ line + 1 ], yPos[ 2 ][ 1 ],
                            2, Brushes.Black );

                    }
                }

            }   // end: lines in 9 possibilities

            // 3 different layers
            for ( int kreis = 0; kreis < topicField.Length; kreis++ )
            {
                if ( topicField[ kreis ] == 1 )
                {   // one knot + number
                    PutDrawAreaEllipse( xPos[ kreis ], yPos[ 0 ][ 0 ] );
                    PutDrawAreaLabel( xPos[ kreis ],
                        yPos[ 0 ][ 0 ], topicField[ kreis ].ToString( ) );

                }
                else if ( topicField[ kreis ] == 2 )
                {   // two knots + number
                    PutDrawAreaEllipse( xPos[ kreis ], yPos[ 1 ][ 0 ] );
                    PutDrawAreaEllipse( xPos[ kreis ], yPos[ 1 ][ 1 ] );
                    PutDrawAreaLabel( xPos[ kreis ],
                        yPos[ 1 ][ 0 ], topicField[ kreis ].ToString( ) );

                }
                else
                {   // many knots + number
                    // first the middle stroked line
                    PutDrawAreaLineStroked( xPos[ kreis ], yPos[ 2 ][ 0 ],
                        xPos[ kreis ], yPos[ 2 ][ 1 ],
                            2, Brushes.Black );
                    // now the knots and the number
                    PutDrawAreaEllipse( xPos[ kreis ], yPos[ 2 ][ 0 ] );
                    PutDrawAreaEllipse( xPos[ kreis ], yPos[ 2 ][ 1 ] );
                    PutDrawAreaLabel( xPos[ kreis ],
                        yPos[ 2 ][ 0 ], topicField[ kreis ].ToString( ) );

                }

            }   // end: 3 different layers

            // additionally update the zoom window
            if ( IsVisible )
                ShowWindow( );

        }   // end: ShowTopic

        /// <summary>
        /// Helper function putting a line into
        /// the 'drawArea'.
        /// </summary>
        /// <param name="x1">from x-coordinate</param>
        /// <param name="y1">from y-coordinate</param>
        /// <param name="x2">to x-coordinate</param>
        /// <param name="y2">to y-coordinate</param>
        /// <param name="thickness">standard broadness is 2</param>
        /// <param name="colBrush">standard color is Brushes.Black</param>
        public void PutDrawAreaLine( double x1, double y1, double x2, double y2,
            int thickness, Brush colBrush )
        {
            Line nr0 = new();
            nr0.X1 = x1;
            nr0.Y1 = y1;
            nr0.X2 = x2;
            nr0.Y2 = y2;
            nr0.Stroke = colBrush;
            nr0.StrokeThickness = thickness;
            drawArea.Children.Add( nr0 );

        }   // end: PutDrawAreaLine

        /// <summary>
        /// Helper function putting a stroked line into
        /// the 'drawArea'.
        /// </summary>
        /// <param name="x1">from x-coordinate</param>
        /// <param name="y1">from y-coordinate</param>
        /// <param name="x2">to x-coordinate</param>
        /// <param name="y2">to y-coordinate</param>
        /// <param name="thickness">standard broadness is 2</param>
        /// <param name="colBrush">standard color is Brushes.Black</param>
        public void PutDrawAreaLineStroked( double x1, double y1, double x2, double y2,
            int thickness, Brush colBrush )
        {
            Line nr0 = new();
            nr0.X1 = x1;
            nr0.Y1 = y1;
            nr0.X2 = x2;
            nr0.Y2 = y2;
            nr0.StrokeDashArray = new DoubleCollection( ) { 1, 1 };
            nr0.Stroke = colBrush;
            nr0.StrokeThickness = thickness;
            drawArea.Children.Add( nr0 );

        }   // end: PutDrawAreaLineStroked

        /// <summary>
        /// Helper function putting an ellipse
        /// into the 'drawArea'.
        /// </summary>
        /// <param name="xPos">distance from the left border minus the half width</param>
        /// <param name="yPos">distance from the top border minus the half height</param>
        /// <param name="height">standard if not changed</param>
        /// <param name="width">standard if not changed</param>
        public void PutDrawAreaEllipse( double xPos, double yPos,
            double height = 20, double width = 20 )
        {
            Ellipse knoten = new();
            knoten.Height = height;
            knoten.Width = width;
            Canvas.SetLeft( knoten, xPos - ( width / 2 ) );
            Canvas.SetTop( knoten, yPos - ( height / 2 ) );
            knoten.Stroke = Brushes.Gray;
            knoten.Fill = Brushes.Orange;
            drawArea.Children.Add( knoten );

        }   // end: PutDrawAreaEllipse

        /// <summary>
        /// Helper function putting a label onto the given coordinates
        /// in the 'drawArea'.
        /// </summary>
        /// <param name="xPos">distance from the left border</param>
        /// <param name="yPos">distance from the top border</param>
        /// <param name="text">message</param>
        /// <param name="height">standard if not changed</param>
        /// <param name="width">standard if not changed</param>
        public void PutDrawAreaLabel( double xPos, double yPos, string text,
            double height = 20, double width = 20 )
        {
            Label lbl1 = new();
            lbl1.Content = text;
            Canvas.SetLeft( lbl1, xPos - ( width / 2 ) );
            Canvas.SetTop( lbl1, yPos - height - 15 );
            drawArea.Children.Add( lbl1 );

        }   // end: PutDrawAreaLabel

        /// <summary>
        /// Parses the int[] of the layers from
        /// a string representation.
        /// </summary>
        /// <param name="topic">the topic string</param>
        /// <param name="layerInts">reference to a int[] for the layers</param>
        /// <returns>success of the operation</returns>
        public bool ParseTopic( string topic, ref int[] layerInts )
        {
            if ( string.IsNullOrEmpty( topic ) )
            {
                return ( false );

            }
            if ( !topic.EndsWith( "." ) )
                return ( false );

            bool ok = false;
            string temp = topic.Replace(" ", "");
            temp = temp.Replace( ".", "" );
            var feld = temp.Split(",");
            int[] tempErgebnis = new int[feld.Length];
            for ( int i = 0; i < feld.Length; i++ )
            {
                try
                {
                    tempErgebnis[ i ] = int.Parse( feld[ i ] );
                }
                catch
                {
                    return ( false );
                }

            }
            if ( feld.Length > 2 )
            {
                ok = true;
                layerInts = ( int[ ] )tempErgebnis;

            }
            ShowTopic( );
            return ( ok );

        }   // end: ParseTopic

        /// <summary>
        /// The given dataset defines the input-output layers.
        /// </summary>
        /// <param name="dataIn"></param>
        /// <param name="dataOut"></param>
        /// <returns></returns>
        public bool ParseDataIntoTopic( double[][] dataIn, double[][] dataOut )
        {
            if ( string.IsNullOrEmpty( workingTopic ) )
            {
                return ( false );

            }
            if ( !workingTopic.EndsWith( "." ) )
                return ( false );

            bool ok = false;
            string temp = workingTopic.Replace(" ", "");
            temp = temp.Replace( ".", "" );
            string[] feld = temp.Split(",");
            int[] tempErgebnis = new int[feld.Length];
            for ( int i = 0; i < feld.Length; i++ )
            {
                try
                {
                    tempErgebnis[ i ] = int.Parse( feld[ i ] );
                }
                catch
                {
                    return ( false );
                }

            }
            if ( feld.Length > 2 )
            {   // creation of the new 'workingTopic' and saving of 'topicField'
                ok = true;
                if ( tempErgebnis[ 0 ] != dataIn[ 0 ].Length )
                    tempErgebnis[ 0 ] = dataIn[ 0 ].Length;
                if ( tempErgebnis[ tempErgebnis.Length - 1 ]
                        != dataOut[ 0 ].Length )
                    tempErgebnis[ tempErgebnis.Length - 1 ]
                        = dataOut[ 0 ].Length;
                workingTopic = "";
                for ( int num = 0; num < ( tempErgebnis.Length - 1 ); num++ )
                {
                    workingTopic += tempErgebnis[ num ].ToString( ) + ", ";
                }
                workingTopic +=
                    tempErgebnis[ tempErgebnis.Length - 1 ].ToString( )
                    + ".";
                topicField = tempErgebnis;

            }
            ShowTopic( );
            return ( ok );
        }   // end: ParseDataIntoTopic

        /// <summary>
        /// The given dataset defines the input-output layers.
        /// </summary>
        /// <param name="dataIn"></param>
        /// <param name="dataOut"></param>
        /// <returns></returns>
        public bool ParseLocalDataIntoTopic( int dataIn, int dataOut )
        {
            if ( string.IsNullOrEmpty( workingTopic ) )
            {
                return ( false );

            }
            if ( !workingTopic.EndsWith( "." ) )
                return ( false );

            bool ok = false;
            string temp = workingTopic.Replace(" ", "");
            temp = temp.Replace( ".", "" );
            string[] feld = temp.Split(",");
            int[] tempErgebnis = new int[feld.Length];
            for ( int i = 0; i < feld.Length; i++ )
            {
                try
                {
                    tempErgebnis[ i ] = int.Parse( feld[ i ] );
                }
                catch
                {
                    return ( false );
                }

            }
            if ( feld.Length > 2 )
            {   // creation of the new 'workingTopic' and saving of 'topicField'
                ok = true;
                if ( tempErgebnis[ 0 ] != dataIn )
                    tempErgebnis[ 0 ] = dataIn;
                if ( tempErgebnis[ tempErgebnis.Length - 1 ]
                        != dataOut )
                    tempErgebnis[ tempErgebnis.Length - 1 ]
                        = dataOut;
                workingTopic = "";
                for ( int num = 0; num < ( tempErgebnis.Length - 1 ); num++ )
                {
                    workingTopic += tempErgebnis[ num ].ToString() + ", ";
                }
                workingTopic +=
                    tempErgebnis[ tempErgebnis.Length - 1 ].ToString()
                    + ".";
                topicField = tempErgebnis;

            }
            ShowTopic( );
            return ( ok );

        }   // end: ParseLocalDataIntoTopic

        /// <summary>
        /// Helper function putting a line into
        /// 'canvasWindow'.
        /// </summary>
        /// <param name="x1">from x-coordinate</param>
        /// <param name="y1">from y-coordinate</param>
        /// <param name="x2">to x-coordinate</param>
        /// <param name="y2">to y-coordinate</param>
        /// <param name="thickness">standard broadness is 2</param>
        /// <param name="colBrush">color of the brush</param>
        public void PutCanvasWindowLine( double x1, double y1, double x2, double y2,
            int thickness, Brush colBrush )
        {
            Line nr0 = new();
            nr0.X1 = x1;
            nr0.Y1 = y1;
            nr0.X2 = x2;
            nr0.Y2 = y2;
            nr0.Stroke = colBrush;
            nr0.StrokeThickness = thickness;
            _canvasWindowCanvas.Children.Add( nr0 );

        }   // end: PutCanvasWindowLine

        /// <summary>
        /// Helper function putting a stroked line into
        /// 'canvasWindow'.
        /// </summary>
        /// <param name="x1">from x-coordinate</param>
        /// <param name="y1">from y-coordinate</param>
        /// <param name="x2">to x-coordinate</param>
        /// <param name="y2">to y-coordinate</param>
        /// <param name="thickness">standard broadness is 2</param>
        /// <param name="colBrush">color of the brush</param>
        public void PutCanvasWindowLineStroked( double x1, double y1, double x2, double y2,
            int thickness, Brush colBrush )
        {
            Line nr0 = new();
            nr0.X1 = x1;
            nr0.Y1 = y1;
            nr0.X2 = x2;
            nr0.Y2 = y2;
            nr0.StrokeDashArray = new DoubleCollection( ) { 1, 1 };
            nr0.Stroke = colBrush;
            nr0.StrokeThickness = thickness;
            _canvasWindowCanvas.Children.Add( nr0 );

        }   // end: PutCanvasWindowLineStroked

        /// <summary>
        /// Helper function putting an ellipse into
        /// 'canvasWindow' at the given coordinates.
        /// </summary>
        /// <param name="xPos">distance from the left border minus half of the width</param>
        /// <param name="yPos">distance from th top border minus half the height</param>
        /// <param name="height">standard if not changed</param>
        /// <param name="width">standard if not changed</param>
        public void PutCanvasWindowEllipse( double xPos, double yPos,
            double height = 20, double width = 20 )
        {
            Ellipse knoten = new();
            knoten.Height = height;
            knoten.Width = width;
            Canvas.SetLeft( knoten, xPos - ( width / 2 ) );
            Canvas.SetTop( knoten, yPos - ( height / 2 ) );
            knoten.Stroke = Brushes.Gray;
            knoten.Fill = Brushes.Orange;
            _canvasWindowCanvas.Children.Add( knoten );

        }   // end: PutCanvasWindowEllipse

        /// <summary>
        /// Helper function for 'canvasWindow'putting a label
        /// onto the given coordinates.
        /// </summary>
        /// <param name="xPos">distance from the left border</param>
        /// <param name="yPos">distance from the top border</param>
        /// <param name="text">the message</param>
        /// <param name="height">standard if not changed</param>
        /// <param name="width">standard if not changed</param>
        public void PutCanvasWindowLabel( double xPos, double yPos, string text,
            double height = 20, double width = 20 )
        {
            Label lbl1 = new();
            lbl1.Content = text;
            Canvas.SetLeft( lbl1, xPos - ( width / 2 ) );
            Canvas.SetTop( lbl1, yPos - height - 15 );
            _canvasWindowCanvas.Children.Add( lbl1 );

        }   // end: PutDrawAreaLabel

        // ------------------------------------     Event handlers

        /// <summary>
        /// Event handler for the 'Canvas' in 'CanvasWindow'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CanvasWindowCanvas_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            _canvasWindowCanvas.Width = _canvasWindow.Width;
            _canvasWindowCanvas.Height = _canvasWindow.Height;
            ShowWindow();

        }   // end: _CanvasWindowCanvas_SizeChanged

    }   // end: public class CanvasTopic

}   // end: namespace MatrixFFN.Tools
