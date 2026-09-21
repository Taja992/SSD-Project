using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Vms.Api.Core.Application.Exceptions;
using Vms.Api.Core.Application.Interfaces;
using Vms.Api.Core.Domain.Dto.Finding;
using Vms.Domain.Enums;

namespace Vms.Api.Api;

public static class FindingsApi
{
    public static RouteGroupBuilder MapFindingsApi(this IEndpointRouteBuilder app)
    {
        // Every endpoint below requires an authenticated staff role.
        // Individual endpoints tighten further; none loosens.
        var api = app.MapGroup("/api/findings")
                     .RequireAuthorization("AnyStaff")
                     .WithTags("findings");

        api.MapGet("/", SearchFindings)
            .WithName("Search findings.")
            .WithDescription("Filters by status, severity and assignee. Paged, max 100 per page.");

        api.MapGet("/{findingId:guid}", GetFinding)
            .WithName("Get finding.")
            .WithDescription("Q1: any authenticated staff role may read any finding.");

        api.MapPost("/", CreateFinding)
            .RequireAuthorization("AnalystOrPm")
            .WithName("Create finding.")
            .WithDescription("Analyst/PM only. Status and source are set by the server.");

        api.MapPut("/{findingId:guid}", UpdateFinding)
            .WithName("Update finding.")
            .WithDescription("Assignee, Analyst or PM only - enforced per record, not by role alone.");

        api.MapPost("/{findingId:guid}/triage", TriageFinding)
            .RequireAuthorization("AnalystOrPm")
            .WithName("Triage finding.")
            .WithDescription("Sets severity, assignee and SLA(Service Level Agreement) due date.");

        return api;
    }

    // ---------------------------------------------------------------------
    // Handlers. Named methods, not lambdas: they can be unit-tested directly,
    // and the Results<...> union documents every status the endpoint returns.
    // ---------------------------------------------------------------------

    public static async Task<Ok<PagedResult<FindingDto>>> SearchFindings(
        [FromServices] IFindingService findingService,
        FindingStatus? status,
        Severity? severity,
        string? assignee,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 25)
    {
        var result = await findingService.SearchAsync(
            status, severity, assignee, page, pageSize, cancellationToken);
        return TypedResults.Ok(result);
    }

    public static async Task<Results<Ok<FindingDto>, NotFound>> GetFinding(
        Guid findingId,
        [FromServices] IFindingService findingService,
        CancellationToken cancellationToken)
    {
        var finding = await findingService.GetAsync(findingId, cancellationToken);
        return finding is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(finding);
    }

    public static async Task<Created<FindingDto>> CreateFinding(
        CreateFindingDto dto,
        [FromServices] IFindingService findingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var created = await findingService.CreateAsync(dto, user, cancellationToken);
        return TypedResults.Created($"/api/findings/{created.Id}", created);
    }

    public static async Task<Results<Ok<FindingDto>, NotFound, ForbidHttpResult>> UpdateFinding(
        Guid findingId,
        UpdateFindingDto dto,
        [FromServices] IFindingService findingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await findingService.UpdateAsync(findingId, dto, user, cancellationToken);
            return updated is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(updated);
        }
        catch (ForbiddenException)
        {
            // 403, not 404: per Q1 the caller may already read this finding,
            // so hiding its existence here would buy nothing (T-API-03).
            return TypedResults.Forbid();
        }
    }

    public static async Task<Results<Ok<FindingDto>, NotFound>> TriageFinding(
        Guid findingId,
        TriageFindingDto dto,
        [FromServices] IFindingService findingService,
        CancellationToken cancellationToken)
    {
        var updated = await findingService.TriageAsync(findingId, dto, cancellationToken);
        return updated is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(updated);
    }
}
