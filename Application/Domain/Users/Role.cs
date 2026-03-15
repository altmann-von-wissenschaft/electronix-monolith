namespace Domain.Users;

public class Role
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;  // GUEST, CLIENT, MANAGER, MODERATOR, ADMINISTRATOR
    public string Name { get; set; } = null!;
    public int Hierarchy { get; set; }  // For role inheritance: 0=Guest, 1=Client, 2=Manager, 3=Moderator, 4=Admin

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
