using System;
using System.Collections.Generic;

namespace Domain.Identity
{
    public class Role
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!; // USER, ADMIN, MANAGER

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}