namespace Application;

public class AuthRequest(string email, string password)
{
    public string Email { get; set; } = email;
    public string Password { get; set; } = password;
}