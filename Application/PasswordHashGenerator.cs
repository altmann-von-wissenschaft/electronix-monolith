using Application.Services;
using System;

// This is a simple program to generate bcrypt hashes for test users

namespace PasswordHashGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var password1 = "12345678";
            var hash1 = AuthService.HashPassword(password1);
            Console.WriteLine($"Password: {password1}");
            Console.WriteLine($"Hash: {hash1}");
            Console.WriteLine();

            var password2 = "TestPassword123";
            var hash2 = AuthService.HashPassword(password2);
            Console.WriteLine($"Password: {password2}");
            Console.WriteLine($"Hash: {hash2}");
        }
    }
}
