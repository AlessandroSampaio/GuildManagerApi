using System;
using System.Security.Cryptography;

public class Program
{
    public static void Main()
    {
        // Generate 32 random bytes and convert them to a Base64 string
        string secureRandomString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        Console.WriteLine(secureRandomString);
    }
}
