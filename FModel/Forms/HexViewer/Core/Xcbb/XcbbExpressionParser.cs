using System;
using System.Collections.Generic;
using System.Text;
using WpfHexaEditor.Core.Bytes;

namespace WpfHexaEditor.Core.Xcbb
{
    /// <summary>
    /// Used to parse expression used in Xccb file for validate byte data. 
    /// </summary>
    /// <remarks>
    /// Expression parser need to be linked to a ByteProvided for validating data.
    /// </remarks>
    public class XcbbExpressionParser
    {
        /// <summary>
        /// This ByteProvider is used for get data from file/stream and validate them in expression
        /// </summary>
        private readonly ByteProvider _provider;

        /// <summary>
        /// Unique constructor
        /// </summary>
        XcbbExpressionParser(ByteProvider provider) => _provider = provider;
        
        /// <summary>
        /// Use for valid expresion "valid if data are equal to..."
        /// </summary>
        /// <param name="expression">expression like: [0x00-0x01]=$'4D 5A'</param>
        /// <returns>
        /// True = expression is valid
        /// False = expression not valid
        /// Null = unable to valid expression</returns>
        public bool? ValidIf(string expression)
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {

                return false;
            }

            return null;
        }

    }
}
