using EmployeeApi.Data;
using EmployeeApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace EmployeeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConnectionMultiplexer _redis;

    public EmployeesController(AppDbContext context, IConnectionMultiplexer redis)
    {
        _context = context;
        _redis = redis;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployees()
    {
        var db = _redis.GetDatabase();

        var cached = await db.StringGetAsync("employees");

        if (!cached.IsNullOrEmpty)
        {
            var cachedEmployees =
                JsonSerializer.Deserialize<List<Employee>>(cached.ToString())
                ?? new List<Employee>();

            return Ok(cachedEmployees);
        }

        var employees = await _context.Employees.ToListAsync();

        await db.StringSetAsync(
            "employees",
            JsonSerializer.Serialize(employees),
            TimeSpan.FromMinutes(5));

        return Ok(employees);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmployee(int id)
    {
        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
            return NotFound();

        return Ok(employee);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Employee employee)
    {
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        await _redis.GetDatabase().KeyDeleteAsync("employees");

        return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Employee employee)
    {
        if (id != employee.Id)
            return BadRequest();

        _context.Entry(employee).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await _redis.GetDatabase().KeyDeleteAsync("employees");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
            return NotFound();

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();

        await _redis.GetDatabase().KeyDeleteAsync("employees");

        return NoContent();
    }
}
