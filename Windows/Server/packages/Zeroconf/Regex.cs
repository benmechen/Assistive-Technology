using System;
using System.Text.RegularExpressions;

namespace Zeroconf
{
    public class Regexes
    {
        public static Regex HAS_A_TO_Z = new Regex("[A-Za-z]");
        public static Regex HAS_ONLY_A_TO_Z_NUM_HYPHEN = new Regex("^[A-Za-z0-9-]+$");
        public static Regex HAS_ASCII_CONTROL_CHARS = new Regex("[\x00-\x1f\x7f]");
    }
}
