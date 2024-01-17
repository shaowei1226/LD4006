using System;
using System.Text;

namespace Cognex.DataMan.SDK.Utils
{
    /// <summary>
    /// Performs escaping/unescaping of string arguments for DMCC commands.
    /// </summary>
    public static class DmccEscaper
    {
        private static char[] _escapeChars = new char[] { '\"', '\\', '|', '\t', '\r', '\n' };
        private static byte[] _escapeMap = new byte[256];
        private static byte[] _unescapeMap = new byte[256];

        static DmccEscaper()
        {
            _escapeMap[(byte)'\"'] = (byte)'\"';
            _escapeMap[(byte)'\\'] = (byte)'\\';
            _escapeMap[(byte)'|'] = (byte)'|';
            _escapeMap[(byte)'\t'] = (byte)'t';
            _escapeMap[(byte)'\r'] = (byte)'r';
            _escapeMap[(byte)'\n'] = (byte)'n';

            _unescapeMap[(byte)'\"'] = (byte)'\"';
            _unescapeMap[(byte)'\\'] = (byte)'\\';
            _unescapeMap[(byte)'|'] = (byte)'|';
            _unescapeMap[(byte)'t'] = (byte)'\t';
            _unescapeMap[(byte)'r'] = (byte)'\r';
            _unescapeMap[(byte)'n'] = (byte)'\n';
        }

        /// <summary>
        /// Escapes a bare string to a string that is compatible with the DMCC protocol.
        /// </summary>
        /// <param name="text">The string to be escaped.</param>
        /// <param name="surroundWithQuotes">Quote flag to control if the escaped string to be returned within quotes</param>
        /// <returns>The escaped string, optionally in quotation marks.</returns>
        public static string Escape(string text, bool surroundWithQuotes)
        {
            //quick return if possible
            int escape_char_pos = text.IndexOfAny(_escapeChars);

            if (escape_char_pos < 0)
            {
                //no need to escape, string does not contain any char to be escaped
                if (surroundWithQuotes)
                    return String.Format("\"{0}\"", text);
                else
                    return text;
            }

            //tedious replacing process
            StringBuilder sb = new StringBuilder(text.Length + 10);
            
            if (surroundWithQuotes)
                sb.Append('\"');

            foreach (char c in text)
            {
                if (c > 255)
                {
                    sb.Append(c);
                    continue;
                }

                byte cb = _escapeMap[(byte)c];
                
                if (cb != 0)
                {
                    sb.Append('\\');
                    sb.Append((char)cb);
                }
                else
                {
                    sb.Append(c);
                }
            }

            if (surroundWithQuotes)
                sb.Append('\"');

            return sb.ToString();
        }

        /// <summary>
        /// Unescapes a DMCC-escaped string.
        /// </summary>
        /// <param name="text">The string to be unescaped. Note: no quotes are allowed around the string.</param>
        /// <returns>The unescaped string.</returns>
        public static string Unescape(string text)
        {
            //quick return if possible
            int escape_char_pos = text.IndexOf('\\');

            if (escape_char_pos < 0)
                return text; //no need to escape, does not contain backslash

            //tedious replacing process
            StringBuilder sb = new StringBuilder(text.Length);

            for (int i = 0; i < text.Length; ++i)
            {
                char c = text[i];
                
                if (c == '\\' && i + 1 < text.Length)
                {
                    char c2 = text[i + 1];
                    
                    if (c2 < 255)
                    {
                        byte cb = _unescapeMap[(byte)c2];
                        
                        if (cb != 0)
                        {
                            sb.Append((char)cb);
                            ++i;
                            continue;
                        }
                    }
                }

                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
