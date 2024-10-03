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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MatrixFFN.Tools
{
    /// <summary>
    /// Interaktionslogik für CanvasFenster.xaml
    /// </summary>
    public partial class CanvasWindow : Window
    {
        // Erstellt ab: 08.07.2023
        // letzte Änderung: 02.10.24
        Version version = new Version("1.0.5");
        /// <summary>
        /// Flag, damit am echten Programmende auch wirklich das Fenster
        /// geschlossen wird.
        /// </summary>
        public bool isNowToEnd = false;

        // --------------------------------     Konstruktoren

        /// <summary>
        /// Konstruktor. Beachte '_canvasWindow' wurde im XAML-Text erklärt und 
        /// definiert ! ( x:Name="_canvasWindow" ). Wird als
        /// Vergrößerungsfenster benutzt.
        /// </summary>
        public CanvasWindow( )
        {
            InitializeComponent( );
            _canvasWindow.Show( );
            _canvasWindow.Close();

        }   // Ende: CanvasWindow ( Konstruktor )

        /// <summary>
        /// Konstruktor. Beachte '_canvasWindow' wurde im XAML-Text erklärt und 
        /// definiert ! ( x:Name="_canvasWindow" ). Wird als
        /// Vergrößerungsfenster benutzt.
        /// </summary>
        /// <param name="titelText">windows title</param>
        public CanvasWindow( string titelText )
        {
            InitializeComponent();
            _canvasWindow.Title = titelText;
            _canvasWindow.Show();
            _canvasWindow.Close();

        }   // Ende: CanvasWindow ( Konstruktor )

        // ------------------------------------------------     Eventhandler

        /// <summary>
        /// Eventhandler für den 'Canvas' im Fenster
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CanvasWindowCanvas_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            
        }   // Ende: _CanvasWindowCanvas_SizeChanged

        /// <summary>
        /// Eventhandler für das Fenster
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Window_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            if ( e.WidthChanged )
                _canvasWindowCanvas.Width =  _canvasWindow.Width;
            if ( e.HeightChanged )
                _canvasWindowCanvas.Height = _canvasWindow.Height;
            _CanvasWindowCanvas_SizeChanged( sender, e );

        }   // Ende: _Window_SizeChanged

        /// <summary>
        /// Eventhandler für das Schließen des Fensters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CanvasWindow_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            if ( !isNowToEnd )
            {
                // das Fenster nicht in Echt schließen
                e.Cancel = true;
                // das Fenster nur verstecken
                Hide();

            }

        }   // Ende: _CanvasWindow_Closing

    }   // Ende: public partial class CanvasWindow : Window

}   // Ende: namespace MatrixFFN.Tools
