namespace MuuqWear.Model.HelpCenter;

public class SupportTicketModel
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Team { get; set; }
    public Guid? AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<SupportTicketReplyModel> Replies { get; set; } = [];
    public int ReplyCount { get; set; }

    public int DisplayReplyCount =>
        Replies.Count > 0 ? Replies.Count : ReplyCount;

    public bool IsAssignedTo(string agentName) =>
        !string.IsNullOrWhiteSpace(AssignedToName) &&
        AssignedToName.Equals(agentName, StringComparison.OrdinalIgnoreCase);

    public int AgentReplyCount =>
        Replies.Count(r => r.IsAgent);
}

public class SupportTicketReplyModel
{
    public Guid Id { get; set; }
    public string SenderType { get; set; } = "agent";
    public string? SenderName { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }

    public bool IsAgent =>
        string.Equals(SenderType, "agent", StringComparison.OrdinalIgnoreCase);
}

public class UpdateTicketModel
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Team { get; set; }
    public Guid? AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
}

public class AddTicketReplyModel
{
    public string Message { get; set; } = string.Empty;
}

public class SubmitTicketModel
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class UpdateTicketStatusModel
{
    public string Status { get; set; } = string.Empty;
}

public class TicketStatsModel
{
    public int OpenCount { get; set; }
    public int InProgressCount { get; set; }
    public int TotalCount { get; set; }
}
