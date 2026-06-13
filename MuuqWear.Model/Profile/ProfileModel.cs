namespace MuuqWear.Model.Profile;

public class ProfileModel
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AffiliateTier { get; set; }
}

public class UpdateProfileModel
{
    public string FullName { get; set; } = string.Empty;
}