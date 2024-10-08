using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MatrixFFN.Tools
{
    /// <summary>
    /// Shortcut for 'MessageBox'.
    /// No instance no fuss.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Shows a 'MessageBox' for convenience.
        /// </summary>
        /// <param name="text"></param>
        public static void Show( string text )
        {
            MessageBox.Show( text,
            "Message", MessageBoxButton.OK, MessageBoxImage.Error );

        }   // end: Show

    }   // end: public class Message

}   // end: namespace MatrixFFN.Tools

