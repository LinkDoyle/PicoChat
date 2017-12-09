using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    class Utility
    {
        private static readonly string CharTable = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private static readonly Random Random = new Random();
        public static string GenerateID()
        {
            var result = new char[16];
            for (int i = 0; i < result.Length; ++i)
                result[i] = CharTable[Random.Next(0, CharTable.Length - 1)];
            return new string(result);
        }
    }
}
