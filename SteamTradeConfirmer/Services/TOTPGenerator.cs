using System;
using System.Security.Cryptography;
using System.Text;

namespace SteamTradeConfirmer.Services
{
    public class TOTPGenerator
    {
        private readonly byte[] _secret;
        private static readonly string _chars = "23456789BCDFGHJKMNPQRTVWXY";

        public TOTPGenerator(string base32Secret)
        {
            _secret = Base32Decode(base32Secret);
        }

        public string GenerateCode()
        {
            var timeStep = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
            return GenerateCode(timeStep);
        }

        public string GenerateCode(ulong timeStep)
        {
            var timeBytes = BitConverter.GetBytes(timeStep);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(timeBytes);
            }

            using var hmac = new HMACSHA1(_secret);
            var hash = hmac.ComputeHash(timeBytes);

            var offset = hash[hash.Length - 1] & 0x0F;
            var code = ((hash[offset] & 0x7F) << 24) |
                      ((hash[offset + 1] & 0xFF) << 16) |
                      ((hash[offset + 2] & 0xFF) << 8) |
                      (hash[offset + 3] & 0xFF);

            // Steam использует 5-символьные коды с алфавитом 23456789BCDFGHJKMNPQRTVWXY
            var steamCode = "";
            for (int i = 0; i < 5; i++)
            {
                steamCode += _chars[(int)(code % _chars.Length)];
                code /= _chars.Length;
            }

            return steamCode;
        }

        private static byte[] Base32Decode(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentNullException(nameof(input));
            }

            input = input.TrimEnd('=').ToUpperInvariant();
            var output = new byte[input.Length * 5 / 8];
            var bitIndex = 0;
            var inputIndex = 0;
            var outputBits = 0;
            var outputIndex = 0;

            while (outputIndex < output.Length)
            {
                var byteIndex = GetBase32Value(input[inputIndex]);
                if (byteIndex < 0)
                {
                    throw new ArgumentException("Invalid Base32 character: " + input[inputIndex]);
                }

                var bits = Math.Min(5 - bitIndex, 8 - outputBits);
                output[outputIndex] <<= bits;
                output[outputIndex] |= (byte)(byteIndex >> (5 - (bitIndex + bits)));

                bitIndex += bits;
                if (bitIndex >= 5)
                {
                    inputIndex++;
                    bitIndex = 0;
                }

                outputBits += bits;
                if (outputBits >= 8)
                {
                    outputIndex++;
                    outputBits = 0;
                }
            }

            return output;
        }

        private static int GetBase32Value(char c)
        {
            return c switch
            {
                'A' => 0, 'B' => 1, 'C' => 2, 'D' => 3, 'E' => 4, 'F' => 5, 'G' => 6, 'H' => 7,
                'I' => 8, 'J' => 9, 'K' => 10, 'L' => 11, 'M' => 12, 'N' => 13, 'O' => 14, 'P' => 15,
                'Q' => 16, 'R' => 17, 'S' => 18, 'T' => 19, 'U' => 20, 'V' => 21, 'W' => 22, 'X' => 23,
                'Y' => 24, 'Z' => 25, '2' => 26, '3' => 27, '4' => 28, '5' => 29, '6' => 30, '7' => 31,
                // Добавляем поддержку цифр 0 и 1 (нестандартный Base32)
                '0' => 26, '1' => 27,
                _ => -1
            };
        }
    }
}
