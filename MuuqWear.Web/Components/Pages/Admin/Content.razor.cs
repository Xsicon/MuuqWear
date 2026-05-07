using Microsoft.AspNetCore.Components.Forms;
using MuuqWear.Model.ContentItem;

namespace MuuqWear.Web.Components.Pages.Admin
{
    public partial class Content
    {
        private ContentCategory activeTab = ContentCategory.JournalArticles;
        private List<ContentItemModel> items = new();
        private bool isLoading = false;
        private bool isFormOpen = false;
        private string formError = string.Empty;
        private bool isSaving = false;
        private CreateContentItemModel form = new();
        private bool isEditMode = false;
        private ContentItemModel? editingItem = null;
        private bool isUploading = false;
        private string imageTab = "url";

        private static readonly ContentCategory[] Tabs =
        {
        ContentCategory.JournalArticles,
        ContentCategory.Events,
        ContentCategory.DesignHistory
    };

        protected override async Task OnInitializedAsync()
        {
            await LoadItems();
        }

        private async Task OnTabChanged(ContentCategory tab)
        {
            activeTab = tab;
            await LoadItems();
        }

        private async Task LoadItems()
        {
            isLoading = true;
            StateHasChanged();

            var result = await ContentService.GetAll(activeTab);
            items = result.Success && result.Data != null
                ? result.Data
                : new();

            isLoading = false;
            StateHasChanged();
        }

        private async Task HandlePublish(Guid id)
        {
            var result = await ContentService.Publish(activeTab, id);
            if (result.Success)
            {
                var item = items.FirstOrDefault(x => x.Id == id);
                if (item != null) item.Status = "published";
                StateHasChanged();
            }
        }

        private async Task HandleUnpublish(Guid id)
        {
            var result = await ContentService.Unpublish(activeTab, id);
            if (result.Success)
            {
                var item = items.FirstOrDefault(x => x.Id == id);
                if (item != null) item.Status = "draft";
                StateHasChanged();
            }
        }

        private string GetTabLabel(ContentCategory tab) => tab switch
        {
            ContentCategory.JournalArticles => "Journal Articles",
            ContentCategory.Events => "Events",
            ContentCategory.DesignHistory => "Design History",
            _ => tab.ToString()
        };

        private void OpenForm()
        {
            form = new CreateContentItemModel();
            formError = string.Empty;
            isFormOpen = true;
        }

        private void CloseForm()
        {
            isFormOpen = false;
            isEditMode = false;
            editingItem = null;
        }

        private async Task HandleCreate()
        {
            if (string.IsNullOrWhiteSpace(form.Title))
            { formError = "Title is required"; return; }

            isSaving = true;
            StateHasChanged();

            var result = await ContentService.Create(activeTab, form);

            if (result.Success && result.Data != null)
            {
                items.Insert(0, result.Data);
                isFormOpen = false;
            }
            else
            {
                formError = result.Message ?? "Failed to create item";
            }

            isSaving = false;
            StateHasChanged();
        }

        private void OpenEditForm(ContentItemModel item)
        {
            isEditMode = true;
            editingItem = item;
            formError = string.Empty;
            isFormOpen = true;
            imageTab = "url";

            form = new CreateContentItemModel
            {
                Title = item.Title,
                Content = item.Content,
                Category = item.Category,
                ImageUrl = item.ImageUrl

            };
        }

        private async Task HandleEdit()
        {
            if (string.IsNullOrWhiteSpace(form.Title))
            { formError = "Title is required"; return; }

            isSaving = true;
            StateHasChanged();

            var result = await ContentService.Update(
                activeTab,
                editingItem!.Id,
                new UpdateContentItemModel
                {
                    Title = form.Title,
                    Content = form.Content,
                    Category = form.Category,
                    ImageUrl = form.ImageUrl
                });

            if (result.Success && result.Data != null)
            {
                var index = items.FindIndex(x => x.Id == editingItem.Id);
                if (index >= 0) items[index] = result.Data;
                isFormOpen = false;
            }
            else
            {
                formError = result.Message ?? "Failed to update item";
            }

            isSaving = false;
            StateHasChanged();
        }

        private bool ValidateImage(IBrowserFile file)
        {
            if (file.Size > 5 * 1024 * 1024)
            { formError = "Image must be under 5MB"; return false; }

            if (!file.ContentType.StartsWith("image/"))
            { formError = "File must be an image"; return false; }

            return true;
        }

        private async Task HandleImageUpload(InputFileChangeEventArgs e)
        {
            formError = string.Empty;
            var file = e.File;

            if (!ValidateImage(file)) return;

            isUploading = true;
            StateHasChanged(); // ← show spinner

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
                StateHasChanged(); // ← hide spinner, show result 
            }
        }
    }
}