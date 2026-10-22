namespace MuuqWear.Model.Staff;

public class StaffUserModel
{
    public string Uid { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool Disabled { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastSignInAt { get; set; }
}

public class CreateStaffUserModel
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UpdateStaffUserModel
{
    public string? DisplayName { get; set; }
    public string? Role { get; set; }
    public bool? Disabled { get; set; }
}
