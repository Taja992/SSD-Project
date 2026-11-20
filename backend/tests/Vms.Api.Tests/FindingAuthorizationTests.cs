using Vms.Domain.Entities;
using Vms.Domain.Enums;

namespace Vms.Api.Tests;

public class FindingAuthorizationTests
{
    private static Finding WithAssignee(string? assignee) => new()
    {
        Title = "t", Description = "d", AffectedAsset = "a",
        AssigneeUsername = assignee
    };

    private static Finding WithStatus(FindingStatus status) => new()
    {
        Title = "t", Description = "d", AffectedAsset = "a",
        Status = status
    };

    // ---- Who may edit (IsEditableBy) --------------------------------------

    [Fact]
    public void Assignee_can_edit_their_own_finding() =>
        Assert.True(WithAssignee("dev.test").IsEditableBy("dev.test", isAnalystOrPm: false, isDeveloper: true));

    [Fact]  // T-API-03 — the regression test that matters
    public void Developer_cannot_edit_someone_elses_finding() =>
        Assert.False(WithAssignee("other.dev").IsEditableBy("dev.test", isAnalystOrPm: false, isDeveloper: true));

    [Fact]
    public void Developer_cannot_edit_an_unassigned_finding() =>
        Assert.False(WithAssignee(null).IsEditableBy("dev.test", isAnalystOrPm: false, isDeveloper: true));

    [Fact]
    public void Analyst_can_edit_any_finding() =>
        Assert.True(WithAssignee("other.dev").IsEditableBy("analyst.test", isAnalystOrPm: true, isDeveloper: false));

    [Fact]  // T-API-16 — an assigned Auditor is still read-only (Q2)
    public void Auditor_cannot_edit_a_finding_assigned_to_them() =>
        Assert.False(WithAssignee("auditor.test").IsEditableBy("auditor.test", isAnalystOrPm: false, isDeveloper: false));

    // ---- Which status changes (CanMoveTo) ---------------------------------

    [Theory]
    [InlineData(FindingStatus.Triaged, FindingStatus.InProgress)]
    [InlineData(FindingStatus.InProgress, FindingStatus.Remediated)]
    public void Developer_can_move_their_work_forward(FindingStatus from, FindingStatus to) =>
        Assert.True(WithStatus(from).CanMoveTo(to, isAnalystOrPm: false));

    [Theory]
    [InlineData(FindingStatus.New)]
    [InlineData(FindingStatus.Triaged)]
    [InlineData(FindingStatus.InProgress)]
    [InlineData(FindingStatus.Remediated)]
    [InlineData(FindingStatus.AcceptedRisk)]
    public void Developer_can_keep_the_status_while_editing_details(FindingStatus status) =>
        Assert.True(WithStatus(status).CanMoveTo(status, isAnalystOrPm: false));

    [Theory]  // T-API-04 — accepting a risk is not a Developer's decision
    [InlineData(FindingStatus.New)]
    [InlineData(FindingStatus.Triaged)]
    [InlineData(FindingStatus.InProgress)]
    [InlineData(FindingStatus.Remediated)]
    public void Developer_cannot_accept_risk(FindingStatus from) =>
        Assert.False(WithStatus(from).CanMoveTo(FindingStatus.AcceptedRisk, isAnalystOrPm: false));

    [Theory]
    [InlineData(FindingStatus.Triaged, FindingStatus.Remediated)]   // skip the work
    [InlineData(FindingStatus.Remediated, FindingStatus.InProgress)] // reopen
    [InlineData(FindingStatus.InProgress, FindingStatus.New)]        // undo triage
    [InlineData(FindingStatus.AcceptedRisk, FindingStatus.Remediated)]
    public void Developer_cannot_skip_or_go_back(FindingStatus from, FindingStatus to) =>
        Assert.False(WithStatus(from).CanMoveTo(to, isAnalystOrPm: false));

    [Theory]
    [InlineData(FindingStatus.New, FindingStatus.AcceptedRisk)]
    [InlineData(FindingStatus.Remediated, FindingStatus.InProgress)]
    public void Analyst_can_set_any_status(FindingStatus from, FindingStatus to) =>
        Assert.True(WithStatus(from).CanMoveTo(to, isAnalystOrPm: true));
}
