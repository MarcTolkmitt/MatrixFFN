// Ignore Spelling: ILGPU

using ILGPU;
using ILGPU.Runtime;
using System;
using System.IO;
using System.Windows;
using System.Threading;
using Array = System.Array;
using System.Linq;

namespace MatrixFFN
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Demoprogramm von MatrixFFN: Parabel als Testdaten, FFN laden/speichern, trainieren, vorhersagen,
    /// FFN-Fenster starten. Übernommen aus dem Projekt ILGPU-Test ( 27.11.26 ).
    /// Erstellt:       04.07.23.
    /// Letzter Edit :  27.11.26.
    /// 27.11.26: in MatrixFFN eingepflegt ( Namensraum MatrixFFN ), Excel über DbaseFrame statt NPOIwrap
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Version des Demoprogramms
        /// </summary>
        public Version version = new Version("1.0.7");

        int nummer = 0;
        /// <summary>
        /// Eingaben der Testdaten ( Parabel ), je Zeile ein Muster
        /// </summary>
        public double[ ][ ] inputArray = new double[1][];
        /// <summary>
        /// Ausgaben der Testdaten ( Parabel ), je Zeile ein Muster
        /// </summary>
        public double[ ][ ] outputArray = new double[1][];
        /// <summary>
        /// das Netz des Demoprogramms
        /// </summary>
        public FFN network = new FFN( new int[] { 1, 2, 1 }, true );
        /// <summary>
        /// das FFN-Fenster ( Menü "start the FFN window" )
        /// </summary>
        public FFN_Window networkWindow = new FFN_Window();

        /// <summary>
        /// Constructor of the class
        /// </summary>
        public MainWindow( )
        {
            InitializeComponent();
            Test1();

        }   // end: MainWindow ( Constructor )

        /// <summary>
        /// Helper function to create the raw parable test data.
        /// </summary>
        public void CreateTestData( )
        {
            inputArray = new double[ 21 ][ ];
            outputArray = new double[ 21 ][ ];
            for ( int pos = 0; pos < 21; pos++ )
            {
                inputArray[ pos ] = new double[ 2 ]
                    { ( pos - 10 ), ( pos - 10 ) };
                outputArray[ ( int ) pos ] = new double[ 1 ]
                    { ( Math.Pow( pos - 10, 2 ) ) };
            }
            
            DisplayText( ArrayToString( inputArray) );
            DisplayText( ArrayToString( outputArray) );


        }   // end: CreateTestData

        /// <summary>
        /// Helper function for writing ragged arrays into a string.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="lineBreak">nach jeder Zeile umbrechen</param>
        /// <returns>the data as 'string'</returns>
        public string ArrayToString( double[ ][ ] data, bool lineBreak = false )
        {
            string text = "";
                
            foreach ( double[ ] dat in data )
            {
                text += $" [ {string.Join( ", ", dat ) } ] ";
                if ( lineBreak )
                    text += "\n";

            }
            text += "\n";
            return( text );

        }   // end: ArrayToString

        /// <summary>
        /// Helper function to write the given text
        /// into the text output - here a part of the
        /// main window.
        /// </summary>
        /// <param name="text">input 'string'</param>
        public void DisplayText( string text )
        { 
            if ( !string.IsNullOrEmpty( text ) )
                _TextBlock.Text += text + "\n"; 
            _TextScroll.ScrollToBottom();

        }   // end: DisplayText

        /// <summary>
        /// Helper function to write the given text
        /// into the text output - here a part of the
        /// main window.
        /// </summary>
        /// <param name="obj">die Zahl</param>
        private void DisplayText( int obj )
        {
            DisplayText( obj.ToString( ) );

        }   // end: DisplayText

        /// <summary>
        /// Takes the info 'string' from ILGPU 
        /// to show system information.
        /// </summary>
        /// <param name="accelerator">ILGPU variable ( GPU or Emulation )</param>
        /// <returns>the info 'string'</returns>
        public string GetInfoString( Accelerator accelerator ) 
        { 
            StringWriter stringWriter = new StringWriter();
            accelerator.PrintInformation( stringWriter );
            return( stringWriter.ToString() );
        }   // end: GetInfoString
        
        /// <summary>
        /// First steps from the
        /// ILGPU-Tutorial.
        /// </summary>
        public void Test1()
        {   // fist Tutorial etc. 
            using Context context = Context.CreateDefault();

            foreach ( Device dev in context )
            {
                Console.WriteLine( dev );
                DisplayText( dev.ToString() );
                using Accelerator accelerator = dev.CreateAccelerator( context );
                Console.WriteLine( accelerator );
                DisplayText( accelerator.ToString() );
                Console.WriteLine( GetInfoString( accelerator ) );
                DisplayText( GetInfoString( accelerator ) );
            }

        }   // end: Test1

        // --------------------------------------------------------------------


        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FFNwindowStart( object sender, RoutedEventArgs e )
        {
            networkWindow.Show();
            networkWindow.Owner = this;

        }   // end: _FFNwindowStart

        /// <summary>
        /// Close the windows to programs end.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            networkWindow.isNowToEnd = true;
            networkWindow.Close();

        }   // end: Window_Closing

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MenuQuit_Click( object sender, RoutedEventArgs e )
        {
            this.Close( );

        }   // end: _MenuQuit_Click

        // -------------------------------------------------      Events

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FFNload_Click( object sender, RoutedEventArgs e )
        {
            DisplayText( "FFNladen: " );
            if ( network == null )
                network = new FFN( new int[] { 1, 2, 1 }, true );
            network.LoadData( network.fileName );


        }   // end: _FFNload_Click

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MenuFFNloadOf_Click( object sender, RoutedEventArgs e )
        {
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
                string filename = dialog.FileName;
                network.fileName = filename;
                network.LoadData( network.fileName );
                DisplayText( $"the chosen file is {filename}" );

            }

        }   // end: _MenuFFNloadOf_Click

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FFNsave_Click( object sender, RoutedEventArgs e )
        {
            DisplayText( " saving FFN " );
            network.SaveData( network.fileName );

        }   // end: _FFNsave_Click

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MenuFFNsaveAs_Click( object sender, RoutedEventArgs e )
        {
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
                string filename = dialog.FileName;
                network.fileName = filename;
                network.SaveData( network.fileName );
                DisplayText( $"the chosen file is {filename}" );

            }

        }   // end: _MenuFFNsaveAs_Click

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FFNinit_Click( object sender, RoutedEventArgs e )
        {
            DisplayText( "initialize the 'FFN'" );
            int[] topic = new int[] { 2, 40, 20, 10, 1 };
            network = new FFN( topic, true );
            DisplayText( network.ToString() );
            //DisplayText( network.schichtenTopic );
            Array.ForEach( network.layersTopic, DisplayText );
            _FFNtrain_1_Click( sender, e );

        }   // end: _FFNinit_Click

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FFNtrain_1_Click( object sender, RoutedEventArgs e )
        {
            DisplayText( "calling 'Fit' for one epoch of the dataset..." );
            string result = network.Fit( inputArray, outputArray, 1 );
            DisplayText( result );
            DisplayText( $"duration for the training: {network.timeFit}.\n" );
            _FFNpredict_Click( sender, e );

        }   // end: _FFNtrain_1_Click

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FFNtrain_10_Click( object sender, RoutedEventArgs e )
        {
            DisplayText( "calling 'Fit' for 10 epochs of the dataset..." );
            string result = network.Fit( inputArray, outputArray, 10 );
            DisplayText( result );
            DisplayText( $"duration for the training: {network.timeFit}.\n" );
            _FFNpredict_Click( sender, e );

        }   // end: _FFNtrain_10_Click

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FFNtrain_100_Click( object sender, RoutedEventArgs e )
        {
            DisplayText( "calling 'Fit' for 100 epochs of the dataset..." );
            string result = network.Fit( inputArray, outputArray, 100 );
            DisplayText( result );
            DisplayText( $"duration for the training: {network.timeFit}.\n" );
            _FFNpredict_Click( sender, e );

        }   // end: _FFNtrain_100_Click

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FFNtrain_1000_Click( object sender, RoutedEventArgs e )
        {
            DisplayText( "calling 'Fit' for 100 epochs of the dataset..." );
            string result = network.Fit( inputArray, outputArray, 1000 );
            DisplayText( result );
            DisplayText( $"duration for the training: {network.timeFit}.\n" );
            _FFNpredict_Click( sender, e );

        }   // end: _FFNtrain_1000_Click

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FFNtrain_10000_Click( object sender, RoutedEventArgs e )
        {
            DisplayText( "calling 'Fit' for 100 epochs of the dataset..." );
            string result = network.Fit( inputArray, outputArray, 10000 );
            DisplayText( result );
            DisplayText( $"duration for the training: {network.timeFit}.\n" );
            _FFNpredict_Click( sender, e );

        }   // end: _FFNtrain_1000_Click

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FFNpredict_Click( object sender, RoutedEventArgs e )
        {
            DisplayText( "collecting the results of prediction..." );
            var predictArray = new double[ 21 ][];
            for ( int pos = 0; pos < 21; pos++ )
            {
                var result = network.Predict( inputArray[ pos ] );
                result[ 0 ] = Math.Round( result[ 0 ], 2 );
                predictArray[ pos ] = result;

            }

            DisplayText( ArrayToString( inputArray ) );
            DisplayText( ArrayToString( outputArray ) );
            DisplayText( ArrayToString( predictArray ) );

        }   // end: _FFNpredict_Click

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FFNlearnrate_Click( object sender, RoutedEventArgs e )
        {
            network.SetLearningRate( 0.1 );

        }   // end: _FFNlearnrate_Click

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FFNlearnLoop_Click( object sender, RoutedEventArgs e )
        {
            for ( int count = 0; count < 1800; count++ )
            {
                DisplayText( "calling Fit' for 10_000 epochs..." );
                string result = network.Fit( inputArray, outputArray, 10_000 );
                DisplayText( result );
                DisplayText( $"duration for 10_000eEpochs: {network.timeFit}.\n" );
                DisplayText( "results of 'Predict'..." );
                var predictArray = new double[ 21 ][];
                for ( int pos = 0; pos < 21; pos++ )
                {
                    double[] outputs = network.Predict( inputArray[ pos ] );
                    outputs[ 0 ] = Math.Round( outputs[ 0 ], 2 );
                    predictArray[ pos ] = outputs;

                }

                DisplayText( ArrayToString( inputArray ) );
                DisplayText( ArrayToString( outputArray ) );
                DisplayText( ArrayToString( predictArray ) );
                Thread.Sleep( 500 );

            }   //

        }   // end: _FFNlearnLoop_Click

        /// <summary>
        /// Handler function -> _MenuDPparableCreate_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MenuDPparableCreate_Click( object sender, RoutedEventArgs e )
        {
            nummer++;
            _TextBlock.Text += nummer.ToString() + ": CreateTestData().\n";
            CreateTestData();

        }   // end: _MenuDPparableCreate_Click

        /// <summary>
        /// Handler function -> MenuItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _MenuDPparableWrite_Click( object sender, RoutedEventArgs e )
        {
            if ( inputArray.Length < 21 )
            {   // noch keine Testdaten ( 27.11.26: vorher IndexOutOfRangeException )
                DisplayText( "create the test data first ( 'Test data parable' )" );
                return;
            }   // Ende: noch keine Testdaten
            double[][] doubles = new double[ 21 ][];
            for ( int i = 0; i < doubles.Length; i++ )
                doubles[ i ] = inputArray[ i ].Concat(outputArray[ i ] ).ToArray();

            DisplayText( $"input.Length: {inputArray.Length}" +
                $" output.Length: {outputArray.Length}" +
                $" doubles.Length: {doubles.Length}");
            DisplayText( $"input[0].Length: {inputArray[ 0 ].Length}" +
                $" output[0].Length: {outputArray[ 0 ].Length}" +
                $" doubles[0].Length: {doubles[ 0 ].Length}" );
            
            /* Schreiben über DbaseFrame ( ExcelInterop, 27.11.26 statt NPOIwrap )
             *  - ohne Quelldatei geöffnet: Excel startet, WriteDoublesToNewTarget legt eine neue Mappe an
             *  - Matrix( x, y ): erster Index Spalte, zweiter Zeile; ohne Kopfzeile wie bisher
             *  - Zieldatei per Dialog, danach Excel sofort beenden
            */
            DbaseFrame.ExcelInterop excel = new DbaseFrame.ExcelInterop( "", true, false, true );
            try
            {
                excel.valuesDoubleArray = FFN.RowsToMatrix( doubles );
                excel.WriteDoublesToNewTarget( "", "Parable" );
                DisplayText( "parable written to excel file" );
            }
            catch ( Exception ex )
            {   // Daten passen nicht oder Excel meldet einen Fehler
                DisplayText( "Excel error: " + ex.Message );
            }   // Ende: Daten passen nicht oder Excel meldet einen Fehler
            finally
            {   // Excel beenden
                excel.ExcelInteropQuit( );
            }   // Ende: Excel beenden

        }   // end: _MenuDPparableWrite_Click

    }   // end: partial class MainWindow

}   // end: namespace MatrixFFN

