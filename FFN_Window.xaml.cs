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


// Ignore Spelling: FFN

using MatrixFFN.Tools;
using NPOI.OpenXmlFormats.Dml;
using NPOI.POIFS.Properties;
using NPOI.SS.UserModel;
using NPOI.Util;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace MatrixFFN
{
    /// <summary>
    /// Interaction logic for FFN_Window.xaml
    /// </summary>
    public partial class FFN_Window : Window
    {
        /// <summary>
        /// created on: 08.07.2023
        /// last edit: 09.10.24
        /// </summary>
        public Version version = new("1.0.16");
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
        /// localized X-Values for the chart ( moving the 'inputArrayField' 
        /// to the right position in the chart )
        /// </summary>
        double[] xValues = new double[ 1 ];
        /// <summary>
        /// localized Y-Values for the chart ( part of 'outputArrayField' )
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
        CanvasChart canvasChartWindow_Values;
        /// <summary>
        /// Chart for the error values.
        /// </summary>
        CanvasChart canvasChartWindow_Errors;
        /// <summary>
        /// Showing the network's design ( layers structure )
        /// </summary>
        CanvasTopic canvasTopicWindow_NetLayers;
        /// <summary>
        /// Flag to close the window for real and not just to hide it
        /// any more.
        /// </summary>
        public bool isNowToEnd = false;
        /// <summary>
        /// Flag for the automatic training ( stop is 0, pause is 1, auto is 2 ). 
        /// </summary>
        int isAutomatic = 0;
        /// <summary>
        /// the working directory
        /// </summary>
        string workingDirectory;
        /// <summary>
        /// timer for the automatic loop
        /// </summary>
        Thread? autoLoopThread;
        int epochsToFit;

        /// <summary>
        /// Constructor to init all the components ( UI ).
        /// </summary>
        public FFN_Window() 
        {
            InitializeComponent();
            ForwardUiSetStatusWorking( "Window is starting up...", 5 );

            canvasTopicWindow_NetLayers = new CanvasTopic( "a view of the net", 
                    ref _canvasNetLayers );

            workingDirectory = GetDirectory( );
            string fullFileName = workingDirectory + "FFN.network";
            network = new FFN( canvasTopicWindow_NetLayers.topicField, true, fullFileName );

            SetLabelFileName();
            SetTextBoxNetLayers( canvasTopicWindow_NetLayers.workingTopic );

            canvasChartWindow_Values = new CanvasChart( "chart function values",
                    ref _canvasValues );

            canvasChartWindow_Errors = new CanvasChart( "chart error values",
                    ref _canvasErrors );

            ForwardUiStatusCheckDone( "Window starting is done." );

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
        /// <returns>the string representation</returns>
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
        private void ForwardUiShowText( string text )
        {
            Action forward = new Action( () =>
            {
                _textBlockOutput.Text = text;
            } );
            Dispatcher.Invoke( forward );

        }   // end: ForwardUiShowText

        /// <summary>
        /// Shows the two lines in the chart windows. Usually done
        /// after 'Predict' ( called from it ).
        /// </summary>
        /// <param name="titleText">the special header</param>
        /// <param name="predictArray">results of the predict for the chosen input/output nodes</param>
        public void ForwardUiShowPredict( string titleText, double[] predictArray )
        {
            Action forward = new Action( () =>
            {
                canvasChartWindow_Values.DataClear();
                canvasChartWindow_Values.titleText = titleText;
                canvasChartWindow_Values.DataAdd( xValues, yValues );
                canvasChartWindow_Values.DataAdd( xValues, predictArray );
                canvasChartWindow_Values.ShowChart();

                canvasChartWindow_Errors.DataClear();
                canvasChartWindow_Errors.titleText = "epochs number to error sum";
                canvasChartWindow_Errors.DataAdd( network.listEpochs, network.listErrorAmount );
                canvasChartWindow_Errors.SetShowNoOfData( 10 );
                canvasChartWindow_Errors.ShowChart();

            } );
            Dispatcher.Invoke( forward );

        }   // end: ForwardUiShowPredict

        // -----------------    UI query/forward StatusBar

        /// <summary>
        /// Helper function for the text ion the status bar.
        /// </summary>
        /// <param name="neuerText">the new text</param>
        void ForwardUiStatusText( string neuerText )
        {
            Action forward = new Action( () =>
            {
                _statusText.Content = neuerText;
                _statusText.UpdateLayout();
            } );
            Dispatcher.Invoke( forward );

        }   // end: ForwardUiStatusText

        /// <summary>
        /// Helper function for the percentage of the progress bar.
        /// </summary>
        /// <param name="percent">percentage</param>
        void ForwardUiStatusProgress( int percent )
        {
            Action forward = new Action( () =>
            {
                _statusProgress.Value = percent;
                _statusProgress.UpdateLayout();

            } );
            Dispatcher.Invoke( forward );

        }   // end: ForwardUiStatusProgress

        /// <summary>
        /// The color 'red' for the 'CheckBox' status bar and
        /// the message text in it. Start means he is no longer idle ( 'green' ).
        /// </summary>
        /// <param name="text">text in the status bar</param>
        void ForwardUiStatusCheckStart( string text )
        {
            Action forward = new Action( ( ) =>
            {
                _statusCheck.Background = Brushes.Red;
                _statusCheck.UpdateLayout();
                ForwardUiStatusText( text );
                ForwardUiStatusProgress( 0 );
            } );
            Dispatcher.Invoke( forward );

        }   // end: ForwardUiStatusCheckStart

        /// <summary>
        /// The color 'green' for the 'CheckBox' status bar and
        /// the message text in it. Done means he is now idle ( not 'red' ).
        /// </summary>
        /// <param name="text">text in the status bar</param>
        public void ForwardUiStatusCheckDone( string text )
        {
            Action forward = new Action( ( ) =>
            {
                _statusCheck.Background = Brushes.Green;
                _statusCheck.UpdateLayout();
                ForwardUiStatusText( text );
                ForwardUiStatusProgress( 100 );

            }
                );
            Dispatcher.Invoke( forward );

        }   // end: ForwardUiStatusCheckDone

        /// <summary>
        /// The at work color 'orange' for the 'CheckBox' status bar and
        /// the message text in it. The percentage for the progress.
        /// </summary>
        /// <param name="text">text in the status bar</param>
        /// <param name="percent">percentage of the progress</param>
        public void ForwardUiSetStatusWorking( string text, int percent )
        {
            Action forward = new Action( ( ) =>
            {
                _statusCheck.Background = Brushes.Orange;
                _statusCheck.UpdateLayout();
                ForwardUiStatusText( text );
                ForwardUiStatusProgress( percent );
                _statusBar.UpdateLayout();

            }
                );
            Dispatcher.Invoke( forward );

        }   // end: ForwardUiSetStatusWorking

        // --------------------------------     AutomaticLoop

        /// <summary>
        /// Thread able function for the automatic training. The thread 
        /// is nice in .Net-ways -
        /// finishing every action in its window. Being hold on priority settings.
        /// </summary>
        public void AutomaticLoop( )
        {
            if ( ( network.listEpochs.Count < 1 )
                && ( isAutomatic == 2 ) )
            {   // no automatic training for new networks
                MessageBox.Show( "Nothing learned yet. DO 'Train' ONCE !",
                    "Training error", MessageBoxButton.OK, MessageBoxImage.Error );
                isAutomatic = 1;

            }
            while ( isAutomatic > 1 )
            {
                // training in intervals ( save/reload for the better approximation )
                double errorNow = network.listErrorAmount.Last();
                int datasetChoice = QueryUiDatasetCheck();
                int epochsToFit = QueryUiEpochsToTrain();
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
                ForwardUiShowText( network.fitText );
                ForwardUiPredictNow();

            }

        }   // end: AutomaticLoop

        //  ------------------------------- UI query/forward

        /// <summary>
        /// For the openness the choice of data source has to
        /// be cared for.
        /// </summary>
        /// <returns>0 is no choice, 1 is internal, 2 is loaded</returns>
        public int QueryUiStatusDatasetCheck( )
        {
            int result = 0;
            Action forward = new Action( ( ) =>
            {
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

            } );
            Dispatcher.Invoke( forward );

            return ( result );

        }   // end: QueryUiStatusDatasetCheck

        /// <summary>
        /// UI query.
        /// </summary>
        /// <returns>StatusDatasetCheck</returns>
        public int QueryUiDatasetCheck()
        {
            int result = 0;
            Action query = new Action( ( ) => 
                { result = QueryUiStatusDatasetCheck(); } 
                );
            Dispatcher.Invoke( query );
            return ( result );

        }   // end: QueryUiDatasetCheck

        /// <summary>
        /// UI send.
        /// </summary>
        public void ForwardUiPredictNow( )
        {
            Action forward = new Action( ( ) =>
                { _ButtonPredict_Click( new object(), new RoutedEventArgs() ); }
                );
            Dispatcher.Invoke( forward );

        }   // end: ForwardUiPredictNow

        /// <summary>
        /// UI query
        /// </summary>
        /// <returns>_textBoxInputEpochs.Text</returns>
        public int QueryUiEpochsToTrain()
        {
            int result = 0;
            Action query = new Action( () =>
            {
                result = 

            });

            return ( result );

        }   // end: QueryUiEpochsToTrain

        /// <summary>
        /// UI query
        /// </summary>
        /// <returns>_textBoxShowIn.Text</returns>
        public int QueryUiShowIn( )
        {
            int result = 0;
            Action query = new Action( ( ) =>
            {
                result = int.Parse( _textBoxShowIn.Text );
            }
                );
            Dispatcher.Invoke( query );
            return ( result );

        }   // end: QueryUiShowIn

        /// <summary>
        /// UI query
        /// </summary>
        /// <returns>_textBoxShowOut.Text</returns>
        public int QueryUiShowOut( )
        {
            int result = 0;
            Action query = new Action( ( ) =>
            {
                result = int.Parse( _textBoxShowOut.Text );
            }
                );
            Dispatcher.Invoke( query );
            return ( result );

        }   // end: QueryUiShowOut

        /// <summary>
        /// UI query
        /// </summary>
        /// <returns>_initCheck.IsChecked == true</returns>
        public bool QueryUiInitCheck( )
        {
            bool result = false;
            Action query = new Action( ( ) =>
            {
                result = ( _initCheck.IsChecked == true );
            }
                );
            Dispatcher.Invoke( query );
            return ( result );

        }   // end: QueryUiInitCheck

        /// <summary>
        /// UI query
        /// </summary>
        /// <returns>_datasetCheckParabel.IsChecked == true</returns>
        public bool QueryUiDatasetCheckParabel( )
        {
            bool result = false;
            Action query = new Action( ( ) =>
            {
                result = ( _datasetCheckParabel.IsChecked == true );
            }
                );
            Dispatcher.Invoke( query );
            return ( result );

        }   // end: QueryUiDatasetCheckParabel

        /// <summary>
        /// UI query
        /// </summary>
        /// <returns>_datasetCheckLoad.IsChecked == true</returns>
        public bool QueryUiDatasetCheckLoad( )
        {
            bool result = false;
            Action query = new Action( ( ) =>
            {
                result = ( _datasetCheckLoad.IsChecked == true );
            }
                );
            Dispatcher.Invoke( query );
            return ( result );

        }   // end: QueryUiDatasetCheckLoad

        // -----------------------------------      Event handling

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
                canvasTopicWindow_NetLayers.isNowToEnd = true;
                canvasTopicWindow_NetLayers.Close();
                canvasChartWindow_Values.isNowToEnd = true;
                canvasChartWindow_Values.Close();
                canvasChartWindow_Errors.isNowToEnd = true;
                canvasChartWindow_Errors.Close();

            }

        }   // end: _Window_Closing

        /// <summary>
        /// Event handler: _ButtonLoad_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonLoad_Click( object sender, RoutedEventArgs e )
        {
            ForwardUiSetStatusWorking( "loading using standard filename... ", 25 );
            SetLabelFileName();
            if ( File.Exists( network.fileName ) )
            {
                network.LoadData( network.fileName );
                // show the loaded data
                canvasTopicWindow_NetLayers.workingTopic = network.workingTopic;
                canvasTopicWindow_NetLayers.ShowTopic();

                _textBoxNetLayers.Text = canvasTopicWindow_NetLayers.workingTopic;
                _initCheck.IsChecked = true;

            }
            ForwardUiStatusCheckDone( "done loading using standard filename." );

        }   // end: _ButtonLoad_Click

        /// <summary>
        /// Event handler: _ButtonLoadOf_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonLoadOf_Click( object sender, RoutedEventArgs e )
        {
            ForwardUiSetStatusWorking( "loading with chosen filename...", 25 );
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "FFN"; // Default file name
            dialog.DefaultExt = ".network"; // Default file extension
            dialog.Filter = "network save file (.network)|*.network"; // Filter files by extension
            dialog.DefaultDirectory = workingDirectory;

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
                canvasTopicWindow_NetLayers.workingTopic = network.workingTopic;
                canvasTopicWindow_NetLayers.ShowTopic();

                _textBoxNetLayers.Text = canvasTopicWindow_NetLayers.workingTopic;
                _initCheck.IsChecked = true;

            }
            else
                MessageBox.Show( "No load was done.",
                    "File error", MessageBoxButton.OK, MessageBoxImage.Error );

            ForwardUiStatusCheckDone( "done loading with chosen filename." );

        }   // end: _ButtonLoadOf_Click

        /// <summary>
        /// Event handler: _ButtonSave_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonSave_Click( object sender, RoutedEventArgs e )
        {
            ForwardUiSetStatusWorking( "saving with standard filename...", 25 );
            SetLabelFileName();
            network.SaveData( network.fileName );
            _ButtonPredict_Click( sender, e );
            ForwardUiStatusCheckDone( "done saving with standard filename." );

        }   // end: _ButtonSave_Click

        /// <summary>
        /// Event handler: _ButtonSaveAs_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonSaveAs_Click( object sender, RoutedEventArgs e )
        {
            ForwardUiSetStatusWorking( "saving network with chosen filename...", 25 );
            // Configure save file dialog box
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FileName = "FFN"; // Default file name
            dialog.DefaultExt = ".network"; // Default file extension
            dialog.Filter = "network save file (.network)|*.network"; // Filter files by extension
            dialog.DefaultDirectory = workingDirectory;

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
            else
                MessageBox.Show( "No save was done.",
                    "File error", MessageBoxButton.OK, MessageBoxImage.Error );

            ForwardUiStatusCheckDone( "done saving network with chosen filename." );

        }   // end: _ButtonSaveAs_Click

        /// <summary>
        /// Event handler: text changed in the 'TextBox'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TextBoxNetLayers_TextChanged( object sender, TextChangedEventArgs e )
        {
            _initCheck.IsChecked = false;
            if ( canvasTopicWindow_NetLayers.ParseTopic( _textBoxNetLayers.Text,
                    ref network.layersTopic ) )
                _topicCheck.IsChecked = true;
            else
                _topicCheck.IsChecked = false;

            if ( _topicCheck.IsChecked == true )
            {
                /*
                canvasTopicWindow_NetLayers.workingTopic = _textBoxNetLayers.Text;
                if ( _datasetCheckParabel.IsChecked == true ) 
                    if ( canvasTopicWindow_NetLayers.ParseDataIntoTopic( inputArrayField, 
                            outputArrayField ) )
                        SetTextBoxNetLayers( canvasTopicWindow_NetLayers.workingTopic );
                if ( _datasetCheckLoad.IsChecked == true )
                    if ( canvasTopicWindow_NetLayers.ParseLocalDataIntoTopic( 
                            network.localIns, network.localOuts ) )
                        SetTextBoxNetLayers( canvasTopicWindow_NetLayers.workingTopic );
                */
                canvasTopicWindow_NetLayers.ShowTopic();

            }

        }   // end: _TextBoxNetLayers_TextChanged

        /// <summary>
        /// Event handler: _ButtonDatasetParabel_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonDatasetParabel_Click( object sender, RoutedEventArgs e )
        {
            ForwardUiSetStatusWorking( "creating test dataset: parable", 25 );
            // order the 'CheckBox's
            _datasetCheckLoad.IsChecked = false;
            _datasetCheckParabel.IsChecked = false;
            _textBoxShowIn.Text = "0";
            _textBoxShowOut.Text = "0";
            if ( _initCheck.IsChecked == false )
                return;

            canvasChartWindow_Values.useLines = true;
            canvasChartWindow_Values.DataClear();
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

            if ( ( network.layersTopic[ 0 ] == 2 ) &&
                ( network.layersTopic[ network.layersTopic.Length - 1 ] == 1 ) )
            {   // dataset created, show it now
                _datasetCheckParabel.IsChecked = true;
                canvasChartWindow_Values.titleText = "Parable [ -10, 10 ]";
                canvasChartWindow_Values.DataAdd( xValues, yValues );
                canvasChartWindow_Values.ShowChart();
                canvasTopicWindow_NetLayers.ShowTopic();

            }
            else
                Message.Show( "Data set is not fitting to network's input/output nodes!");

            ForwardUiStatusCheckDone( "done creating test dataset: parable." );

        }   // end: _ButtonDatasetParabel_Click

        /// <summary>
        /// Event handler: _ButtonDatasetLoad_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonDatasetLoad_Click( object sender, RoutedEventArgs e )
        {
            // order the 'CheckBox's
            _datasetCheckLoad.IsChecked = false;
            _datasetCheckParabel.IsChecked = false;
            _textBoxShowIn.Text = "0";
            _textBoxShowOut.Text = "0";
            // network input/output has to be compatible or what?
            if ( _initCheck.IsChecked == false )
                return;
            canvasChartWindow_Values.useLines = false;
            canvasChartWindow_Values.DataClear( );
            // if it is inititet
            try
            {
                //network.LoadDataFromExcel( "", 0, true );
                bool ok = network.LoadDataFromExcel( "", 0, false, 2, 1 );
                if ( !ok )
                {
                    MessageBox.Show( "Loading was not successful !",
                        "Nothing was loaded!",
                        MessageBoxButton.OK, MessageBoxImage.Warning );
                    return;

                }

            }
            catch ( ArgumentException aEx )
            {
                string boxText = $"{aEx.Message} -> \n{aEx.ParamName}";
                MessageBox.Show( boxText,
                    "Excel file's data does not fit !",
                    MessageBoxButton.OK, MessageBoxImage.Warning );
                return;

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
            canvasChartWindow_Values.titleText =
                $"nodes# input: {showIn} output: {showOut} ";
            canvasChartWindow_Values.DataAdd( xValues, yValues );
            canvasChartWindow_Values.ShowChart( );
            canvasTopicWindow_NetLayers.ShowTopic( );

            _topicCheck.IsChecked = true;

        }   // end: _ButtonDatasetLoad_Click

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
                canvasTopicWindow_NetLayers.ShowWindow();

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
                canvasChartWindow_Values.ShowWindow();

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
                canvasChartWindow_Errors.ShowWindow();

            }

        }   // end: _CanvasErrors_MouseDown

        /// <summary>
        /// Event handler: _ButtonInit_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonInit_Click( object sender, RoutedEventArgs e )
        {
            ForwardUiSetStatusWorking( "creating the new network ...", 25 );
            canvasTopicWindow_NetLayers.workingTopic = _textBoxNetLayers.Text;
            canvasTopicWindow_NetLayers.ParseTopic( canvasTopicWindow_NetLayers.workingTopic,
                    ref canvasTopicWindow_NetLayers.topicField );
            _initCheck.IsChecked = true;
            _datasetCheckParabel.IsChecked = false;
            _datasetCheckLoad.IsChecked = false;
            string fullFileName = GetDirectory() + "FFN.network";
            network = new FFN( canvasTopicWindow_NetLayers.topicField, true, fullFileName );

            ForwardUiStatusCheckDone( "done creating the new network." );

        }   // end: _ButtonInit_Click

        /// <summary>
        /// Event handler: _ButtonPredict_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonPredict_Click( object sender, RoutedEventArgs e )
        {
            int showIn = QueryUiShowIn();
            int showOut = QueryUiShowOut();
            if ( QueryUiInitCheck()
                && QueryUiDatasetCheckParabel() )
            {
                var predictArray = new double[ xValues.Length ];
                for ( int pos = 0; pos < xValues.Length; pos++ )
                {
                    double[] result = network.Predict( inputArrayField[ pos ] );
                    predictArray[ pos ] = Math.Round( result[ showOut ], 2 );

                }

                ForwardUiShowPredict( "Parable [ -10, 10 ] + Predict", predictArray );

            }

            if ( QueryUiInitCheck()
                && QueryUiDatasetCheckLoad() )
            {
                var predictArray = new double[ xValues.Length ];
                for ( int pos = 0; pos < xValues.Length; pos++ )
                {
                    double[] result = network.Predict( network.localInputArrayField[ pos ] );
                    predictArray[ pos ] = Math.Round( result[ showOut ], 2 );

                }

                ForwardUiShowPredict( 
                    $"nodes# input: {showIn} output: {showOut} + Predict",
                    predictArray );

            }
            ForwardUiStatusCheckDone( "Predict done." );

        }   // end: _ButtonPredict_Click

        /// <summary>
        /// Event handler: _DatasetCheckParabel_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DatasetCheckParabel_Click( object sender, RoutedEventArgs e )
        {
            if ( _datasetCheckLoad.IsChecked == true )
            {
                _datasetCheckLoad.IsChecked = false;
                _ButtonDatasetParabel_Click( sender, e );

            }

        }   // end: _DatasetCheckParabel_Click

        /// <summary>
        /// Event handler: _DatasetCheckLoad_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DatasetCheckLoad_Click( object sender, RoutedEventArgs e )
        {
            if ( _datasetCheckParabel.IsChecked == true )
            {
                _datasetCheckParabel.IsChecked = false;
                _ButtonDatasetLoad_Click( sender, e );

            }

        }   // end: _DatasetCheckLoad_Click

        /// <summary>
        /// Event handler: _TextBoxInputEpochs_PreviewTextInput
        /// <para>text input from any source works via the preview-versions</para>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TextBoxInputEpochs_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            Regex regex = new("[0-9]+");
            e.Handled = !regex.IsMatch( e.Text );

        }   // end: _TextBoxInputEpochs_PreviewTextInput

        /// <summary>
        /// Event handler: _ButtonAutomaticTraining_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonAutomaticTraining_Click( object sender, RoutedEventArgs e )
        {
            bool networkOK;
            // training in intervals ( start/resume for the 'automaticLoopThread' )
            switch ( isAutomatic )
            {
                case 0:
                    // new start
                    networkOK = true;
                    // order the 'CheckBox's
                    networkOK &= ( ( _datasetCheckLoad.IsChecked == true )
                            || ( _datasetCheckParabel.IsChecked == true ) );
                    networkOK &= ( _topicCheck.IsChecked == true );
                    networkOK &= ( _initCheck.IsChecked == true );
                    networkOK &= ( network.epochsNumber > 0 );
                    if ( networkOK )
                    {
                        int epochsToFit = QueryUiEpochsToTrain();
                        isAutomatic = 2;
                        autoLoopThread = new Thread ( 
                            () =>
                            {
                                ForwardUiStatusCheckStart( 
                                    $"automatic 'Train' over {epochsToFit} epochs: start..." );
                                while ( isAutomatic == 2 )
                                    AutomaticLoop();
                                ForwardUiStatusCheckDone(
                                    $"automatic 'Train' over {epochsToFit} epochs: done" );

                            } );
                        autoLoopThread.Start();

                    }
                    break;
                case 1:
                    // resume thread
                    networkOK = true;
                    // order the 'CheckBox's
                    networkOK &= ( ( _datasetCheckLoad.IsChecked == true )
                            || ( _datasetCheckParabel.IsChecked == true ) );
                    networkOK &= ( _topicCheck.IsChecked == true );
                    networkOK &= ( _initCheck.IsChecked == true );
                    networkOK &= ( network.epochsNumber > 0 );
                    if ( networkOK )
                    {
                        isAutomatic = 2;
                        autoLoopThread = new Thread (
                            () =>
                            {
                                while ( isAutomatic == 2 )
                                    AutomaticLoop();

                            } );
                        autoLoopThread.Start();

                    }
                    break;
                case 2:
                    // new pause
                    isAutomatic = 1;
                    break;

            }

        }   // end: _ButtonAutomaticTraining_Click

        /// <summary>
        /// Event handler: _TextBoxNoInput_PreviewTextInput
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TextBoxNoInput_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            Regex regex = new("[0-9]+");
            e.Handled = !regex.IsMatch( e.Text );

        }   // end: _TextBoxNoInput_PreviewTextInput

        /// <summary>
        /// Event handler: _ShowLikeCheck_Checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ShowLikeCheck_Checked( object sender, RoutedEventArgs e )
        {

        }   // end: _ShowLikeCheck_Checked

        /// <summary>
        /// Event handler: _ButtonTrain_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonTrain_Click( object sender, RoutedEventArgs e )
        {
            bool networkOK = true;
            // order the 'CheckBox's
            networkOK &= ( ( _datasetCheckLoad.IsChecked == true )
                    || ( _datasetCheckParabel.IsChecked == true ) );
            networkOK &= ( _topicCheck.IsChecked == true );
            networkOK &= ( _initCheck.IsChecked == true );
            // status has to be OK
            int datasetChoice = QueryUiStatusDatasetCheck();
            Thread workIt = new Thread( ( ) =>
            {
                if ( networkOK )
                {
                    ForwardUiStatusCheckStart( $"Train for {epochsToFit} epochs: start..." );
                    string fitText = "";
                    if ( datasetChoice == 1 )
                        fitText = network.Fit( inputArrayField, outputArrayField, epochsToFit );
                    if ( datasetChoice == 2 )
                        fitText = network.Fit_LocalData( epochsToFit );
                    network.SaveData( network.fileName );
                    ForwardUiShowText( fitText );
                    _ButtonPredict_Click( sender, e );
                    ForwardUiStatusCheckDone( $"Train for {epochsToFit} epochs: done" );

                }

            }
                );
            workIt.Start();

        }   // end: _ButtonTrain_Click

        /// <summary>
        /// Event handler: _ButtonAutomaticTrainingPause_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonAutomaticTrainingPause_Click( object sender, RoutedEventArgs e )
        {
            switch ( isAutomatic )
            {
                case 2:
                    isAutomatic = 1;
                    //autoLoopTimer.Stop();
                    break;
                case 1:
                    isAutomatic = 2;
                    //autoLoopTimer.Start();
                    break;

            }

        }   // end: _ButtonAutomaticTrainingPause_Click

        /// <summary>
        /// Event handler: _ButtonAutomaticTrainingStop_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ButtonAutomaticTrainingStop_Click( object sender, RoutedEventArgs e )
        {
            isAutomatic = 0;
            //autoLoopTimer.Stop();
            
        }   // end: _ButtonAutomaticTrainingStop_Click

        /// <summary>
        /// Event handler: _TextBoxShowIn_PreviewTextInput
        /// <para>text input from any source works via the preview-versions</para>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TextBoxShowIn_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            Regex regex = new("[0-9]+");
            e.Handled = !regex.IsMatch( e.Text );

        }   // end: _TextBoxShowIn_PreviewTextInput

        /// <summary>
        /// Event handler: _TextBoxShowOut_PreviewTextInput
        /// <para>text input from any source works via the preview-versions</para>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TextBoxShowOut_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            Regex regex = new("[0-9]+");
            e.Handled = !regex.IsMatch( e.Text );

        }   // end: _TextBoxShowOut_PreviewTextInput

        /// <summary>
        /// Event handler: _TextBoxShowIn_TextChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        private void _TextBoxInputEpochs_TextChanged( object sender, TextChangedEventArgs e )
        {
            epochsToFit = int.Parse( _textBoxInputEpochs.Text );

        }

    }   // end: partial class FFN_Window

}   // end: namespace MatrixFFN

/*
            Action forward = new Action( ( ) =>
            {

            }
                );
            Dispatcher.Invoke( forward );


            Thread workIt = new Thread( ( ) =>
            {

            }
                );
            workIt.Start();

*/