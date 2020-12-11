using System;
using System.Runtime.InteropServices;

namespace ScincFuncs
{
    public static class Clipbase
    {
        [DllImport("ScincBridge.dll")]
        public static extern int Clipbase_clear();
        public static int Clear()
        {
            int result = Clipbase_clear();
            return result;
        }
    }

}
