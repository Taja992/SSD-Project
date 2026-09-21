namespace Vms.Api.Core.Application.Exceptions;

/// <summary>
/// Thrown when an authenticated user may not act on a specific resource.
/// Distinct from "not found" on purpose — the endpoint decides what to leak.
/// </summary>
public class ForbiddenException(string message) : Exception(message);
