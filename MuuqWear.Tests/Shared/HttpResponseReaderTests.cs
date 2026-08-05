using System.Net;
using System.Text;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Customer;
using Xunit;

namespace MuuqWear.Tests.Shared;

public class HttpResponseReaderTests
{
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

    [Fact]
    public async Task ReadAsync_reads_wrapped_envelope()
    {
        var id = Guid.NewGuid();
        using var response = Json(
            "{\"success\":true,\"message\":\"ok\",\"data\":{\"id\":\"" + id +
            "\",\"accountStatus\":\"suspended\"}}");

        var result = await HttpResponseReader.ReadAsync<CustomerModel>(response);

        Assert.True(result.Success);
        Assert.Equal(id, result.Data?.Id);
        Assert.Equal("suspended", result.Data?.AccountStatus);
    }

    [Fact]
    public async Task ReadAsync_reads_unwrapped_payload()
    {
        var id = Guid.NewGuid();
        using var response = Json(
            "{\"id\":\"" + id + "\",\"accountStatus\":\"active\",\"fullName\":\"Sagal Ali\"}");

        var result = await HttpResponseReader.ReadAsync<CustomerModel>(response);

        Assert.True(result.Success);
        Assert.Equal(id, result.Data?.Id);
        Assert.Equal("Sagal Ali", result.Data?.FullName);
    }

    [Fact]
    public async Task ReadAsync_keeps_failure_from_wrapped_envelope()
    {
        using var response = Json("""{"success":false,"message":"Invalid customer id","data":null}""");

        var result = await HttpResponseReader.ReadAsync<CustomerModel>(response);

        Assert.False(result.Success);
        Assert.Equal("Invalid customer id", result.Message);
    }

    [Fact]
    public async Task ReadAsync_treats_empty_body_as_success()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NoContent)
        {
            Content = new StringContent(string.Empty)
        };

        var result = await HttpResponseReader.ReadAsync<CustomerModel>(response);

        Assert.True(result.Success);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task ReadAsync_treats_unreadable_body_on_success_status_as_success()
    {
        using var response = Json("not json at all");

        var result = await HttpResponseReader.ReadAsync<CustomerModel>(response);

        Assert.True(result.Success);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task ReadAsync_infers_success_when_envelope_omits_the_flag()
    {
        var id = Guid.NewGuid();
        using var response = Json("{\"data\":{\"id\":\"" + id + "\",\"accountStatus\":\"active\"}}");

        var result = await HttpResponseReader.ReadAsync<CustomerModel>(response);

        Assert.True(result.Success);
        Assert.Equal(id, result.Data?.Id);
    }

    [Fact]
    public async Task ReadAsync_keeps_success_when_envelope_data_does_not_fit_the_type()
    {
        using var response = Json("""{"success":true,"message":"Customer suspended","data":"ok"}""");

        var result = await HttpResponseReader.ReadAsync<CustomerModel>(response);

        Assert.True(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Customer suspended", result.Message);
    }

    [Fact]
    public async Task ReadAsync_keeps_failure_when_envelope_data_does_not_fit_the_type()
    {
        using var response = Json("""{"success":false,"message":"Invalid customer id","data":42}""");

        var result = await HttpResponseReader.ReadAsync<CustomerModel>(response);

        Assert.False(result.Success);
        Assert.Equal("Invalid customer id", result.Message);
    }

    [Fact]
    public async Task ReadAsync_fails_on_error_status()
    {
        using var response = Json(
            """{"success":false,"message":"Customer is already suspended"}""",
            HttpStatusCode.BadRequest);

        var result = await HttpResponseReader.ReadAsync<CustomerModel>(response);

        Assert.False(result.Success);
        Assert.Equal("Customer is already suspended", result.Message);
    }
}
