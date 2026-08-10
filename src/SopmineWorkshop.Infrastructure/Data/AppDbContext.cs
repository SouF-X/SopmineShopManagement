using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Clients;
using SopmineWorkshop.Domain.Clients.Contacts;
using SopmineWorkshop.Domain.Common;
using SopmineWorkshop.Domain.Fournisseurs;
using SopmineWorkshop.Domain.Fournisseurs.ContactsDeFournisseur;
using SopmineWorkshop.Domain.Invoices;
using SopmineWorkshop.Domain.Produits;
using SopmineWorkshop.Domain.Settings;
using SopmineWorkshop.Infrastructure.Identity;

namespace SopmineWorkshop.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options), IAppDbContext
{
    public DbSet<Fournisseur> Fournisseurs => Set<Fournisseur>();
    public DbSet<ContactFournisseur> ContactsFournisseur => Set<ContactFournisseur>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ContactClient> ContactsClient => Set<ContactClient>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<InvoicePayment> InvoicePayments => Set<InvoicePayment>();
    public DbSet<Produit> Produits => Set<Produit>();
    public DbSet<FamilleProduit> FamillesProduit => Set<FamilleProduit>();
    public DbSet<UniteMesure> UnitesMesure => Set<UniteMesure>();
    public DbSet<DocumentNomination> DocumentNominations => Set<DocumentNomination>();
    public DbSet<DocumentReferenceSequence> DocumentReferenceSequences => Set<DocumentReferenceSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAtUtc == default)
                {
                    entry.Entity.CreatedAtUtc = now;
                }

                entry.Entity.LastModifiedUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(entity => entity.CreatedAtUtc).IsModified = false;
                entry.Entity.LastModifiedUtc = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
