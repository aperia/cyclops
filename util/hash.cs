using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Cyclops
{
	public static class Hash
	{
		public static string GetMd5(string data)
		{
			byte[] input = ASCIIEncoding.ASCII.GetBytes(data);
			byte[] output = MD5.Create().ComputeHash(input);
			
			StringBuilder sb = new StringBuilder();
			
			for (int i = 0; i < output.Length; i++)
				sb.Append(output[i].ToString("X2"));
			
			return sb.ToString();
		}
		
		private static SHA256 sha = new SHA256Managed();
		
        public static string GetSha256(string data)
        {
            byte[] hash = sha.ComputeHash(Encoding.Unicode.GetBytes(data));

            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in hash)
            {
                stringBuilder.AppendFormat("{0:x2}", b);
            }
            return stringBuilder.ToString();
        }
	}
}

