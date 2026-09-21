using Vms.Domain.Entities;

namespace Vms.Api.Tests;

public class FindingAuthorizationTests
{
    private static Finding WithAssignee(string? assignee) => new()
    {
        Title = "t", Description = "d", AffectedAsset = "a",
        AssigneeUsername = assignee
    };

    [Fact]
    public void Assignee_can_edit_their_own_finding() =>
        Assert.True(WithAssignee("dev.test").IsEditableBy("dev.test", isAnalystOrPm: false));

    [Fact]  // T-API-03 — the regression test that matters
    public void Developer_cannot_edit_someone_elses_finding() =>
        Assert.False(WithAssignee("other.dev").IsEditableBy("dev.test", isAnalystOrPm: false));

    [Fact]
    public void Developer_cannot_edit_an_unassigned_finding() =>
        Assert.False(WithAssignee(null).IsEditableBy("dev.test", isAnalystOrPm: false));

    [Fact]
    public void Analyst_can_edit_any_finding() =>
        Assert.True(WithAssignee("other.dev").IsEditableBy("analyst.test", isAnalystOrPm: true));
}
