using FlightBooking.Domain.Common;

namespace FlightBooking.Domain.Identity;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // Static role names for consistency
    public static class Names
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string Customer = "Customer";
    }

    // Helper methods
    public static Role CreateAdminRole()
    {
        return new Role
        {
            Name = Names.Admin,
            Description = "System administrator with full access",
            IsActive = true
        };
    }

    public static Role CreateStaffRole()
    {
        return new Role
        {
            Name = Names.Staff,
            Description = "Staff member with limited administrative access",
            IsActive = true
        };
    }

    public static Role CreateCustomerRole()
    {
        return new Role
        {
            Name = Names.Customer,
            Description = "Regular customer with booking access",
            IsActive = true
        };
    }
}
