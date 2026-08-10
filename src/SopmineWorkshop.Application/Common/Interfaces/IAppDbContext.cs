using Microsoft.EntityFrameworkCore;
using SopmineWorkshop.Domain.Clients;
using SopmineWorkshop.Domain.Clients.Contacts;
using SopmineWorkshop.Domain.Fournisseurs;
using SopmineWorkshop.Domain.Fournisseurs.ContactsDeFournisseur;
using SopmineWorkshop.Domain.Invoices;
using SopmineWorkshop.Domain.Produits;
using SopmineWorkshop.Domain.Settings;

namespace SopmineWorkshop.Application.Common.Interfaces;

public interface IAppDbContext
{
    public DbSet<Fournisseur> Fournisseurs { get; }
    public DbSet<ContactFournisseur> ContactsFournisseur { get; }
    public DbSet<Client> Clients { get; }
    public DbSet<ContactClient> ContactsClient { get; }
    public DbSet<Invoice> Invoices { get; }
    public DbSet<InvoiceLine> InvoiceLines { get; }
    public DbSet<InvoicePayment> InvoicePayments { get; }
    public DbSet<Produit> Produits { get; }
    public DbSet<FamilleProduit> FamillesProduit { get; }
    public DbSet<UniteMesure> UnitesMesure { get; }
    public DbSet<DocumentNomination> DocumentNominations { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
