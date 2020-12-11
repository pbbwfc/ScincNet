using System;
using System.Runtime.InteropServices;

namespace ScincFuncs
{
    public static class Base
    {
        [DllImport("ScincBridge.dll")]
        public static extern int Base_autoload(bool getbase, uint basenum);
        public static int Autoload(bool getbase, uint basenum)
        {
            int result = Base_autoload(getbase, basenum);
            return result;
        }
    }
}
