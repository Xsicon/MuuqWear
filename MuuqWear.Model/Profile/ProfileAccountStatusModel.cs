namespace MuuqWear.Model.Profile;

public class ProfileAccountStatusModel
{
    public bool IsActive { get; set; } = true;
    public string AccountStatus { get; set; } = "active";
    public DateTime? SuspendedUntil { get; set; }
}
