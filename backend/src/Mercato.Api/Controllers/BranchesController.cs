using Mercato.Api.Services;
using Mercato.Application.DTOs;
using Mercato.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/branches")]
[Authorize]
public sealed class BranchesController : ControllerBase
{
    private readonly IBranchService _branches;
    private readonly CurrentUserBranchAccess _branchAccess;

    public BranchesController(IBranchService branches, CurrentUserBranchAccess branchAccess)
    {
        _branches = branches;
        _branchAccess = branchAccess;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await _branches.GetAllAsync(cancellationToken));

    [HttpGet("accessible")]
    [Authorize(Roles = "Admin,Manager,Cashier")]
    public async Task<IActionResult> GetAccessible(CancellationToken cancellationToken)
    {
        var branches = await _branches.GetAllAsync(cancellationToken);
        if (_branchAccess.IsAdmin) return Ok(branches);
        var allowed = (await _branchAccess.GetAllowedBranchIdsAsync(cancellationToken)).ToHashSet();
        return Ok(branches.Where(x => allowed.Contains(x.Id)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var branch = await _branches.GetAsync(id, cancellationToken);
        return branch is null ? NotFound() : Ok(branch);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(CreateBranchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var branch = await _branches.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = branch.Id }, branch);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(Guid id, UpdateBranchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var branch = await _branches.UpdateAsync(id, request, cancellationToken);
            return branch is null ? NotFound() : Ok(branch);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => await _branches.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
