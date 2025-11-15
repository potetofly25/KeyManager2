using System;
using System.Linq;
using System.Text;

namespace potetofly25.KeyManager2.Services
{
    public class PasswordGeneratorService
    {
        private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Lower = "abcdefghijklmnopqrstuvwxyz";
        private const string Digits = "0123456789";
        private const string Symbols = "!@#$%^&*()-_=+[]{};:,.<>/?";

        private readonly Random _rnd = new();

        public string Generate(int length, bool useUpper = true, bool useLower = true, bool useDigits = true, bool useSymbols = true)
        {
            var pool = new StringBuilder();
            if (useUpper) pool.Append(Upper);
            if (useLower) pool.Append(Lower);
            if (useDigits) pool.Append(Digits);
            if (useSymbols) pool.Append(Symbols);
            var p = pool.ToString();
            if (string.IsNullOrEmpty(p)) p = Lower;
            return new string(Enumerable.Range(0, length).Select(_ => p[_rnd.Next(p.Length)]).ToArray());
        }
    }
}
