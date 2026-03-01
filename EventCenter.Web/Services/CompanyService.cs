using EventCenter.Web.Domain;
using EventCenter.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventCenter.Web.Services;

public class CompanyService
{
    private readonly EventCenterDbContext _context;

    public CompanyService(EventCenterDbContext context)
    {
        _context = context;
    }

    public async Task<List<Company>> GetAllAsync()
    {
        return await _context.Companies
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Company?> GetByIdAsync(int id)
    {
        return await _context.Companies.FindAsync(id);
    }

    public async Task<List<Company>> SearchAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return await GetAllAsync();

        var lower = term.ToLower();
        return await _context.Companies
            .Where(c => c.Name.ToLower().Contains(lower) ||
                        c.ContactEmail.ToLower().Contains(lower) ||
                        (c.City != null && c.City.ToLower().Contains(lower)))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Company> CreateAsync(Company company)
    {
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        return company;
    }

    public async Task UpdateAsync(Company company)
    {
        _context.Companies.Update(company);
        await _context.SaveChangesAsync();
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var company = await _context.Companies
            .Include(c => c.EventCompanies)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (company == null)
            return (false, "Firma nicht gefunden.");

        if (company.EventCompanies.Any())
            return (false, "Firma kann nicht gelöscht werden, da sie bereits zu Einladungen verknüpft ist.");

        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();
        return (true, null);
    }
}
