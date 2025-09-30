using Microsoft.EntityFrameworkCore;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.RulesEngine.Interfaces;
using SmartUnderwrite.Infrastructure.Data;

namespace SmartUnderwrite.Infrastructure.Repositories;

public class RuleRepository : IRuleRepository
{
    private readonly SmartUnderwriteDbContext _context;

    public RuleRepository(SmartUnderwriteDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Rule>> GetActiveRulesAsync()
    {
        return await _context.Rules
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync();
    }

    public async Task<Rule?> GetByIdAsync(int id)
    {
        return await _context.Rules.FindAsync(id);
    }

    public async Task<IEnumerable<Rule>> GetAllAsync()
    {
        return await _context.Rules.ToListAsync();
    }

    public async Task<Rule> CreateAsync(Rule rule)
    {
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task<Rule> UpdateAsync(Rule rule)
    {
        _context.Rules.Update(rule);
        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var rule = await _context.Rules.FindAsync(id);
        if (rule != null)
        {
            _context.Rules.Remove(rule);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }
}