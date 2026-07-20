using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MuuqWear.Application.Content;
using MuuqWear.Application.Services.ContentService;
using MuuqWear.Application.Services.ProductService;
using MuuqWear.Application.Services.VoteService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.ContentItem;
using MuuqWear.Model.Muuqsimo;
using MuuqWear.Web.Services;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminContentComponent : IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IContentService ContentService { get; set; } = default!;
    [Inject] private IProductService ProductService { get; set; } = default!;
    [Inject] private IVoteService VoteService { get; set; } = default!;
    [Inject] private AdminContentTabCoordinator ContentTabCoordinator { get; set; } = default!;
    [Inject] private AdminContentCountsCacheService ContentCountsCache { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "view")]
    public string? ViewQuery { get; set; }

    private string activeView = "journal";
    private bool IsContentCategoryView => AdminContentTabCoordinator.IsContentCategoryView(activeView);
    private ContentCategory ActiveCategory => AdminContentTabCoordinator.ViewToCategory(activeView);

    private List<ContentItemModel> items = new();
    private bool isLoading;
    private bool isFormOpen;
    private string formError = string.Empty;
    private string pageError = string.Empty;
    private string? metricsWarning;
    private bool isSaving;
    private CreateContentItemModel form = new();
    private string formProductId = string.Empty;
    private bool isEditMode;
    private ContentItemModel? editingItem;
    private bool isUploading;
    private string imageTab = "url";
    private string secondImageTab = "url";
    private bool isSecondImageUploading;

    private string searchQuery = string.Empty;
    private string statusFilter = "all";

    private bool IsCurrentViewLoading =>
        isLoading || voteItemsLoading || mediaLoading || loadedView != activeView;

    private bool isDeleteModalOpen;
    private ContentItemModel? deletingItem;
    private bool isDeleting;

    private MuuqsimoPageContentModel eventContent = new();

    private int healthJournalLowSeoCount;
    private int healthJournalDraftCount;
    private int healthVoteActiveCount;

    private static readonly (string View, string Label)[] TabViews =
    {
        ("journal", "Journal"),
        ("design-history", "Design History"),
        ("events", "Events"),
        ("vote", "Vote & Pre-Order"),
        ("media", "Media Library")
    };

    protected override void OnInitialized()
    {
        ContentTabCoordinator.ViewChanged += OnContentViewChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        var previousView = activeView;
        ApplyViewFromQuery();

        if (!string.IsNullOrEmpty(loadedView) && previousView != activeView)
            BeginViewTransition();

        await SyncActiveViewAsync();
    }

    public void Dispose()
    {
        disposed = true;
        ContentTabCoordinator.ViewChanged -= OnContentViewChanged;
    }

    private void ExitViewSync(bool lockAcquired)
    {
        if (!lockAcquired)
            return;

        try
        {
            viewSyncLock.Release();
        }
        catch (ObjectDisposedException)
        {
            // Component torn down while sync was in flight.
        }
    }

    private async Task RefreshBackgroundMetricsAsync()
    {
        if (disposed)
            return;

        try
        {
            await ApplyCachedCountsAndHealthAsync(forceRefresh: false);
        }
        catch (Exception ex)
        {
            metricsWarning = AdminUiErrorHelper.FromException(ex);
        }
    }

    private async Task ApplyCachedCountsAndHealthAsync(bool forceRefresh = false)
    {
        var snapshot = await ContentCountsCache.GetSnapshotAsync(forceRefresh);

        if (disposed)
            return;

        foreach (var (view, count) in snapshot.TabCounts)
            tabCounts[view] = count;

        healthJournalLowSeoCount = snapshot.HealthJournalLowSeoCount;
        healthJournalDraftCount = snapshot.HealthJournalDraftCount;
        healthVoteActiveCount = snapshot.HealthVoteActiveCount;
        metricsWarning = ContentCountsCache.LastError;

        if (activeView == "media")
            tabCounts["media"] = mediaItems.Count;

        await InvokeAsync(StateHasChanged);
    }

    private void ResetViewFilters()
    {
        searchQuery = string.Empty;
        statusFilter = "all";
        journalCategoryFilter = "All";
        journalStatusFilter = "All";
        voteStatusFilter = "All";
        mediaError = string.Empty;
    }

    private void BeginViewTransition()
    {
        isLoading = true;
        loadedView = string.Empty;
        pageError = string.Empty;
        items.Clear();
        voteCampaigns.Clear();
        voteStats = null;
    }

    private async Task SyncActiveViewAsync()
    {
        if (disposed || loadedView == activeView)
            return;

        var lockAcquired = false;
        try
        {
            await viewSyncLock.WaitAsync();
            lockAcquired = true;
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        try
        {
            if (disposed || loadedView == activeView)
                return;

            await LoadItems();
            if (disposed)
                return;

            loadedView = activeView;
            SyncActiveTabCount();
            _ = RefreshBackgroundMetricsAsync();
        }
        finally
        {
            ExitViewSync(lockAcquired);

            if (!disposed)
                await InvokeAsync(StateHasChanged);
        }
    }

    private IEnumerable<ContentItemModel> FilteredItems =>
        items.Where(item =>
        {
            if (statusFilter == "published"
                && !ContentItemStatusHelper.IsPublishedStatus(item.Status))
                return false;

            if (statusFilter == "draft"
                && !ContentItemStatusHelper.IsDraftStatus(item.Status))
                return false;

            if (string.IsNullOrWhiteSpace(searchQuery))
                return true;

            return item.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
        });

    private async Task LoadItems()
    {
        if (disposed)
            return;

        isLoading = true;
        pageError = string.Empty;

        try
        {
            switch (activeView)
            {
                case "vote":
                    await LoadVoteItemsAsync();
                    if (!disposed)
                        items = new();
                    break;
                case "media":
                    await LoadMediaLibraryAsync();
                    if (!disposed)
                        items = new();
                    break;
                default:
                    var result = await ContentService.GetAll(ActiveCategory);
                    if (disposed)
                        return;

                    items = result.Success && result.Data != null
                        ? result.Data
                        : new();

                    if (!result.Success)
                        pageError = result.Message ?? "Failed to load content.";

                    if (activeView == "events")
                        await LoadEventTicketSalesAsync();
                    break;
            }
        }
        finally
        {
            if (!disposed)
                isLoading = false;
        }
    }

    private void OnContentViewChanged(string view)
    {
        if (disposed)
            return;

        var normalized = AdminContentTabCoordinator.NormalizeView(view);
        if (activeView == normalized && loadedView == normalized)
            return;

        activeView = normalized;
        ResetViewFilters();
        CloseForm();
        BeginViewTransition();

        _ = InvokeAsync(async () =>
        {
            if (!disposed)
                await SyncActiveViewAsync();
        });
    }

    private void ApplyViewFromQuery()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        var view = query.TryGetValue("view", out var value) && !string.IsNullOrEmpty(value)
            ? value.ToString()
            : ViewQuery;

        activeView = AdminContentTabCoordinator.NormalizeView(view);
    }

    private void SwitchTab(string view)
    {
        var normalized = AdminContentTabCoordinator.NormalizeView(view);
        if (activeView == normalized)
            return;

        NavigationManager.NavigateTo($"/admin/content?view={normalized}");
    }

    private void SetStatusFilter(string filter)
    {
        statusFilter = filter;
    }

    private async Task<bool> HandlePublish(Guid id)
    {
        pageError = string.Empty;
        var result = await ContentService.Publish(ActiveCategory, id);
        if (result.Success && result.Data != null)
        {
            ReplaceItemInList(items, result.Data);
            SyncActiveTabCount();
            InvalidateContentCounts();
            _ = RefreshBackgroundMetricsAsync();
            StateHasChanged();
            return true;
        }

        pageError = result.Message ?? "Failed to publish item.";
        StateHasChanged();
        return false;
    }

    private async Task<bool> HandleUnpublish(Guid id)
    {
        pageError = string.Empty;
        var result = await ContentService.Unpublish(ActiveCategory, id);
        if (result.Success && result.Data != null)
        {
            ReplaceItemInList(items, result.Data);
            SyncActiveTabCount();
            InvalidateContentCounts();
            _ = RefreshBackgroundMetricsAsync();
            StateHasChanged();
            return true;
        }

        pageError = result.Message ?? "Failed to unpublish item.";
        StateHasChanged();
        return false;
    }

    private string GetTabLabel(string view) =>
        TabViews.First(t => t.View == AdminContentTabCoordinator.NormalizeView(view)).Label;

    private void InvalidateContentCounts() => ContentCountsCache.Invalidate();

    private string GetPageSubtitle() => activeView switch
    {
        "events" => "Manage Muuqsimo events and announcements",
        "design-history" => "Curate design archive entries for the storefront",
        "vote" => "Manage community vote and pre-order campaigns",
        "media" => "Upload and organize images used across the site",
        _ => "Publish and manage all customer-facing content across the site"
    };

    private string GetPublicViewUrl(ContentItemModel item) => activeView switch
    {
        "events" => EventPublicUrl(item),
        "design-history" => $"/design-history#{item.Id}",
        "journal" => GetJournalPublicUrl(item),
        _ => "/journal"
    };

    private static string EventPublicUrl(ContentItemModel item)
    {
        if (string.IsNullOrWhiteSpace(item.Content))
            return "/muuqsimo";

        try
        {
            var content = EventPageContentSerializer.Parse(item.Content);
            if (!string.IsNullOrWhiteSpace(content.Slug))
                return $"/muuqsimo?slug={Uri.EscapeDataString(content.Slug)}";
        }
        catch
        {
            // fall through
        }

        return "/muuqsimo";
    }

    private static string FormatItemDate(ContentItemModel item)
    {
        var date = item.PublishedAt ?? item.CreatedAt;
        return date?.ToString("MMM dd, yyyy") ?? "—";
    }

    private void OpenForm()
    {
        if (activeView == "vote")
        {
            OpenVoteCampaignPanel();
            return;
        }

        form = new CreateContentItemModel();
        formProductId = string.Empty;
        formError = string.Empty;
        isEditMode = false;
        editingItem = null;
        imageTab = "url";
        secondImageTab = "url";
        eventContent = EventPageContentSerializer.CreateDefault();
        InitJournalFormFields();
        isFormOpen = true;
    }

    private void CloseForm()
    {
        isFormOpen = false;
        isEditMode = false;
        editingItem = null;
        formProductId = string.Empty;
        formError = string.Empty;
        eventContent = new MuuqsimoPageContentModel();
    }

    private bool TryPrepareEventForm()
    {
        if (ActiveCategory != ContentCategory.Events)
            return true;

        if (string.IsNullOrWhiteSpace(eventContent.Slug))
        {
            formError = "Event slug is required";
            return false;
        }

        form.Content = EventPageContentSerializer.Serialize(eventContent);
        return true;
    }

    private bool TryApplyProductId()
    {
        if (ActiveCategory != ContentCategory.DesignHistory)
            return true;

        if (string.IsNullOrWhiteSpace(formProductId))
        {
            form.ProductId = null;
            return true;
        }

        if (Guid.TryParse(formProductId.Trim(), out var productId) && productId != Guid.Empty)
        {
            form.ProductId = productId;
            return true;
        }

        formError = "Linked product ID must be a valid GUID.";
        return false;
    }

    private async Task<bool> TryValidateLinkedProductAsync()
    {
        if (ActiveCategory != ContentCategory.DesignHistory || form.ProductId == null)
            return true;

        var result = await ProductService.GetById(form.ProductId.Value);
        if (result.Success && result.Data != null)
            return true;

        formError = "Linked product ID does not match an existing product.";
        return false;
    }

    private Task HandleCreateSubmitAsync() => HandleCreate();

    private Task HandleEditSubmitAsync() => HandleEdit();

    private async Task<bool> HandleCreate(bool skipJournalPanelApply = false)
    {
        if (string.IsNullOrWhiteSpace(form.Title))
        {
            formError = "Title is required";
            return false;
        }

        if (!TryPrepareEventForm())
            return false;

        if (!TryApplyProductId())
            return false;

        if (!await TryValidateLinkedProductAsync())
            return false;

        if (!skipJournalPanelApply)
            ApplyJournalPanelToForm();

        if (!TryValidateJournalSchedule())
            return false;

        isSaving = true;
        formError = string.Empty;
        StateHasChanged();

        var result = await ContentService.Create(ActiveCategory, form);

        if (result.Success && result.Data != null)
        {
            items.Insert(0, result.Data);
            isFormOpen = false;
            SyncActiveTabCount();
            InvalidateContentCounts();
            _ = RefreshBackgroundMetricsAsync();
            isSaving = false;
            StateHasChanged();
            return true;
        }

        formError = result.Message ?? "Failed to create item";
        isSaving = false;
        StateHasChanged();
        return false;
    }

    private void OpenEditForm(ContentItemModel item)
    {
        isEditMode = true;
        editingItem = item;
        formError = string.Empty;
        isFormOpen = true;
        imageTab = "url";
        secondImageTab = "url";

        form = new CreateContentItemModel
        {
            Title = item.Title,
            Content = item.Content,
            Category = item.Category,
            ImageUrl = item.ImageUrl,
            Designer = item.Designer,
            Year = item.Year,
            Inspiration = item.Inspiration,
            Collection = item.Collection,
            SecondImageUrl = item.SecondImageUrl,
            TechnicalFabric = item.TechnicalFabric,
            TechnicalTechniques = item.TechnicalTechniques,
            TechnicalProduction = item.TechnicalProduction,
            TechnicalAvailability = item.TechnicalAvailability,
            ProductId = item.ProductId,
            Author = item.Author,
            Excerpt = item.Excerpt,
            Slug = item.Slug,
            SeoTitle = item.SeoTitle,
            Tags = item.Tags,
            IsFeatured = item.IsFeatured,
            ScheduledAt = item.ScheduledAt,
            ReadTimeMinutes = item.ReadTimeMinutes,
            Status = item.Status
        };

        formProductId = item.ProductId?.ToString() ?? string.Empty;

        eventContent = ActiveCategory == ContentCategory.Events
            ? EventPageContentSerializer.Parse(item.Content)
            : new MuuqsimoPageContentModel();

        if (ActiveCategory == ContentCategory.JournalArticles)
            LoadJournalPanelFromItem(item);
        else
            InitJournalFormFields();
    }

    private async Task<bool> HandleEdit(bool skipJournalPanelApply = false)
    {
        if (string.IsNullOrWhiteSpace(form.Title))
        {
            formError = "Title is required";
            return false;
        }

        if (!TryPrepareEventForm())
            return false;

        if (!TryApplyProductId())
            return false;

        if (!await TryValidateLinkedProductAsync())
            return false;

        if (!skipJournalPanelApply)
            ApplyJournalPanelToForm();

        if (!TryValidateJournalSchedule())
            return false;

        isSaving = true;
        formError = string.Empty;
        StateHasChanged();

        var result = await ContentService.Update(
            ActiveCategory,
            editingItem!.Id,
            BuildUpdateModel());

        if (result.Success && result.Data != null)
        {
            ReplaceItemInList(items, result.Data);
            isFormOpen = false;
            SyncActiveTabCount();
            InvalidateContentCounts();
            _ = RefreshBackgroundMetricsAsync();
            isSaving = false;
            StateHasChanged();
            return true;
        }

        formError = result.Message ?? "Failed to update item";
        isSaving = false;
        StateHasChanged();
        return false;
    }

    private void OpenDeleteModal(ContentItemModel item)
    {
        deletingItem = item;
        isDeleteModalOpen = true;
    }

    private void CloseDeleteModal()
    {
        isDeleteModalOpen = false;
        deletingItem = null;
    }

    private async Task ConfirmDelete()
    {
        if (deletingItem == null)
            return;

        isDeleting = true;
        pageError = string.Empty;
        StateHasChanged();

        var result = await ContentService.Delete(ActiveCategory, deletingItem.Id);
        if (result.Success)
        {
            items.RemoveAll(x => x.Id == deletingItem.Id);
            var title = deletingItem.Title;
            CloseDeleteModal();
            SyncActiveTabCount();
            await NotifyContentMutatedAsync();
            if (activeView is "journal" or "design-history" or "events")
                ShowToast($"\"{title}\" deleted");
        }
        else
        {
            pageError = result.Message ?? "Failed to delete item.";
        }

        isDeleting = false;
        StateHasChanged();
    }

    private bool ValidateImage(IBrowserFile file)
    {
        if (file.Size > 5 * 1024 * 1024)
        {
            formError = "Image must be under 5MB";
            return false;
        }

        if (!file.ContentType.StartsWith("image/"))
        {
            formError = "File must be an image";
            return false;
        }

        return true;
    }

    private async Task HandleImageUpload(InputFileChangeEventArgs e)
    {
        formError = string.Empty;
        var file = e.File;

        if (!ValidateImage(file))
            return;

        isUploading = true;
        StateHasChanged();

        try
        {
            using var stream = file.OpenReadStream(5 * 1024 * 1024);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            var result = await ContentService.UploadImage(
                $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}",
                ms.ToArray(),
                file.ContentType);

            if (result.Success && result.Data != null)
                form.ImageUrl = result.Data;
            else
                formError = result.Message ?? "Upload failed";
        }
        catch (Exception ex)
        {
            formError = "Upload failed: " + ex.Message;
        }
        finally
        {
            isUploading = false;
            StateHasChanged();
        }
    }

    private async Task HandleSecondImageUpload(InputFileChangeEventArgs e)
    {
        formError = string.Empty;
        var file = e.File;

        if (!ValidateImage(file))
            return;

        isSecondImageUploading = true;
        StateHasChanged();

        try
        {
            using var stream = file.OpenReadStream(5 * 1024 * 1024);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            var result = await ContentService.UploadImage(
                $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}",
                ms.ToArray(),
                file.ContentType);

            if (result.Success && result.Data != null)
                form.SecondImageUrl = result.Data;
            else
                formError = result.Message ?? "Upload failed";
        }
        catch (Exception ex)
        {
            formError = "Upload failed: " + ex.Message;
        }
        finally
        {
            isSecondImageUploading = false;
            StateHasChanged();
        }
    }

    private void SetEventEditorError(string message)
    {
        formError = message;
        StateHasChanged();
    }
}
