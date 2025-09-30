using Microsoft.EntityFrameworkCore;
using SmartUnderwrite.Core.RulesEngine.Interfaces;
using SmartUnderwrite.Core.RulesEngine.Models;
using SmartUnderwrite.Infrastructure.Data;

namespace SmartUnderwrite.Infrastructure.Repositories;

public class RuleVersionRepository : IRuleVersionRepository
{
    private readonly SmartUnderwriteDbContext _context;

    public RuleVersionRepository(SmartUnderwriteDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<RuleVersion>> GetRuleHistoryAsync(int originalRuleId)
    {
        return await _context.RuleVersions
            .Where(rv => rv.OriginalRuleId == originalRuleId)
            .OrderBy(rv => rv.Version)
            .ToListAsync();
    }

    public async Task<RuleVersion?> GetLatestVersionAsync(int originalRuleId)
    {
        return await _context.RuleVersions
            .Where(rv => rv.OriginalRuleId == originalRuleId)
            .OrderByDescending(rv => rv.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<RuleVersion> CreateAsync(RuleVersion ruleVersion)
    {
        _context.RuleVersions.Add(ruleVersion);
        await _context.SaveChangesAsync();
        return ruleVersion;
    }

    public async Task<RuleVersion?> GetByIdAsync(int id)
    {
        return await _context.RuleVersions.FindAsync(id);
    }

    public async Task<IEnumerable<RuleVersion>> GetAllAsync()
    {
        return await _context.RuleVersions
            .OrderBy(rv => rv.OriginalRuleId)
            .ThenBy(rv => rv.Version)
            .ToListAsync();
    }
}