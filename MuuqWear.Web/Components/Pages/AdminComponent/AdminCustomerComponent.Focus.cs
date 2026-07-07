using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.WebUtilities;
using MuuqWear.Model.Customer;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminCustomerComponent
{
    [SupplyParameterFromQuery(Name = "customerId")]
    public string? CustomerIdQuery { get; set; }

    private Guid? pendingFocusCustomerId;

    private void ApplyCustomerFocusFromQuery()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("customerId", out var value) &&
            Guid.TryParse(value.ToString(), out var customerId))
        {
            pendingFocusCustomerId = customerId;
            return;
        }

        if (!string.IsNullOrWhiteSpace(CustomerIdQuery) &&
            Guid.TryParse(CustomerIdQuery, out var fromParam))
        {
            pendingFocusCustomerId = fromParam;
        }
    }

    private void OnCustomerFocusRequested(Guid customerId)
    {
        pendingFocusCustomerId = customerId;

        _ = InvokeAsync(async () =>
        {
            if (activeView != "notes")
                activeView = "notes";

            currentPage = 1;
            await LoadCustomers();
            await TryFocusCustomerAsync();
            StateHasChanged();
        });
    }

    private async Task TryFocusCustomerAsync()
    {
        if (!pendingFocusCustomerId.HasValue)
            return;

        var customerId = pendingFocusCustomerId.Value;
        pendingFocusCustomerId = null;

        var customer = await FindCustomerAsync(customerId);
        if (customer == null)
            return;

        if (activeView == "details")
            await SelectCustomerForDetailsWithNotes(customer);
        else
            await OpenNotesPanel(customer);
    }

    private async Task<CustomerModel?> FindCustomerAsync(Guid customerId)
    {
        var customer = customers.FirstOrDefault(c => c.Id == customerId);
        if (customer != null)
            return customer;

        var result = await CustomerService.GetAll(null, 1, 1000);
        if (!result.Success || result.Data?.Data == null)
            return null;

        customer = result.Data.Data.FirstOrDefault(c => c.Id == customerId);
        if (customer == null)
            return null;

        if (customers.All(c => c.Id != customerId))
            customers.Insert(0, customer);

        return customer;
    }
}
