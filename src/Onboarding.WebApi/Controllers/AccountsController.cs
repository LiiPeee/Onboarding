using Microsoft.AspNetCore.Mvc;
using Onboarding.Services.Interfaces;
using Onboarding.WebApi.Mappers;
using Onboarding.WebApi.Models.Request;
using Onboarding.WebApi.Models.Response;

namespace Onboarding.WebApi.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController(IAccountService accountService) : ControllerBase
{
    private readonly IAccountService _accountService = accountService;

    [HttpPost]
    public async Task<ActionResult<AccountResponse>> CreateAsync([FromBody] CreateAccountRequest request)
    {
        var created = await _accountService.CreateAsync(request.ToData());
        var response = created.ToResponse();
        return CreatedAtRoute("GetAccountById", new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> GetAllAsync()
    {
        var accounts = await _accountService.GetAllAsync();
        return Ok(accounts.Select(a => a.ToResponse()).ToList());
    }

    [HttpGet("{id:long}", Name = "GetAccountById")]
    public async Task<ActionResult<AccountResponse>> GetByIdAsync(long id)
    {
        var account = await _accountService.GetByIdAsync(id);
        return account is null ? NotFound() : Ok(account.ToResponse());
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<AccountResponse>> UpdateAsync(long id, [FromBody] UpdateAccountRequest request)
    {
        var updated = await _accountService.UpdateAsync(id, request.ToData());
        return Ok(updated.ToResponse());
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAsync(long id)
    {
        await _accountService.DeleteAsync(id);
        return NoContent();
    }
}
