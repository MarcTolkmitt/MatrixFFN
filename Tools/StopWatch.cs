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
using System.Diagnostics;
using System.IO;

namespace MatrixFFN.Tools
{
    /// <summary>
    /// Wrapper for the 'Stopwatch' - nicely formated.
    /// </summary>
    public class StopWatch
    {
        /// <summary>
        /// created on: 07.07.2023
        /// last edit: 02.10.24
        /// </summary>
        public Version version = new Version("1.0.8");
        /// <summary>
        /// internal 'Stopwatch'
        /// </summary>
        public Stopwatch localWatch;
        /// <summary>
        /// 'Stopwatch's 'timespan'
        /// </summary>
        public TimeSpan localTimespan;
        /// <summary>
        /// flag for the class
        /// </summary>
        bool isClockOn;

        /// <summary>
        /// constructor without parameters
        /// </summary>
        public StopWatch()
        {
            localWatch = new Stopwatch();
            isClockOn = false;
            localTimespan = new TimeSpan();

        }   // end: StopWatch ( constructor )

        /// <summary>
        /// Diesem constructor wird eine TimeSpan übergeben. Nützlich für das
        /// Speichern des Netzes.
        /// </summary>
        /// <param name="inputTimeSpan">die abgesicherte 'localTimespan'</param>
        public StopWatch(TimeSpan inputTimeSpan)
        {
            localWatch = new Stopwatch();
            isClockOn = false;
            localTimespan = inputTimeSpan;

        }   // end: StopWatch ( constructor )

        /// <summary>
        /// 'Stopwatch's Stop()
        /// </summary>
        public void Stop()
        {
            localWatch.Stop();
            isClockOn = false;
            localTimespan = localWatch.Elapsed;

        }   // end: Stop

        /// <summary>
        /// 'Stopwatch's Start()
        /// </summary>
        public void Start()
        {
            localWatch.Restart();
            localWatch.Start();
            isClockOn = true;

        }   // end: Start

        /// <summary>
        /// Last complete measured time - will change if 
        /// 'Stopwatch' is still running.
        /// </summary>
        /// <returns>time string</returns>
        override
        public string ToString()
        {
            localTimespan = localWatch.Elapsed;
            string text = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            localTimespan.Hours, localTimespan.Minutes, localTimespan.Seconds,
            localTimespan.Milliseconds / 10);
            if ( isClockOn )
                text += "\twatch is still running";
            else
                text += "\twatch is stopped";

            return (text);

        }   // end: ToString

        /// <summary>
        /// delivers the 'Timespan' - more suited for calculations.
        /// </summary>
        /// <returns>the 'TimeSpan'</returns>
        public TimeSpan GetTimeSpan()
        {
            return localTimespan;

        }   // end: GetTimeSpan

    }   // end: class StopUhr

}   // end: namespace MatrixFFN.Tools
