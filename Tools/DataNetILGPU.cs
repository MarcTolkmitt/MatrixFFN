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
using System.IO;
using System.Linq;

namespace MatrixFFN.Tools
{
    /// <summary>
    /// Die Klasse 'DatenNetz' normalisiert die realen Daten und zurück.
    /// <para>
    /// Das geht für reale ( Pattern ) Features.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Bei Java hatte ich einen MathContext benutzt - hier gibt es 'decimal'. Dieser
    /// Datentyp soll präzise aber langsam sein. Meine Theorie war bestimmt gut und sollte 
    /// auch zukunftsweisend sein. Wichtig ist die Abbildung auf das Intervall und zurück.
    /// <para>
    /// Deswegen werde ich alles mit 'decimal' realisieren - dachte ich.
    /// Dieser Wertetyp wird aber nicht von 'Math' unterstützt und ist eine
    /// Baustelle. So muß es mit 'double' gehen.
    /// </para>
    /// </remarks>
    public class DataNetILGPU
    {
        // Erstellt ab: 06.07.2023
        // letzte Änderung: 02.10.24
        Version version = new Version("1.0.11");
        /// <summary>
        /// Datenobjekt der Klasse - alle Patterns werden hier gefunden.
        /// </summary>
        public List<Pattern> data;
        /// <summary>
        /// reale weltliche Werte
        /// </summary>
        public List<double> valuesReal;
        /// <summary>
        /// normierte Werte für das Netzwerk
        /// </summary>
        public List<double> valuesNormed;
        /// <summary>
        /// hat alle Lernwerte der Patterns
        /// </summary>
        public List<double> valuesAlpha;
        /// <summary>
        /// Positionsname: Eingabe- oder Ausgabeseite
        /// </summary>
        public string name = "Eingabeseite";

        /// <summary>
        /// Standardkonstruktor - wichtig für die Serialisierung.
        /// </summary>
        public DataNetILGPU( string inName = "Eingabeseite" )
        {
            data = new List<Pattern>();
            valuesReal = new List<double>();
            valuesNormed = new List<double>();
            valuesAlpha = new List<double>();

            if ( !inName.Equals( "Eingabeseite" ) )
                name = "Ausgabeseite";

        }   // Ende: DataNetILGPU ( Konstruktor )

        /// <summary>
        /// Der Konstruktor, der sich aus der Speicherdatei lädt.
        /// </summary>
        /// <param name="reader">die übergebene offene Speicherdatei</param>
        public DataNetILGPU( BinaryReader reader )
        {
            data = new List<Pattern>();
            valuesReal = new List<double>();
            valuesNormed = new List<double>();
            valuesAlpha = new List<double>();

            LoadDataFromReader( reader );

        }   // Ende: DataNetILGPU ( Konstruktor ) 

        /// <summary>
        /// Hier werden automatisch aus den übergebenen Daten die
        /// Parameter fürs DatenNetz gebildet.
        /// <para>
        /// Bisher werden keine kategorischen Daten verarbeitet. ( mögl. ToDo-Liste )
        /// </para>
        /// </summary>
        /// <param name="datenArray">die Daten fürs Netz als Feld</param>
        public MatrixILGPU DataNetInit( double[][] datenArray )
        {

            Clear();

            int batchSize = datenArray.Length;
            int featureSize = datenArray[0].Length;
            MatrixILGPU lernRateT = new MatrixILGPU(1, featureSize, 0);

            // für jedes Feature wird ermittelt...
            for ( int feature = 0; feature < featureSize; feature++ )
            {
                // die Limits finden

                double tempMin = ( from feld in datenArray select feld[ feature ] ).Min();
                double tempMax = ( from feld in datenArray select feld[ feature ] ).Max();
                double tempAbstand = (from feld1 in datenArray
                                      from feld2 in datenArray
                                      where Math.Abs(feld1[feature] - feld2[feature] ) > 0.0
                                      select Math.Abs(feld1[feature] - feld2[feature]) ).Min();

                double tempSchritte = (tempMax - tempMin) / tempAbstand;

                // die Ergebnisse bewahren

                Pattern muster = new Pattern( datenArray[0][ feature ],
                    tempMin, tempMax, tempSchritte);
                Add( muster );
                lernRateT.data[ 0, feature ] = muster.learnValue;

            }

            return ( lernRateT );

        }   // Ende: DataNetInit

        /// <summary>
        /// Ich will das DatenNetz in der Ausgabe ( ToString() ) besser darstellen. Deswegen name = { Eingabeseite, Ausgabeseite }
        /// <para>
        /// Eingaben ungleich 'Eingabeseite' ergeben automatisch 'Ausgabeseite'
        /// </para>
        /// </summary>
        /// <param name="inName"></param>
        public void SetName( string inName = "Eingabeseite" )
        {
            if ( !inName.Equals( "Eingabeseite" ) )
                name = "Ausgabeseite";

        }   // Ende: SetName

        /// <summary>
        /// Setzt die Datenliste zurück.
        /// </summary>
        public void Clear( )
        {
            data.Clear();
            valuesAlpha.Clear();
            valuesNormed.Clear();
            valuesReal.Clear();

        }   // Ende: Clear

        /// <summary>
        /// Soll ein 'Pattern' der Liste hinzufügen.
        /// </summary>
        /// <param name="inPattern">zu addierendes Pattern</param>
        public void Add( Pattern inPattern )
        {
            data.Add( inPattern );

        }   // Ende: Add

        /// <summary>
        /// Standardrepräsentation des Objekts.
        /// </summary>
        /// <returns>Beschreibung des DatenNetz'es</returns>
        override
        public string ToString( )
        {
            string text = $"Position des DatenNetzes: {name}\n";
            if ( data.Count > 0 )
            {
                foreach ( var pattern in data )
                {
                    text += pattern.ToString() + "\n";

                }

            }
            else
                text += "keine Daten bisher.\n";
            return ( text );

        }   // Ende: ToString

        /// <summary>
        /// Liest die Größe der Datenliste aus und gibt sie zurück
        /// </summary>
        /// <returns>Größe der Datenliste</returns>
        public int Size( )
        {
            return data.Count;

        }   // Ende: Size

        /// <summary>
        /// Liefert den Alphavektor der die lokale Varianz des jeweiligen
        /// Patternobjektes ausliest.Dies ist für die angepasste Lernrate
        /// wichtig. Sollte für Ein- und Ausgabeschicht im Backpropverfahren
        /// benutzt werden.
        /// </summary>
        /// <returns>Ein Array der speziellen Lernwerte</returns>
        public List<double> GetValuesAlpha( )
        {
            if ( data.Count < 1 )
                throw new ArgumentException(
                    "DatenNetz.DatenAlpha: Datenliste ist leer, Abbruch!",
                    "( data.Count < 1 )" );

            valuesAlpha.Clear();

            for ( int pos = 0; pos < data.Count; pos++ )
            {
                Pattern pattern = data[ pos ];
                valuesAlpha.Add( pattern.learnValue );
            }

            return ( valuesAlpha );

        }   // Ende: GetValuesAlpha

        /// <summary>
        /// eine traditionelle Speicherroutine ( binär )
        /// </summary>
        /// <param name="writer">ein BinaryWriter</param>
        public void SaveDataToWriter( BinaryWriter writer )
        {
            writer.Write( name );
            writer.Write( data.Count );
            for ( int pos = 0; pos < data.Count; pos++ )
                data[ pos ].SaveDataToWriter( writer );

        }   // Ende: SaveDataToWriter

        /// <summary>
        /// eine traditionelle Laderoutine ( binär )
        /// </summary>
        /// <param name="reader">ein BinaryReader</param>
        public void LoadDataFromReader( BinaryReader reader )
        {
            name = reader.ReadString();
            data.Clear();
            int no = reader.ReadInt32();
            for ( int pos = 0; pos < no; pos++ )
                data.Add( new Pattern( reader ) );

        }   // Ende: LoadDataFromReader

    }   // Ende: class DataNetILGPU

}   // Ende: namespace MatrixFFN.Tools

