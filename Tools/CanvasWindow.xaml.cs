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
    /// Interactions logic for CanvasFenster.xaml
    /// </summary>
    public partial class CanvasWindow : Window
    {
        /// <summary>
        /// created on: 08.07.2023
        /// last edit: 04.10.24
        /// </summary>
        public Version version = new Version("1.0.6");
        /// <summary>
        /// Flag to close the window for real at programs end.
        /// </summary>
        public bool isNowToEnd = false;

        // --------------------------------     Constructors

        /// <summary>
        /// Constructor. Take care '_canvasWindow' was defined in the XAML-text !
        /// ( x:Name="_canvasWindow" ). Used as zoom window.
        /// </summary>
        public CanvasWindow( )
        {
            InitializeComponent( );
            _canvasWindow.Show( );
            _canvasWindow.Close();

        }   // end: CanvasWindow ( constructor )

        /// <summary>
        /// Constructor. Take care '_canvasWindow' was defined in the XAML-text !
        /// ( x:Name="_canvasWindow" ). Used as zoom window.
        /// </summary>
        /// <param name="titelText">windows title</param>
        public CanvasWindow( string titelText )
        {
            InitializeComponent();
            _canvasWindow.Title = titelText;
            _canvasWindow.Show();
            _canvasWindow.Close();

        }   // end: CanvasWindow ( constructor )

        // ------------------------------------------------     Event handler

        /// <summary>
        /// Event handler for the 'Canvas' of the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CanvasWindowCanvas_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            
        }   // end: _CanvasWindowCanvas_SizeChanged

        /// <summary>
        /// Event handler for the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Window_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            if ( e.WidthChanged )
                _canvasWindowCanvas.Width =  _canvasWindow.Width - 20;
            if ( e.HeightChanged )
                _canvasWindowCanvas.Height = _canvasWindow.Height - 20;
            _CanvasWindowCanvas_SizeChanged( sender, e );

        }   // end: _Window_SizeChanged

        /// <summary>
        /// Event handler for the closing of the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CanvasWindow_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            if ( !isNowToEnd )
            {
                // don't close the window for real
                e.Cancel = true;
                // only hide it
                Hide();

            }

        }   // end: _CanvasWindow_Closing

    }   // end: public partial class CanvasWindow : Window

}   // end: namespace MatrixFFN.Tools
