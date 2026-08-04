using MuuqWear.Web.Components.Pages.AdminComponent.Support;
using Xunit;

namespace MuuqWear.Tests.Admin;

public class SupportLiveChatUiHelperTests
{
    [Fact]
    public void BuildTabShellClass_returns_base_when_no_flags()
    {
        Assert.Equal("cs-tab-shell", SupportLiveChatUiHelper.BuildTabShellClass(false, false));
    }

    [Fact]
    public void BuildTabShellClass_adds_kb_open_when_kb_visible()
    {
        var result = SupportLiveChatUiHelper.BuildTabShellClass(showKbPanel: true, hasSelectedSession: false);

        Assert.Equal("cs-tab-shell cs-tab-shell--kb-open", result);
    }

    [Fact]
    public void BuildTabShellClass_adds_chat_open_when_session_selected()
    {
        var result = SupportLiveChatUiHelper.BuildTabShellClass(showKbPanel: false, hasSelectedSession: true);

        Assert.Equal("cs-tab-shell cs-tab-shell--chat-open", result);
    }

    [Fact]
    public void BuildTabShellClass_combines_kb_and_chat_flags()
    {
        var result = SupportLiveChatUiHelper.BuildTabShellClass(showKbPanel: true, hasSelectedSession: true);

        Assert.Equal("cs-tab-shell cs-tab-shell--kb-open cs-tab-shell--chat-open", result);
    }
}
