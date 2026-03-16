using BCrypt.Net;

var password1 = "12345678";
var password2 = "TestPassword123";

var hash1 = BCrypt.HashPassword(password1, workFactor: 11);
var hash2 = BCrypt.HashPassword(password2, workFactor: 11);

Console.WriteLine("Email: altmannvonw@icloud.com");
Console.WriteLine($"Password: {password1}");
Console.WriteLine($"Hash: {hash1}");
Console.WriteLine($"Verify: {BCrypt.Verify(password1, hash1)}");
Console.WriteLine();

Console.WriteLine("Email: testuser@example.com");
Console.WriteLine($"Password: {password2}");
Console.WriteLine($"Hash: {hash2}");
Console.WriteLine($"Verify: {BCrypt.Verify(password2, hash2)}");
