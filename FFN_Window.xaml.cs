﻿/* ====================================================================
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

using MatrixFFN.Tools;
using NPOI.OpenXmlFormats.Dml;
using NPOI.SS.UserModel;
using NPOI.Util;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;


namespace MatrixFFN
{
    /// <summary>
    /// Interactionlogic for FFN_Window.xaml
    /// </summary>
    public partial class FFN_Window : Window
    {
        /// <summary>
        /// created on: 08.07.2023
        /// last edit: 02.10.24
        /// </summary>
        public Version version = new Version("1.0.13");
        /// <summary>
        /// local FFN
        /// </summary>
        FFN network;
        /// <summary>
        /// Input data for the network.
        /// </summary>
        public double[][] inputArrayField = new double[ 1 ][];
        /// <summary>
        /// Output data for the network.
        /// </summary>
        public double[][] outputArrayField = new double[ 1 ][];
        /// <summary>
        /// localised X-Values for the chart ( moving the 'inputArrayField' 
        /// to the right position in the chart )
        /// </summary>
        double[] xValues = new double[ 1 ];
        /// <summary>
        /// localised Y-Values for the chart ( part of 'outputArrayField' )
        /// </summary>
        double[] yValues = new double[ 1 ];
        /// <summary>
        /// local result of a 'Predict'
        /// </summary>
        double[] predictValues = new double[ 1 ];
        /// <summary>
        /// Chart for the function's and the 
        /// predict's values.
        /// </summary>
        CanvasChart canvasChartValues;
        /// <summary>
        /// Chart for the error values.
        /// </summary>
        CanvasChart canvasChartErrors;
        /// <summary>
        /// Showing the network's design ( layers structure )
        /// </summary>
        CanvasTopic canvasTopicNetLayers;
        /// <summary>
        /// Flag to close the window for real and not just to hide it
        /// any more.
        /// </summary>
        bool isNowToEnd = false;
        /// <summary>
        /// Connection to the wrapper for NPOI
        /// </summary>
        NPOIwrap.NPOIexcel myExcelFile = new NPOIwrap.NPOIexcel();
        /// <summary>
        /// Flag for the automatic training ( stop is 0, pause is 1, auto is 2 ). 
        /// </summary>
        int isAutomatic = 0;
        /// <summary>
        /// the Thread for the calculation
        /// </summary>
        Thread automaticLoopThread;

        /// <summary>
        /// Constructor to init all the components ( UI ).
        /// </summary>
        public FFN_Window()
        {
            InitializeComponent();
            SetStatusWorking( "Window is starting up...", 5 );

            canvasTopicNetLayers = new CanvasTopic( "a view of the net", 
                    ref _canvasNetLayers );

            string fullFileName = GetDirectory() + "FFN.network";
            network = new FFN( canvasTopicNetLayers.topicField, true, fullFileName );

            SetLabelFileName();
            SetTextBoxNetLayers( canvasTopicNetLayers.workingTopic );

            canvasChartValues = new CanvasChart( "chart function values",
                    ref _canvasValues );

            canvasChartErrors = new CanvasChart( "chart error values",
                    ref _canvasErrors );

            SetStatusCheckDone( "Window starting is done." );

            automaticLoopThread = new Thread( AutomaticLoop );
            automaticLoopThread.Priority = ThreadPriority.Lowest;

        }   // end: FFN_Window ( constructor )

        /// <summary>
        /// Delivers the working directory with the systems separator
        /// symbol.
        /// </summary>
        /// <returns>working directory...</returns>
        string GetDirectory()
        {
            string text =
                Directory.GetCurrentDirectory()
                + System.IO.Path.DirectorySeparatorChar;
            return (text);

        }   // end: GetDirectory

        /// <summary>
        /// Puts the correct filename into the label.
        /// </summary>
        void SetLabelFileName()
        {
            _labelFileName.Content = network.fileName;

        }   // end: SetLabelFileName

        /// <summary>
        /// Puts the text into the 'TextBox' for typing
        /// in the network's topic.
        /// </summary>
        /// <param name="text">wanted text</param>
        void SetTextBoxNetLayers(string text)
        {
            _textBoxNetLayers.Text = text;

        }   // end: SetTextBoxNetLayers

        /// <summary>
        /// Helper function converting an array field to string.
        /// </summary>
        /// <param name="dataField">the data set</param>
        /// <param name="linebreak">newline after every array ?</param>
        /// <returns>the stringrepresentation</returns>
        string ArrayToString(double[][] dataField, bool linebreak = false )
        {
            string text = "";

            foreach (double[] dataArray in dataField)
            {
                text += $" [ {string.Join(", ", dataArray)} ] ";
                if ( linebreak )
                    text += "\n";

            }
            text += "\n";
            return (text);


        }   // end: ArrayToString


        /// <summary>
        /// Helper function serving the 'text' to the 'TextBox'.
        /// Mostly used for the 'Fit'-string.
        /// </summary>
        /// <param name="text">the message</param>
        private void ShowText( string text )
        {
            _textBlockOutput.Text = text;

        }   // end: ShowText

        /// <summary>
        /// Shows the two lines in the chart windows. Usually done
        /// after 'Predict' ( called from it ).
        /// </summary>
        /// <param name="titelText">the special header</param>
        /// <param name="predictArray">results of the predict for the chosen input/output nodes</param>
        public void ShowPredict( string titelText, double[] predictArray )
        {
            canvasChartValues.DataClear();
            canvasChartValues.titelText = titelText;
            canvasChartValues.DataAdd( xValues, yValues );
            canvasChartValues.DataAdd( xValues, predictArray );
            canvasChartValues.ShowChart();

            canvasChartErrors.DataClear();
            canvasChartErrors.titelText = "Epochenanzahl zu FehlerSumme";
            canvasChartErrors.DataAdd( network.listEpochs, network.listErrorAmount );
            canvasChartErrors.SetShowNoOfData( 10 );
            canvasChartErrors.ShowChart();

        }   // end: ShowPredict

        // --------------------------------------       StatusBar

        /// <summary>
        /// Helper function for the text ion the statusbar.
        /// </summary>
        /// <param name="neuerText">the new text</param>
        void SetStatusText( string neuerText )
        {
            _statusText.Content = neuerText;
            _statusText.UpdateLayout();

        }   // end: SetStatusText

        /// <summary>
        /// Helper function for the percentage of the progress bar.
        /// </summary>
        /// <param name="prozent">percentage</param>
        void SetStatusProgress( int prozent )
        {
            _statusProgress.Value = prozent;
            _statusProgress.UpdateLayout();

        }   // end: SetStatusProgress

        /// <summary>
        /// The color 'red' for the 'CheckBox' status bar and
        /// the message text in it. Start means he is no longer idle ( 'green' ).
        /// </summary>
        /// <param name="text">text in the status bar</param>
        void SetStatusCheckStart( string text )
        {
            _statusCheck.Background = Brushes.Red;
            _statusCheck.UpdateLayout();
            SetStatusText( text );
            SetStatusProgress( 0 );

        }   // end: SetStatusCheckStart

        /// <summary>
        /// The color 'green' for the 'CheckBox' status bar and
        /// the message text in it. Done means he is now idle ( not 'red' ).
        /// </summary>
        /// <param name="text">text in the status bar</param>
        public void SetStatusCheckDone( string text )
        {
            _statusCheck.Background = Brushes.Green;
            _statusCheck.UpdateLayout();
            SetStatusText( text );
            SetStatusProgress( 100 );

        }   // end: SetStatusCheckDone

        /// <summary>
        /// The at work color 'orange' for the 'CheckBox' status bar and
        /// the message text in it. The percentage for the progress.
        /// </summary>
        /// <param name="text">text in the status bar</param>
        /// <param name="prozent">percentage of the progress</param>
        public void SetStatusWorking( string text, int prozent )
        {
            _statusCheck.Background = Brushes.Orange;
            _statusCheck.UpdateLayout();
            SetStatusText( text );
            SetStatusProgress( prozent );
            _statusBar.UpdateLayout();

        }   // end: SetStatusWorking

        /// <summary>
        /// For the openess the choice of data source has to
        /// be cared for.
        /// </summary>
        /// <returns>0 is no choice, 1 is internal, 2 is loaded</returns>
        public int GetStatusDatasetCheck( )
        {
            int result = 0;
            if ( _datasetCheckParabel.IsChecked == true )
            {
                result = 1;
                _datasetCheckLoad.IsChecked = false;
            }
            if ( _datasetCheckLoad.IsChecked == true )
            {
                result = 2;
                _datasetCheckParabel.IsChecked= false;
            }

            return ( result );

        }   // end: GetStatusDatasetCheck

        // --------------------------------     AutomaticLoop

        /// <summary>
        /// Threadable function for the automatic training. The thread 
        /// is nice in .Net-ways -
        /// finishing every action in its window. Being hold on priority settings.
        /// </summary>
        public void AutomaticLoop()
        {
            if ( ( network.listEpochs.Count < 1 )
                && ( isAutomatic == 2 ) )
            {   // no automatic training for new networks
                MessageBox.Show( "Nothing learned yet. DO 'Train' ONCE !",
                    "Training error", MessageBoxButton.OK, MessageBoxImage.Error );
                isAutomatic = 1;
                automaticLoopThread.Priority = ThreadPriority.Lowest;

            }
            // training in intervalls ( save/reload for the better approximation )
            bool networkOK = true;
            // order the 'CheckBox's
            networkOK &= ( ( _datasetCheckLoad.IsChecked == true )
                    || ( _datasetCheckParabel.IsChecked == true ) );
            networkOK &= ( _topicCheck.IsChecked == true );
            networkOK &= ( _initCheck.IsChecked == true );
            networkOK &= ( isAutomatic == 2 );
            networkOK &= ( network.epochsNumber > 0 );
            // status has to be ok
            if ( networkOK )
            {
                double errorNow = network.listErrorAmount.Last();
                int datasetChoice = GetStatusDatasetCheck();
                int epochsToFit = int.Parse( _textBoxInputEpochs.Text );
                // intern example's dataset is always there from here on
                if ( datasetChoice == 1 )
                    network.Fit( inputArrayField, outputArrayField, epochsToFit );
                // loading the old network is not destroying the local data
                if ( datasetChoice == 2 )
                    network.Fit_LocalData( epochsToFit );
                if ( errorNow <= network.listErrorAmount.Last() )
                    network.LoadData( network.fileName );
                else
                    network.SaveData( network.fileName );
                ShowText( network.fitText );
                _ButtonPredict_Click( new object(), new RoutedEventArgs() );

            }

        }   // end: AutomaticLoop

        // -----------------------------------      Eventhandling

        /// <summary>
        /// Event handler for the closing of the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            if ( !isNowToEnd )
            {
                // don't do anything ( event canceled )
                e.Cancel = true;
                // only hide the window
                Hide();

            }
            else
            {
                canvasTopicNetLayers.isNowToEnd = true;
                canvasTopicNetLayers.Close();
                canvasChartValues.isNowToEnd = true;
                canvasChartValues.Close();
                canvasChartErrors.isNowToEnd = true;
                canvasChartErrors.Close();

            }

        }   // end: _Window_Closing

        /// <summary>
        /// Event handler: _ButtonLoad_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonLoad_Click( object sender, RoutedEventArgs e )
        {
            SetStatusWorking( "loading using standard filename... ", 25 );
            SetLabelFileName();
            if ( File.Exists( network.fileName ) )
            {
                network.LoadData( network.fileName );
                // show the loaded data
                canvasTopicNetLayers.workingTopic = network.workingTopic;
                canvasTopicNetLayers.ShowTopic();

                _textBoxNetLayers.Text = canvasTopicNetLayers.workingTopic;
                _initCheck.IsChecked = true;

            }
            SetStatusCheckDone( "done loading using standard filename." );

        }   // end: _ButtonLoad_Click

        /// <summary>
        /// Event handler: _ButtonLoadOf_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonLoadOf_Click( object sender, RoutedEventArgs e )
        {
            SetStatusWorking( "loading with chosen filename...", 25 );
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "FFN"; // Default file name
            dialog.DefaultExt = ".network"; // Default file extension
            dialog.Filter = "network save file (.network)|*.network"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if ( result == true )
            {
                // Open document
                network.fileName = dialog.FileName;
                SetLabelFileName();
                network.LoadData( network.fileName );
                // show the loaded data
                canvasTopicNetLayers.workingTopic = network.workingTopic;
                canvasTopicNetLayers.ShowTopic();

                _textBoxNetLayers.Text = canvasTopicNetLayers.workingTopic;
                _initCheck.IsChecked = true;

            }
            SetStatusCheckDone( "done loading with chosen filename." );

        }   // end: _ButtonLoadOf_Click

        /// <summary>
        /// Event handler: _ButtonSave_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonSave_Click( object sender, RoutedEventArgs e )
        {
            SetStatusWorking( "saving with standard filename...", 25 );
            SetLabelFileName();
            network.SaveData( network.fileName );
            _ButtonPredict_Click( sender, e );
            SetStatusCheckDone( "done saving with standard filename." );

        }   // end: _ButtonSave_Click

        /// <summary>
        /// Event handler: _ButtonSaveAs_Click
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _ButtonSaveAs_Click( object sender, RoutedEventArgs e )
        {
            SetStatusWorking( "saving network with chosen filename...", 25 );
            // Configure save file dialog box
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FileName = "FFN"; // Default file name
            dialog.DefaultExt = ".network"; // Default file extension
            dialog.Filter = "network save file (.network)|*.network"; // Filter files by extension

            // Show save file dialog box
            bool? result = dialog.ShowDialog();

            // Process save file dialog box results
            if ( result == true )
            {
                // Save document
                network.fileName = dialog.FileName;
                SetLabelFileName();
                network.SaveData( network.fileName );

            }
            SetStatusCheckDone( "done saving network with chosen filename." );

        }   // end: _ButtonSaveAs_Click

        /// <summary>
        /// Event handler: Textänderung in der TextBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TextBoxNetLayers_TextChanged( object sender, TextChangedEventArgs e )
        {
            _initCheck.IsChecked = false;
            if ( canvasTopicNetLayers.ParseTopic( _textBoxNetLayers.Text,
                    ref network.layersTopic ) )
                _topicCheck.IsChecked = true;
            else
                _topicCheck.IsChecked = false;

            if ( _topicCheck.IsChecked == true )
            {
                canvasTopicNetLayers.workingTopic = _textBoxNetLayers.Text;
                if ( _datasetCheckParabel.IsChecked == true ) 
                    if ( canvasTopicNetLayers.ParseDataIntoTopic( inputArrayField, 
                            outputArrayField ) )
                        SetTextBoxNetLayers( canvasTopicNetLayers.workingTopic );
                if ( _datasetCheckLoad.IsChecked == true )
                    if ( canvasTopicNetLayers.ParseLocalDataIntoTopic( 
                            network.localIns, network.localOuts ) )
                        SetTextBoxNetLayers( canvasTopicNetLayers.workingTopic );

                canvasTopicNetLayers.ShowTopic();

            }

        }   // end: _TextBoxNetLayers_TextChanged

        /// <summary>
        /// Event handler: _ButtonDatasetParabel_Click
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _ButtonDatasetParabel_Click( object sender, RoutedEventArgs e )
        {
            SetStatusWorking( "creating test dataset: parabel", 25 );
            // order the 'CheckBox's
            _datasetCheckLoad.IsChecked = false;
            _datasetCheckParabel.IsChecked = true;
            _initCheck.IsChecked = false;
            _textBoxShowIn.Text = "0";
            _textBoxShowOut.Text = "0";
            canvasChartValues.useLines = true;
            
            canvasChartValues.DataClear();
            inputArrayField = new double[ 21 ][];
            outputArrayField = new double[ 21 ][];
            xValues = new double[ 21 ];
            yValues = new double[ 21 ];
            int showIn = int.Parse( _textBoxShowIn.Text );
            int showOut = int.Parse( _textBoxShowOut.Text );
            for ( int pos = 0; pos < 21; pos++ )
            {
                inputArrayField[ pos ] = new double[ 2 ]
                    { ( pos - 10 ), ( pos - 10 ) };
                xValues[ pos ] = inputArrayField[ pos ][ showIn ];
                outputArrayField[ pos ] = new double[ 1 ]
                    { ( Math.Pow( pos - 10, 2 ) ) };
                yValues[ pos ] = outputArrayField[ pos ][ showOut ];
            }

            if ( canvasTopicNetLayers.ParseDataIntoTopic( inputArrayField, outputArrayField ) )
            {   // dataset created, show it now
                canvasChartValues.titelText = "Parabel [ -10, 10 ]";
                canvasChartValues.DataAdd( xValues, yValues );
                canvasChartValues.ShowChart();
                canvasTopicNetLayers.ShowTopic();

            }
            SetStatusCheckDone( "done creating test dataset: parabel." );

        }   // end: _ButtonDatasetParabel_Click

        /// <summary>
        /// Try to get a double-click ( tricky input pattern )
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CanvasNetLayers_MouseDown( object sender, MouseButtonEventArgs e )
        {
            // try to get double-click -> it will open a window
            if ( e.ChangedButton == MouseButton.Left && e.ClickCount == 2 )
            {   // 2 clicks
                canvasTopicNetLayers.ShowWindow();

            }

        }   // end: _CanvasNetLayers_MouseDown

        /// <summary>
        /// Try to get a double-click ( tricky input pattern )
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CanvasValues_MouseDown( object sender, MouseButtonEventArgs e )
        {
            // try to get double-click -> it will open a window
            if ( e.ChangedButton == MouseButton.Left && e.ClickCount == 2 )
            {   // 2 clicks
                canvasChartValues.ShowWindow();

            }

        }   // end: _CanvasValues_MouseDown

        /// <summary>
        /// Try to get a double-click ( tricky input pattern )
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CanvasErrors_MouseDown( object sender, MouseButtonEventArgs e )
        {
            // try to get double-click -> it will open a window
            if ( e.ChangedButton == MouseButton.Left && e.ClickCount == 2 )
            {   // 2 clicks
                canvasChartErrors.ShowWindow();

            }

        }   // end: _CanvasErrors_MouseDown

        /// <summary>
        /// Event handler: _ButtonInit_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonInit_Click( object sender, RoutedEventArgs e )
        {
            SetStatusWorking( "creating the new network ...", 25 );
            _initCheck.IsChecked = false;
            int checkedNr = GetStatusDatasetCheck();
            if ( canvasTopicNetLayers.ParseTopic( canvasTopicNetLayers.workingTopic,
                        ref network.layersTopic ) )
                return;
            if ( checkedNr > 0 )
                if ( ( _datasetCheckParabel.IsChecked == true ) 
                    || ( _datasetCheckLoad.IsChecked == true ) )
            {
                _initCheck.IsChecked = true;
                string fullFileName = GetDirectory() + "FFN.network";
                network = new FFN( canvasTopicNetLayers.topicField, true, fullFileName );

            }
            SetStatusCheckDone( "done creating the new network." );

        }   // end: _ButtonInit_Click

        /// <summary>
        /// Event handler: _ButtonPredict_Click
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _ButtonPredict_Click( object sender, RoutedEventArgs e )
        {
            int showIn = int.Parse( _textBoxShowIn.Text );
            int showOut = int.Parse( _textBoxShowOut.Text );
            if ( ( _initCheck.IsChecked == true )
                && ( _datasetCheckParabel.IsChecked == true ) )
            {
                var predictArray = new double[ xValues.Length ];
                for ( int pos = 0; pos < xValues.Length; pos++ )
                {
                    double[] result = network.Predict( inputArrayField[ pos ] );
                    predictArray[ pos ] = Math.Round( result[ showOut ], 2 );

                }

                ShowPredict( "Parabel [ -10, 10 ] + Predict", predictArray );

            }

            if ( ( _initCheck.IsChecked == true )
                && ( _datasetCheckLoad.IsChecked == true ) )
            {
                var predictArray = new double[ xValues.Length ];
                for ( int pos = 0; pos < xValues.Length; pos++ )
                {
                    double[] result = network.Predict( inputArrayField[ pos ] );
                    predictArray[ pos ] = Math.Round( result[ showOut ], 2 );

                }

                ShowPredict( 
                    $"nodes# input: {showIn} output: {showOut} + Predict",
                    predictArray );

            }
            SetStatusCheckDone( "Predict beendet." );

        }   // end: _ButtonPredict_Click

        /// <summary>
        /// Event handler: _ButtonDatasetLoad_Click
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _ButtonDatasetLoad_Click( object sender, RoutedEventArgs e )
        {
            // order the 'CheckBox's
            _datasetCheckLoad.IsChecked = false;
            _datasetCheckParabel.IsChecked = false;
            _topicCheck.IsChecked = false;
            canvasChartValues.useLines = false;
            // network input/output has to be compatible or what?
            if ( _initCheck.IsChecked == false )
                return;
            // if it is inititet
            try
            {
                network.LoadDataFromExcel( "" );

            }
            catch ( ArgumentException aEx )
            {
                string boxText = $"{aEx.Message} -> \n{aEx.ParamName}";
                MessageBox.Show( boxText,
                    "Excel file's data does not fit !", 
                    MessageBoxButton.OK, MessageBoxImage.Warning );
            }
            // order the 'CheckBox's
            _datasetCheckLoad.IsChecked = true;

            xValues = new double[ network.localInputArrayField.Length ];
            yValues = new double[ network.localOutputArrayField.Length ];
            int showIn = int.Parse( _textBoxShowIn.Text );
            int showOut = int.Parse( _textBoxShowOut.Text );
            for ( int pos = 0; pos < network.localInputArrayField.Length; pos++ )
            {
                xValues[ pos ] = network.localInputArrayField[ pos ][ showIn ];
                yValues[ pos ] = network.localOutputArrayField[ pos ][ showOut ];
            }
            if ( canvasTopicNetLayers.ParseDataIntoTopic( inputArrayField, outputArrayField ) )
            {   // dataset created, show it now
                canvasChartValues.titelText = 
                    $"nodes# input: {showIn} output: {showOut} ";
                canvasChartValues.DataAdd( xValues, yValues );
                canvasChartValues.ShowChart();
                canvasTopicNetLayers.ShowTopic();

            }

            _topicCheck.IsChecked = true;

        }   // end: _ButtonDatasetLoad_Click

        /// <summary>
        /// Event handler: _datasetCheckParabel_Click
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _datasetCheckParabel_Click( object sender, RoutedEventArgs e )
        {
            if ( _datasetCheckLoad.IsChecked == true ) 
                _datasetCheckLoad.IsChecked = false;

        }   // end: _datasetCheckParabel_Click

        /// <summary>
        /// Event handler: _datasetCheckLoad_Click
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _datasetCheckLoad_Click( object sender, RoutedEventArgs e )
        {
            if ( _datasetCheckParabel.IsChecked == true )
                _datasetCheckParabel.IsChecked = false;

        }   // end: _datasetCheckLoad_Click

        /// <summary>
        /// Event handler: _TextBoxInputEpochs_PreviewTextInput
        /// <para>textinput from any source works via the preview-versions</para>
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _TextBoxInputEpochs_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            Regex regex = new Regex("[0-9]+");
            e.Handled = !regex.IsMatch( e.Text );

        }   // end: _TextBoxInputEpochs_PreviewTextInput

        /// <summary>
        /// Event handler: _ButtonAutomaticTraining_Click
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _ButtonAutomaticTraining_Click( object sender, RoutedEventArgs e )
        {
            // training in intervalls ( start/resume for the 'automaticLoopThread' )
            switch ( isAutomatic )
            {
                case 0:
                    // new start
                    isAutomatic = 2;
                    automaticLoopThread.Start();
                    automaticLoopThread.Priority = ThreadPriority.Highest;
                    break;
                case 1:
                    // resume thread
                    isAutomatic = 2;
                    automaticLoopThread.Priority = ThreadPriority.Highest;
                    break;
                case 2:
                    // new pause
                    isAutomatic = 1;
                    automaticLoopThread.Join();
                    automaticLoopThread.Priority = ThreadPriority.Lowest;
                    break;

            }

        }   // end: _ButtonAutomaticTraining_Click

        /// <summary>
        /// Event handler: _TextBoxNoInput_PreviewTextInput
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _TextBoxNoInput_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            Regex regex = new Regex("[0-9]+");
            e.Handled = !regex.IsMatch( e.Text );

        }   // end: _TextBoxNoInput_PreviewTextInput

        /// <summary>
        /// Event handler: _ShowLikeCheck_Checked
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _ShowLikeCheck_Checked( object sender, RoutedEventArgs e )
        {

        }   // end: _ShowLikeCheck_Checked

        /// <summary>
        /// Event handler: _ButtonTrain_Click
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _ButtonTrain_Click( object sender, RoutedEventArgs e )
        {
            bool networkOK = true;
            // order the 'CheckBox's
            networkOK &= ( ( _datasetCheckLoad.IsChecked == true )
                    || ( _datasetCheckParabel.IsChecked == true ) );
            networkOK &= ( _topicCheck.IsChecked == true );
            networkOK &= ( _initCheck.IsChecked == true );
            // status has to be ok
            if ( networkOK )
            {
                int datasetChoice = GetStatusDatasetCheck();
                int epochsToFit = int.Parse( _textBoxInputEpochs.Text );
                string fitText = "";
                if ( datasetChoice == 1 )
                    fitText = network.Fit( inputArrayField, outputArrayField, epochsToFit );
                if ( datasetChoice == 2 )
                    fitText = network.Fit_LocalData( epochsToFit );
                network.SaveData( network.fileName );
                ShowText( fitText );
                _ButtonPredict_Click( sender, e );

            }

        }   // end: _ButtonTrain_Click

        /// <summary>
        /// Event handler: _ButtonAutomaticTrainingPause_Click
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _ButtonAutomaticTrainingPause_Click( object sender, RoutedEventArgs e )
        {
            switch ( isAutomatic )
            {
                case 2:
                    isAutomatic = 1;
                    automaticLoopThread.Join();
                    automaticLoopThread.Priority = ThreadPriority.Lowest;
                    break;
                case 1:
                    isAutomatic = 2;
                    automaticLoopThread.Priority = ThreadPriority.Highest;
                    break;

            }

        }   // end: _ButtonAutomaticTrainingPause_Click

        /// <summary>
        /// Event handler: _ButtonAutomaticTrainingStop_Click
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _ButtonAutomaticTrainingStop_Click( object sender, RoutedEventArgs e )
        {
            isAutomatic = 0;
            automaticLoopThread.Join();
            automaticLoopThread.Priority= ThreadPriority.Lowest;
            
        }   // end: _ButtonAutomaticTrainingStop_Click

        /// <summary>
        /// Event handler: _TextBoxShowIn_PreviewTextInput
        /// <para>textinput from any source works via the preview-versions</para>
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _TextBoxShowIn_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            Regex regex = new Regex("[0-9]+");
            e.Handled = !regex.IsMatch( e.Text );

        }   // end: _TextBoxShowIn_PreviewTextInput

        /// <summary>
        /// Event handler: _TextBoxShowOut_PreviewTextInput
        /// <para>textinput from any source works via the preview-versions</para>
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _TextBoxShowOut_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            Regex regex = new Regex("[0-9]+");
            e.Handled = !regex.IsMatch( e.Text );

        }   // end: _TextBoxShowOut_PreviewTextInput

        /// <summary>
        /// Event handler: _TextBoxShowIn_TextChanged
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _TextBoxShowIn_TextChanged( object sender, TextChangedEventArgs e )
        {
            if ( !_initCheck.IsChecked == true )
                return;
            int showIn = int.Parse( _textBoxShowIn.Text );
            int showOut = int.Parse( _textBoxShowOut.Text );
            if ( showIn < 0 )
                showIn = 0;
            if ( showIn >= xValues.Length )
                showIn = ( xValues.Length - 1 );
            if ( showOut < 0 )
                showOut = 0;
            if ( showOut >= yValues.Length )
                showOut = ( yValues.Length - 1 );
            if ( ( _initCheck.IsChecked == true )
                && ( _datasetCheckParabel.IsChecked == true ) )
            {
                for ( int pos = 0; pos < xValues.Length; pos++ )
                {
                    xValues[ pos ] = inputArrayField[ pos ][ showIn ];
                    yValues[ pos ] = outputArrayField[ pos ][ showOut ];
                }

            }   // end: _datasetCheckParabel.IsChecked

            if ( ( _initCheck.IsChecked == true )
                && ( _datasetCheckLoad.IsChecked == true ) )
            {
                for ( int pos = 0; pos < xValues.Length; pos++ )
                {
                    xValues[ pos ] = network.localInputArrayField[ pos ][ showIn ];
                    yValues[ pos ] = network.localOutputArrayField[ pos ][ showOut ];
                }

            }   // end: _datasetCheckLoad.IsChecked

        }   // end: _TextBoxShowIn_TextChanged

        /// <summary>
        /// Event handler: _TextBoxShowOut_TextChanged
        /// </summary>
        /// <param name="sender">Quelle</param>
        /// <param name="e">Quellparameter</param>
        private void _TextBoxShowOut_TextChanged( object sender, TextChangedEventArgs e )
        {
            if ( !_initCheck.IsChecked == true )
                return;
            int showIn = int.Parse( _textBoxShowIn.Text );
            int showOut = int.Parse( _textBoxShowOut.Text );
            if ( showIn < 0 )
                showIn = 0;
            if ( showIn >= xValues.Length )
                showIn = ( xValues.Length - 1 );
            if ( showOut < 0 )
                showOut = 0;
            if ( showOut >= yValues.Length )
                showOut = ( yValues.Length - 1 );
            if ( _datasetCheckParabel.IsChecked == true )
            {
                for ( int pos = 0; pos < xValues.Length; pos++ )
                {
                    xValues[ pos ] = inputArrayField[ pos ][ showIn ];
                    yValues[ pos ] = outputArrayField[ pos ][ showOut ];
                }

            }   // end: _datasetCheckParabel.IsChecked

            if ( _datasetCheckLoad.IsChecked == true )
            {
                for ( int pos = 0; pos < xValues.Length; pos++ )
                {
                    xValues[ pos ] = network.localInputArrayField[ pos ][ showIn ];
                    yValues[ pos ] = network.localOutputArrayField[ pos ][ showOut ];
                }

            }   // end: _datasetCheckLoad.IsChecked

        }   // end: _TextBoxShowOut_TextChanged

    }   // end: partial class FFN_Window

}   // end: namespace MatrixFFN

