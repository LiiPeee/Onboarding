using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.Services.Interfaces;
using Onboarding.WebApi.Mappers;
using Onboarding.WebApi.Models.Request;
using Onboarding.WebApi.Models.Response;

namespace Onboarding.WebApi.Controllers;

[ApiController]
[Route("api/accounts")]
//[Authorize]
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
    public async Task<ActionResult> GetAllAsync([FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
    {
        var accounts = await _accountService.GetAllAsync(page,pageSize);
        return Ok(accounts);
    }

    [HttpGet("{id:long}", Name = "GetAccountById")]
    public async Task<ActionResult<AccountResponse>> GetByIdAsync(long id)
    {
        var account = await _accountService.GetByIdAsync(id);
        return account is null ? NotFound() : Ok(account.ToResponse());
    }

    [HttpGet("cpf/{cpf}")]
    public async Task<ActionResult<AccountResponse>> GetByCpfAsync(string cpf)
    {
        var account = await _accountService.GetByCpfAsync(cpf);
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
