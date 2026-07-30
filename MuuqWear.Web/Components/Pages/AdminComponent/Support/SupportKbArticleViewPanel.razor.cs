using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MuuqWear.Application.Services.HelpCenterService;
using MuuqWear.Model.HelpCenter;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent.Support;

public partial class SupportKbArticleViewPanel
{
    [Parameter, EditorRequired] public HelpArticleModel Article { get; set; } = default!;
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public bool ShowEditButton { get; set; }
    [Parameter] public bool ShowOpenInHelpCenter { get; set; } = true;
    [Parameter] public bool ShowAgentEngagement { get; set; }

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IHelpCenterService HelpCenterService { get; set; } = default!;

    private CategoryMeta CategoryMeta => HelpCategoryMeta.Get(Article.Category);

    private IReadOnlyList<HelpArticleCommentModel> comments = [];
    private string commentInput = string.Empty;
    private int likeCount;
    private int dislikeCount;
    private string? myVote;
    private bool engagementBusy;
    private string? engagementError;

    protected override async Task OnParametersSetAsync()
    {
        RefreshEngagementFromArticle();
        await Task.CompletedTask;
    }

    private void RefreshEngagementFromArticle()
    {
        comments = Article.Comments
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
        likeCount = Article.LikeCount;
        dislikeCount = Article.DislikeCount;
        myVote = Article.MyVote;
    }

    private async Task VoteAsync(string vote)
    {
        if (engagementBusy)
            return;

        engagementBusy = true;
        engagementError = null;
        try
        {
            var result = await HelpCenterService.SetArticleVote(Article.Id, vote);
            if (result.Success && result.Data != null)
            {
                ApplyEngagement(result.Data);
            }
            else
            {
                engagementError = AdminUiErrorHelper.FromApi(
                    result.Message, "Failed to save vote.");
                await ReloadArticleAsync(showErrors: false);
            }
        }
        catch (Exception ex)
        {
            engagementError = AdminUiErrorHelper.FromException(ex);
        }
        finally
        {
            engagementBusy = false;
        }
    }

    private async Task PostCommentAsync()
    {
        if (engagementBusy || string.IsNullOrWhiteSpace(commentInput))
            return;

        engagementBusy = true;
        engagementError = null;
        try
        {
            var body = commentInput.Trim();
            var result = await HelpCenterService.AddArticleComment(Article.Id, body);
            if (result.Success)
            {
                commentInput = string.Empty;
                await ReloadArticleAsync(showErrors: false);
            }
            else
            {
                engagementError = AdminUiErrorHelper.FromApi(
                    result.Message, "Failed to post comment.");
            }
        }
        catch (Exception ex)
        {
            engagementError = AdminUiErrorHelper.FromException(ex);
        }
        finally
        {
            engagementBusy = false;
        }
    }

    private async Task ReloadArticleAsync(bool showErrors = true)
    {
        var result = await HelpCenterService.GetAdminArticleById(Article.Id);
        if (!result.Success || result.Data == null)
        {
            if (showErrors)
            {
                engagementError = AdminUiErrorHelper.FromApi(
                    result.Message, "Failed to refresh article.");
            }

            return;
        }

        CopyArticleFields(result.Data);
        RefreshEngagementFromArticle();
        StateHasChanged();
    }

    private void ApplyEngagement(HelpArticleEngagementModel engagement)
    {
        Article.LikeCount = engagement.LikeCount;
        Article.DislikeCount = engagement.DislikeCount;
        Article.MyVote = engagement.MyVote;
        Article.HelpfulCount = engagement.LikeCount;
        if (engagement.Comments.Count > 0)
            Article.Comments = engagement.Comments;

        RefreshEngagementFromArticle();
    }

    private void CopyArticleFields(HelpArticleModel source)
    {
        Article.Comments = source.Comments;
        Article.LikeCount = source.LikeCount;
        Article.DislikeCount = source.DislikeCount;
        Article.MyVote = source.MyVote;
        Article.HelpfulCount = source.HelpfulCount;
        Article.ViewCount = source.ViewCount;
    }

    private async Task HandleCommentKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
            await PostCommentAsync();
    }

    private void OpenHelpCenter() =>
        NavigationManager.NavigateTo("/help", true);
}
