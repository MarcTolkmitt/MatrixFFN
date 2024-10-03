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
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace MatrixFFN.Tools
{
    /// <summary>
    /// helper for the colornumbers used
    /// </summary>
    public struct ColorNumber
    {
        /// <summary>
        /// black color brush
        /// </summary>
        static public int Black = 3;
        /// <summary>
        /// gray color brush
        /// </summary>
        static public int Gray = 4;

    }   // end: ColorNumber

    /// <summary>
    /// Using LINQ to show up to two lines in the chart in
    /// properly scaled way.
    /// <para>Allows to choose not to connect the datapoints
    /// with lines for unordered datasets and you can reduce the
    /// shown dataset to the last elements.</para>
    /// <para>You can open a bigger window with the charts data. ( 'CanvasWindow' )</para>
    /// <para>Chart is painted onto a given 'Canvas'.</para>
    /// </summary>
    public class CanvasChart : CanvasWindow
    {
        /// <summary>
        /// <para>created on: 08.07.2023</para>
        /// <para>last edit: 02.10.24</para>
        /// </summary>
        public Version version = new Version("1.0.16");
        Canvas drawArea;    
        double xSize;
        double ySize;
        Label titel = new Label();
        Label xAxis = new Label();
        Label yAxis = new Label();
        /// <summary>
        /// titel of the coordinate system
        /// </summary>
        public string titelText = "empty dataset";
        List<double[ ]> dataX = new List<double[ ]>();
        List<double[ ]> dataY = new List<double[ ]>();
        // the relative distances
        double xStart;
        double yStart;
        double xEnd;
        double yEnd;
        double xDist;
        double yDist;
        int numDAdata = 0;
        double minDAx;
        double minDAy;
        double maxDAx;
        double maxDAy;
        double spanDAx;
        double spanDAy;
        double xDAstart;
        double yDAstart;
        double xDAend;
        double yDAend;
        double xDAdist;
        double yDAdist;

        // the field
        double fieldXstart;
        double fieldXend;
        double fieldYstart;
        double fieldYend;
        double fieldX_DAstart;
        double fieldX_DAend;
        double fieldY_DAstart;
        double fieldY_DAend;
        // vars for the display
        int numData = 0;
        double minX;
        double minY;
        double maxX;
        double maxY;
        double spanX;
        double spanY;
        Brush[ ] colorsLines;
        Brush[ ] colorsCircles;   
        int showNoOfData = 0;
        /// <summary>
        /// draw lines between data points - not recommended
        /// for unsorted data
        /// </summary>
        public bool useLines = true;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="windowTitle">title for the window</param>
        /// <param name="inCanvas">to use 'Canvas'</param>
        /// <param name="useLinesBool">show the dots connected with lines</param>
        public CanvasChart( string windowTitle, ref Canvas inCanvas, 
            bool useLinesBool = true )
        {
            this.drawArea = inCanvas;
            if ( !useLinesBool )
                useLines = false;
            xSize = Width;
            ySize = Height;
            if ( windowTitle != "" )
                titelText = windowTitle;
            Title = titelText;
            colorsLines = new Brush[ 4 ] { Brushes.Magenta, Brushes.DarkGreen, 
                    Brushes.Black, Brushes.Gray };
            colorsCircles = new Brush[ 2 ] { Brushes.Blue, Brushes.Orange };
            ShowChart();

        }   // end: CanvasChart ( constructor )

        /// <summary>
        /// Set this value to only show the last elemnts of the
        /// dataset in the chart.
        /// <para>0 show all the data.</para>
        /// </summary>
        /// <param name="inNo">the new value 'showNoOfData'</param>
        public void SetShowNoOfData( int inNo = 0 )
        {
            showNoOfData = inNo;

        }   // end: SetShowNoOfData

        /// <summary>
        /// Clears the datalists.
        /// </summary>
        public void DataClear()
        {
            dataX.Clear();
            dataY.Clear();
            titelText = "empty dataset";
            Title = titelText;

        }   // end: DataClear

        /// <summary>
        /// Saves the dataset in two lists ( X, Y ).
        /// <para>Used with normal data.</para>
        /// </summary>
        /// <param name="inX">field with the coordinates for X</param>
        /// <param name="inY">field with the coordinates for Y</param>
        public void DataAdd( double[ ] inX, double[ ] inY )
        {
            dataX.Add( inX );
            dataY.Add( inY );

        }   // end: DataAdd

        /// <summary>
        /// Saves the dataset in two lists ( X, Y ).
        /// <para>Used with the error data.</para>
        /// </summary>
        /// <param name="inX">field with the coordinates for X</param>
        /// <param name="inY">field with the coordinates for Y</param>
        public void DataAdd( List<long> inX, List<double> inY )
        {
            double[] tempX = 
                ( from long num in  inX 
                  select Convert.ToDouble( num ) ).ToArray<double>();
            double[] tempY = ( from num in inY select num ).ToArray<double>();
            dataX.Add( tempX );
            dataY.Add( tempY );

        }   // end: DataAdd

        /// <summary>
        /// Shows the coordinate system scaled to the dataset  
        /// including the 'showNoOfData' last elements. You need more
        /// than one datapoint or nothing is shown!
        /// <para>The next first use of LINQ - an achievement for me.</para>
        /// </summary>
        public void ShowChart()
        {
            // the relative distances
            xStart = xSize / 20;
            yStart = ySize / 10;
            xEnd = xSize - xStart;
            yEnd = ySize - yStart;
            xDist = xStart / 4;
            yDist = yStart / 4;
            
            drawArea.Children.Clear();
            // the titel
            SetDrawAreaLabel( 150, 0, titelText, 14, true );
            // the frame
            SetDrawAreaLine( 0, 0, xSize, 0, 1, ColorNumber.Gray );
            SetDrawAreaLine( 0, 0, 0, ySize, 1, ColorNumber.Gray );
            SetDrawAreaLine( xSize, 0, xSize, ySize, 1, ColorNumber.Gray );
            SetDrawAreaLine( 0, ySize, xSize, ySize, 1, ColorNumber.Gray );

            // the coordinate system
            SetDrawAreaLine( xStart, yStart, xStart, yEnd, 2, ColorNumber.Black );
            SetDrawAreaLine( xStart, yEnd, xEnd, yEnd, 2, ColorNumber.Black );
            SetDrawAreaLine( xStart + xDist, yStart + yDist, xStart, yStart, 1, ColorNumber.Black );
            SetDrawAreaLine( xStart - xDist, yStart + yDist, xStart, yStart, 1, ColorNumber.Black );
            SetDrawAreaLine( xEnd - xDist, yEnd - yDist, xEnd, yEnd, 1, ColorNumber.Black );
            SetDrawAreaLine( xEnd - xDist, yEnd + yDist, xEnd, yEnd, 1, ColorNumber.Black );
            SetDrawAreaLabel( xStart / 2, 0, "y", 10 );
            SetDrawAreaLabel( xEnd, yEnd - ( 2 * yDist ), "x", 10 );
            // Feldlimits
            SetDrawAreaLine( xStart - xDist, yStart + ( 2 * yDist ), 
                xStart + xDist, yStart + ( 2 * yDist ), 1, ColorNumber.Black );
            SetDrawAreaLine( xEnd - ( 2 * xDist ), yEnd - yDist,
                xEnd - ( 2 * xDist ), yEnd + yDist, 1, ColorNumber.Black );
            // the fields boundary
            fieldXstart = xStart + ( 2 * xDist );
            fieldXend = xEnd - ( 2 * xDist );
            fieldYstart = yStart + ( 2 * yDist );
            fieldYend = yEnd - ( 2 * yDist );
            // the fields lines
            SetDrawAreaLineStroked( fieldXstart, fieldYstart, 
                fieldXend, fieldYstart, 1, ColorNumber.Black );
            SetDrawAreaLineStroked( fieldXend, fieldYstart, 
                fieldXend, fieldYend, 1, ColorNumber.Black );
            SetDrawAreaLineStroked( fieldXstart, fieldYstart, 
                fieldXstart, fieldYend, 1, ColorNumber.Black );
            SetDrawAreaLineStroked( fieldXstart, fieldYend, 
                fieldXend, fieldYend, 1, ColorNumber.Black );
            var tempDataX = GetFieldsLastElements( dataX );
            var tempDataY = GetFieldsLastElements( dataY );

            if ( tempDataX.Count > 0 )
                if ( tempDataX.Count == 1 )
                {
                    numData = tempDataX.First( ).Length;
                    minX = ( from num in tempDataX.First() select num ).Min( );
                    maxX = ( from num in tempDataX.First( ) select num ).Max( );
                    minY = ( from num in tempDataY.First( ) select num ).Min( );
                    maxY = ( from num in tempDataY.First( ) select num ).Max( );
                    spanX = maxX - minX;
                    spanY = maxY - minY;

                }
                else
                {
                    numData = ( from feld in tempDataX select feld.Length ).Max( );
                    minX = ( from feld in tempDataX
                             from num in feld
                             select num ).Min( );
                    maxX= ( from feld in tempDataX
                            from num in feld
                            select num ).Max( );
                    minY = ( from feld in tempDataY
                             from num in feld
                             select num ).Min( );
                    maxY = ( from feld in tempDataY
                             from num in feld
                             select num ).Max( );
                    spanX = maxX - minX;
                    spanY = maxY - minY;

                }
            // loop to show now the data
            if ( tempDataX.Count > 0 )
                if ( tempDataX[ 0 ].Length > 1 )
                    for ( int feld = 0; feld < tempDataX.Count; feld++ )
            {   // every single field was given for X and Y
                double[] xRealPos = new double[ tempDataX[ feld ].Length ];
                double[] yRealPos = new double[ tempDataY[ feld ].Length ];
                for ( int num = 0; num < tempDataX[ feld ].Length; num++ )
                {
                    xRealPos[ num ] = GetXpos( tempDataX[ feld ][ num ] );
                    yRealPos[ num ] = GetYpos( tempDataY[ feld ][ num ] );

                }
                // from here the real data will be shown
                if ( useLines )
                    for ( int num = 0; num < ( xRealPos.Length - 1 ); num++ )
                    {
                        SetDrawAreaLine( xRealPos[ num ], yRealPos[ num ],
                            xRealPos[ num + 1 ], yRealPos[ num + 1 ],
                            2, feld );

                    }
                for ( int num = 0; num < xRealPos.Length; num++ )
                    PutDrawAreaEllipse( xRealPos[ num ], yRealPos[ num ], 5, 5, 
                        feld,
                        tempDataX[ feld ][ num ], tempDataY[ feld ][ num ],
                        useLines );

                // ornaments for the coordinate achses if data can be shown
                double stepX = spanX / 4;
                double stepY = spanY / 4;

                for ( int num = 0; num < 5; num++ )
                {
                    SetXaxisLabel( minX + ( num * stepX ) );
                    SetYaxisLabel( minY + ( num * stepY ) );

                }

            }   // end: loop to show now the data
            if ( IsVisible )
                ShowWindow( );

        }   // end: ShowChart

        /// <summary>
        /// Delivers the wanted last elements of a list 
        /// containing double-fields.
        /// </summary>
        /// <param name="inList">input list</param>
        /// <returns>new list with the number of elements</returns>
        public List<double[ ]> GetFieldsLastElements( List<double[ ]> inList )
        {
            List<double[]> result = new List<double[ ]>();
            for ( int feldNum = 0; feldNum < inList.Count; feldNum++ )
            {
                result.Add( GetLastElements( inList[ feldNum ] ) );

            }
            return( result );

        }   // end: GetFieldsLastElements

        /// <summary>
        /// Dependent on 'showNoOfData' the lessered length of the inputfield
        /// will be given back.
        /// </summary>
        /// <param name="inArray">the inputarray</param>
        /// <returns>the shortened field</returns>
        public double[ ] GetLastElements( double[ ] inArray )
        {
            if ( ( inArray.Length <= showNoOfData ) || ( showNoOfData == 0 ) )
                return ( inArray );
            else
            {
                double[] result = new double[ showNoOfData ];
                for ( int pos = 0; pos < showNoOfData; pos++ )
                    result[ pos ] = 
                        inArray[ ( inArray.Length - showNoOfData - 1) + pos ];
                return( result );

            }

        }   // end: GetLastElements

        /// <summary>
        /// Sets the label of the x-axle.
        /// </summary>
        /// <param name="xPosition">position on the 'Canvas'</param>
        public void SetXaxisLabel( double xPosition )
        {
            double tNum = GetXpos( xPosition );
            SetDrawAreaLine( tNum, yEnd - yDist, tNum, yEnd + yDist, 1, ColorNumber.Black );
            SetDrawAreaLabel( tNum - xDist, yEnd, xPosition.ToString() );

        }   // end: SetXaxisLabel

        /// <summary>
        /// Sets the label of the y-axle.
        /// </summary>
        /// <param name="yPosition">position on the 'Canvas'</param>
        public void SetYaxisLabel( double yPosition )
        {
            double tNum = GetYpos( yPosition );
            SetDrawAreaLine( xStart - xDist, tNum, xStart + xDist, tNum, 1, ColorNumber.Black );
            SetDrawAreaLabel( 0, tNum - yDist, yPosition.ToString() );

        }   // end: SetYaxisLabel

        /// <summary>
        /// Calculates from the logical datavalue the position
        /// in the diagramm for the x-position.
        /// </summary>
        /// <param name="xValue">a value of the dataset</param>
        /// <returns>the real x-position on the 'Canvas'</returns>
        public double GetXpos( double xValue )
        {
            double xPosWert = ( xValue - minX ) / spanX;
            double xRealPos = fieldXstart + ( ( fieldXend - fieldXstart ) * xPosWert );
            if ( xRealPos == double.NaN )
                throw new ArgumentException(
                    $"CanvasChart.GetXpos -> NaN-Fehler: Eingabewert = {xValue}",
                    "( xRealPos == double.NaN )" );
            return ( xRealPos );

        }   // end: GetXpos

        /// <summary>
        /// Calculates from the logical datavalue the position
        /// in the diagramm for the y-position.
        /// </summary>
        /// <param name="yValue">a value of the dataset</param>
        /// <returns>the real y-position on the 'Canvas'</returns>
        public double GetYpos( double yValue )
        {
            double yPosWert = ( yValue - minY ) / spanY;
            double yRealPos = fieldYend - ( ( fieldYend - fieldYstart ) * yPosWert );
            if ( yRealPos == double.NaN )
                throw new ArgumentException(
                    $"CanvasChart.GetYpos -> NaN-Fehler: Eingabewert = {yValue}", 
                    "( yRealPos == double.NaN )" );
            return ( yRealPos );

        }   // end: GetYpos

        /// <summary>
        /// Helpfunction for the 'drawArea' setting a label
        /// onto the given position. Can inscribe the chart.
        /// </summary>
        /// <param name="xPos">distance from the left border</param>
        /// <param name="yPos">distance from the upper border</param>
        /// <param name="text">the string</param>
        /// <param name="fontSize">the fonts size</param>
        /// <param name="bold">bold?</param>
        public void SetDrawAreaLabel( double xPos, double yPos, string text,
            int fontSize = 10, bool bold = false )
        {
            Label lbl1 = new Label();
            lbl1.Content = text;
            if ( bold ) 
                lbl1.FontWeight = FontWeights.Bold;
            else 
                lbl1.FontWeight = FontWeights.Regular;
            lbl1.FontSize = fontSize;
            Canvas.SetLeft(lbl1, xPos );
            Canvas.SetTop(lbl1, yPos );
            drawArea.Children.Add(lbl1);

        }   // end: SetDrawAreaLabel

        /// <summary>
        /// Helpfunction setting a line into
        /// the 'drawArea'.
        /// </summary>
        /// <param name="x1">from the x-coordinate</param>
        /// <param name="y1">from the y-coordinate</param>
        /// <param name="x2">to the x-coordinate</param>
        /// <param name="y2">to the y-coordinate</param>
        /// <param name="thickness">linebroadness</param>
        /// <param name="brushNumber">number of the brush</param>
        public void SetDrawAreaLine( double x1, double y1, double x2, double y2,
            int thickness, int brushNumber )
        {
            Line nr0 = new Line();
            nr0.X1 = x1;
            nr0.Y1 = y1;
            nr0.X2 = x2;
            nr0.Y2 = y2;
            nr0.Stroke = colorsLines[ brushNumber ];
            nr0.StrokeThickness = thickness;
            drawArea.Children.Add( nr0 );

        }   // end: SetDrawAreaLine

        /// <summary>
        /// Helpfunction setting a stroked line into
        /// the 'drawArea'.
        /// </summary>
        /// <param name="x1">from the x-coordinate</param>
        /// <param name="y1">from the y-coordinate</param>
        /// <param name="x2">to the x-coordinate</param>
        /// <param name="y2">to the y-coordinate</param>
        /// <param name="thickness">linebroadness</param>
        /// <param name="brushNumber">number of the brush</param>
        public void SetDrawAreaLineStroked( double x1, double y1, double x2, double y2,
            int thickness, int brushNumber )
        {
            Line nr0 = new Line();
            nr0.X1 = x1;
            nr0.Y1 = y1;
            nr0.X2 = x2;
            nr0.Y2 = y2;
            nr0.Stroke = colorsLines[ brushNumber ];
            nr0.StrokeDashArray = new DoubleCollection( ) { 1, 1 };
            nr0.StrokeThickness = thickness;
            drawArea.Children.Add( nr0 );

        }   // end: SetDrawAreaLineStroked

        /// <summary>
        /// Helpfunction setting an ellipse
        /// into the 'drawArea'.
        /// </summary>
        /// <param name="xPos">distance from the left border minus the half broadness</param>
        /// <param name="yPos">distance from the top minus half the height</param>
        /// <param name="height">normal height is 5</param>
        /// <param name="width">normal width is 5</param>
        /// <param name="brushNumber">number of the brushColor</param>
        /// <param name="xReal">tooltip x-position ( = 0 )</param>
        /// <param name="yReal">tooltip y-position ( = 0 )</param>
        /// <param name="useTheLines">charts with lines?</param>
        void PutDrawAreaEllipse( double xPos, double yPos,
            double height, double width, int brushNumber, 
            double xReal = 0, double yReal = 0, bool useTheLines = true )
        {
            Ellipse knot = new Ellipse();
            knot.Height = height;
            knot.Width = width;
            Canvas.SetLeft( knot, xPos - ( width / 2 ) );
            Canvas.SetTop( knot, yPos - ( height / 2 ) );
            if ( brushNumber == 1 )
                knot.Opacity = 0.5;
            knot.Stroke = Brushes.Gray;
            knot.Fill = colorsCircles[ brushNumber ];
            ToolTip tip = new ToolTip();
            tip.Content = $"( {xReal}, {yReal} )";
            knot.ToolTip = tip;
            drawArea.Children.Add( knot );

        }   // end: PutDrawAreaEllipse

        /// <summary>
        /// Shows the data in the window ( 'CanvasWindow' )
        /// </summary>
        public void ShowWindow( )
        {
            double xFsize = _canvasWindowCanvas.Width;
            double yFsize = _canvasWindowCanvas.Height;
            
            // the relative distances
            xDAstart = xFsize / 20;
            yDAstart = yFsize / 10;
            xDAend = xFsize - xDAstart;
            yDAend = yFsize - yDAstart;
            xDAdist = xDAstart / 4;
            yDAdist = yDAstart / 4;

            Title = titelText;
            Show( );
            _canvasWindowCanvas.Children.Clear( );
            // the titel
            SetCanvasWindowLabel( 150, 0, titelText, 14, true );
            // the frame
            SetCanvasWindowLine( 0, 0, xFsize, 0, 1, ColorNumber.Gray );
            SetCanvasWindowLine( 0, 0, 0, yFsize, 1, ColorNumber.Gray );
            SetCanvasWindowLine( xFsize, 0, xFsize, yFsize, 1, ColorNumber.Gray );
            SetCanvasWindowLine( 0, yFsize, xFsize, yFsize, 1, ColorNumber.Gray );

            // the coordinate system
            SetCanvasWindowLine( xDAstart, yDAstart, xDAstart, yDAend,2 , ColorNumber.Black );
            SetCanvasWindowLine( xDAstart, yDAend, xDAend, yDAend, 2, ColorNumber.Black );
            SetCanvasWindowLine( xDAstart + xDAdist, yDAstart + yDAdist, xDAstart, yDAstart, 1, ColorNumber.Black );
            SetCanvasWindowLine( xDAstart - xDAdist, yDAstart + yDAdist, xDAstart, yDAstart, 1, ColorNumber.Black );
            SetCanvasWindowLine( xDAend - xDAdist, yDAend - yDAdist, xDAend, yDAend, 1, ColorNumber.Black );
            SetCanvasWindowLine( xDAend - xDAdist, yDAend + yDAdist, xDAend, yDAend, 1, ColorNumber.Black );
            SetCanvasWindowLabel( xDAstart / 2, 0, "y", 20 );
            SetCanvasWindowLabel( xDAend, yDAend - ( 2 * yDAdist ), "x", 20 );
            // fieldlimits
            SetCanvasWindowLine( xDAstart - xDAdist, yDAstart + ( 2 * yDAdist ),
                xDAstart + xDAdist, yDAstart + ( 2 * yDAdist ), 1, ColorNumber.Black );
            SetCanvasWindowLine( xDAend - ( 2 * xDAdist ), yDAend - yDAdist,
                xDAend - ( 2 * xDAdist ), yDAend + yDAdist, 1, ColorNumber.Black );
            // the field
            fieldX_DAstart = xDAstart + ( 2 * xDAdist );
            fieldX_DAend = xDAend - ( 2 * xDAdist );
            fieldY_DAstart = yDAstart + ( 2 * yDAdist );
            fieldY_DAend = yDAend - ( 2 * yDAdist );
            // the field frame
            SetCanvasWindowLineStroked( fieldX_DAstart, fieldY_DAstart, 
                fieldX_DAend, fieldY_DAstart, 1, ColorNumber.Black );
            SetCanvasWindowLineStroked( fieldX_DAend, fieldY_DAstart, 
                fieldX_DAend, fieldY_DAend, 1, ColorNumber.Black );
            SetCanvasWindowLineStroked( fieldX_DAstart, fieldY_DAstart, 
                fieldX_DAstart, fieldY_DAend, 1, ColorNumber.Black );
            SetCanvasWindowLineStroked( fieldX_DAstart, fieldY_DAend, 
                fieldX_DAend, fieldY_DAend, 1, ColorNumber.Black );
            // distances are got with LINQ from the data
            var tempDataX = GetFieldsLastElements( dataX );
            var tempDataY = GetFieldsLastElements( dataY );

            if ( tempDataX.Count > 0 )
                if ( tempDataX.Count == 1 )
                {
                    numDAdata = dataX.First( ).Length;
                    minDAx = ( from num in tempDataX.First( ) select num ).Min( );
                    maxDAx = ( from num in tempDataX.First( ) select num ).Max( );
                    minDAy = ( from num in tempDataY.First( ) select num ).Min( );
                    maxDAy = ( from num in tempDataY.First( ) select num ).Max( );
                    spanDAx = maxDAx - minDAx;
                    spanDAy = maxDAy - minDAy;

                }
                else
                {
                    numDAdata = ( from feld in tempDataX select feld.Length ).Max( );
                    minDAx = ( from feld in tempDataX
                             from num in feld
                             select num ).Min( );
                    maxDAx = ( from feld in tempDataX
                             from num in feld
                             select num ).Max( );
                    minDAy = ( from feld in tempDataY
                             from num in feld
                             select num ).Min( );
                    maxDAy = ( from feld in tempDataY
                             from num in feld
                             select num ).Max( );
                    spanDAx = maxDAx - minDAx;
                    spanDAy = maxDAy - minDAy;

                }
            // starting to show the data
            if ( tempDataX.Count > 0 )
                if ( tempDataX[ 0 ].Length > 1 )
                    for ( int feld = 0; feld < tempDataX.Count; feld++ )
                    {   // every single field was given for X and Y
                        double[] xRealPos = new double[ tempDataX[ feld ].Length ];
                        double[] yRealPos = new double[ tempDataY[ feld ].Length ];
                        for ( int num = 0; num < tempDataX[ feld ].Length; num++ )
                        {
                            xRealPos[ num ] = GetCanvasWindowXpos( tempDataX[ feld ][ num ] );
                            yRealPos[ num ] = GetCanvasWindowYpos( tempDataY[ feld ][ num ] );

                        }
                        // now the data comes
                        if ( useLines )
                            for ( int num = 0; num < ( xRealPos.Length - 1 ); num++ )
                            {
                                SetCanvasWindowLine( xRealPos[ num ], yRealPos[ num ],
                                    xRealPos[ num + 1 ], yRealPos[ num + 1 ],
                                    2, feld );

                            }
                        for ( int num = 0; num < xRealPos.Length; num++ )
                            PutCanvasWindowEllipse( xRealPos[ num ], yRealPos[ num ], 
                                5, 5, feld,
                                tempDataX[ feld ][ num ], tempDataY[ feld ][ num ] );

                        // ornaments for the coordinate axles if data is there
                        double stepFx = spanDAx / 4;
                        double stepFy = spanDAy / 4;

                        for ( int num = 0; num < 5; num++ )
                        {
                            SetCanvasWindowXaxisLabel( minDAx + ( num * stepFx ) );
                            SetCanvasWindowYaxisLabel( minDAy + ( num * stepFy ) );

                        }

                    }   // end: for ( int feld

        }   // end: ShowWindow

        /// <summary>
        /// Helperfunction for 'canvasWindowCanvas'setting a label on the
        /// given coordinates. Can inscribe chart objects.
        /// </summary>
        /// <param name="xPos">distance from the left border</param>
        /// <param name="yPos">distance from the top</param>
        /// <param name="text">the message</param>
        /// <param name="fontSize">standard size is 10</param>
        /// <param name="bold = false">bold letters ?</param>
        public void SetCanvasWindowLabel( double xPos, double yPos, string text,
            int fontSize = 10, bool bold = false )
        {
            Label lbl1 = new Label();
            lbl1.Content = text;
            if ( bold )
                lbl1.FontWeight = FontWeights.Bold;
            else
                lbl1.FontWeight = FontWeights.Regular;
            lbl1.FontSize = fontSize;
            Canvas.SetLeft( lbl1, xPos );
            Canvas.SetTop( lbl1, yPos );
            _canvasWindowCanvas.Children.Add( lbl1 );

        }   // end: SetCanvasWindowLabel

        /// <summary>
        /// Helperfunction putting a line into
        /// 'canvasWindowCanvas'.
        /// </summary>
        /// <param name="x1">from x-coordinte</param>
        /// <param name="y1">from y-coordinate</param>
        /// <param name="x2">to x-coordinate</param>
        /// <param name="y2">to y-coordinate</param>
        /// <param name="thickness">standard broadness is 2</param>
        /// <param name="brushColor">color of the brush</param>
        public void SetCanvasWindowLine( double x1, double y1, double x2, double y2,
            int thickness, int brushColor )
        {
            Line nr0 = new Line();
            nr0.X1 = x1;
            nr0.Y1 = y1;
            nr0.X2 = x2;
            nr0.Y2 = y2;
            nr0.Stroke = colorsLines[ brushColor ];
            nr0.StrokeThickness = thickness;
            _canvasWindowCanvas.Children.Add( nr0 );

        }   // end: SetCanvasWindowLine

        /// <summary>
        /// Helperfunction putting a stroked line into
        /// 'canvasWindowCanvas'.
        /// </summary>
        /// <param name="x1">from x-coordinte</param>
        /// <param name="y1">from y-coordinate</param>
        /// <param name="x2">to x-coordinate</param>
        /// <param name="y2">to y-coordinate</param>
        /// <param name="thickness">standard broadness is 2</param>
        /// <param name="brushColor">color of the brush</param>
        public void SetCanvasWindowLineStroked( double x1, double y1, double x2, double y2,
            int thickness, int brushColor )
        {
            Line nr0 = new Line();
            nr0.X1 = x1;
            nr0.Y1 = y1;
            nr0.X2 = x2;
            nr0.Y2 = y2;
            nr0.Stroke = colorsLines[ brushColor ];
            nr0.StrokeDashArray = new DoubleCollection( ) { 1, 1 };
            nr0.StrokeThickness = thickness;
            _canvasWindowCanvas.Children.Add( nr0 );

        }   // end: SetCanvasWindowLineStroked

        /// <summary>
        /// Helperfunction to put an ellipse into
        /// 'canvasWindowCanvas.
        /// </summary>
        /// <param name="xPos">distance from left border minus half the width</param>
        /// <param name="yPos">distance from top border minus hals height</param>
        /// <param name="height">standard height is 5</param>
        /// <param name="width">standard width is 5</param>
        /// <param name="brushColor">color of the brush</param>
        /// <param name="xReal">tooltip X</param>
        /// <param name="yReal">tooltip Y</param>
        /// <param name="useTheLines">chart with lines?</param>
        void PutCanvasWindowEllipse( double xPos, double yPos,
            double height, double width, int brushColor, 
            double xReal = 0, double yReal = 0, bool useTheLines = true )
        {
            Ellipse knoten = new Ellipse();
            knoten.Height = height;
            knoten.Width = width;
            Canvas.SetLeft( knoten, xPos - ( width / 2 ) );
            Canvas.SetTop( knoten, yPos - ( height / 2 ) );
            if ( useTheLines )
                knoten.Opacity = 0.5;
            knoten.Stroke = Brushes.Gray;
            knoten.Fill = colorsCircles[ brushColor ];
            ToolTip tip = new ToolTip();
            tip.Content = $"( {xReal}, {yReal} )";
            knoten.ToolTip = tip;
            _canvasWindowCanvas.Children.Add( knoten );

        }   // end: PutCanvasWindowEllipse

        /// <summary>
        /// Sets the x-axle label.
        /// </summary>
        /// <param name="xPosition">x-position of the text</param>
        public void SetCanvasWindowXaxisLabel( double xPosition )
        {
            double tNum = GetCanvasWindowXpos( xPosition );
            SetCanvasWindowLine( tNum, yDAend - yDAdist, tNum, yDAend + yDAdist, 1, ColorNumber.Black );
            SetCanvasWindowLabel( tNum - xDAdist, yDAend + yDAdist, xPosition.ToString( ) );

        }   // end: SetCanvasWindowXaxisLabel

        /// <summary>
        /// Sets the y-axle label.
        /// </summary>
        /// <param name="yPosition">y-position of the text</param>
        public void SetCanvasWindowYaxisLabel( double yPosition )
        {
            double tNum = GetCanvasWindowYpos( yPosition );
            SetCanvasWindowLine( xDAstart - xDAdist, tNum, 
                xDAstart + xDAdist, tNum, 1, ColorNumber.Black );
            SetCanvasWindowLabel( 0, tNum - yDAdist, yPosition.ToString( ) );

        }   // end: SetCanvasWindowYaxisLabel

        /// <summary>
        /// Calculates from the logical datavalue the real x-position
        /// in the chart.
        /// </summary>
        /// <param name="xValue">one of the datasets value</param>
        /// <returns>the real x-position in the 'Canvas'</returns>
        public double GetCanvasWindowXpos( double xValue )
        {
            double xPosWert = ( xValue - minDAx ) / spanDAx;
            double xRealPos = fieldX_DAstart + 
                ( ( fieldX_DAend - fieldX_DAstart ) * xPosWert );
            if ( xRealPos == double.NaN )
                throw new ArgumentException(
                    $"GetCanvasWindowXpos -> NaN-Fehler: Eingabewert = {xValue}",
                    "( xRealPos == double.NaN )" );
            return ( xRealPos );

        }   // end: GetCanvasWindowXpos

        /// <summary>
        /// Calculates from the logical datavalue the real x-position
        /// in the chart.
        /// </summary>
        /// <param name="yValue">one of the datasets value</param>
        /// <returns>the real y-position in the 'Canvas'</returns>
        public double GetCanvasWindowYpos( double yValue )
        {
            double yPosWert = ( yValue - minDAy ) / spanDAy;
            double yRealPos = fieldY_DAend - 
                ( ( fieldY_DAend - fieldY_DAstart ) * yPosWert );
            if ( yRealPos == double.NaN )
                throw new ArgumentException(
                    $"GetCanvasWindowYpos -> NaN-Fehler: Eingabewert = {yValue}",
                    "( yRealPos == double.NaN )" );
            return ( yRealPos );

        }   // end: GetCanvasWindowYpos

        // ------------------------------------     Eventhandler

        /// <summary>
        /// Eventhandler for the 'Canvas' in the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CanvasWindowCanvas_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            _canvasWindowCanvas.Width = _canvasWindow.Width;
            _canvasWindowCanvas.Height = _canvasWindow.Height;
            ShowWindow();

        }   // end: _CanvasWindowCanvas_SizeChanged

    }   // end: public class CanvasChart : CanvasWindow

}   // end: namespace MatrixFFN.Tools
