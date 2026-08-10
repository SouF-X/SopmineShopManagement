using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Invoices;
using SopmineWorkshop.Infrastructure.Data;
using Xunit;

namespace SopmineWorkshop.Infrastructure.Tests;

public sealed class InvoicePaymentPersistenceTests
{
    [Fact]
    public async Task Payment_amount_is_mapped_with_currency_precision_and_persisted()
    {
        await using var database = await SqliteDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var amount = context.Model.FindEntityType(typeof(InvoicePayment))!
            .FindProperty(nameof(InvoicePayment.Amount))!;

        Assert.Equal(18, amount.GetPrecision());
        Assert.Equal(2, amount.GetScale());

        var invoice = CreateInvoice();
        Assert.False(invoice.RecordPayment(Guid.NewGuid(), 99.99m, new DateTime(2026, 7, 23), InvoicePaymentMethod.Virement, "PAY-001", null).IsError);

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        await using var verificationContext = database.CreateContext();
        var persistedPayment = await verificationContext.Set<InvoicePayment>().SingleAsync();
        Assert.Equal(99.99m, persistedPayment.Amount);
    }

    [Fact]
    public async Task Payment_relationship_is_loaded_from_its_invoice()
    {
        await using var database = await SqliteDatabase.CreateAsync();
        var invoice = CreateInvoice();
        Assert.False(invoice.RecordPayment(Guid.NewGuid(), 50m, new DateTime(2026, 7, 23), InvoicePaymentMethod.Carte, "PAY-002", null).IsError);

        await using (var setupContext = database.CreateContext())
        {
            setupContext.Invoices.Add(invoice);
            await setupContext.SaveChangesAsync();
        }

        await using var context = database.CreateContext();
        var persistedInvoice = await context.Invoices
            .Include(candidate => candidate.Payments)
            .SingleAsync(candidate => candidate.Id == invoice.Id);

        Assert.Single(persistedInvoice.Payments);
        Assert.Equal(invoice.Id, persistedInvoice.Payments.Single().InvoiceId);
    }

    [Fact]
    public async Task Invoice_delete_is_restricted_when_payments_exist()
    {
        await using var database = await SqliteDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var foreignKey = context.Model.FindEntityType(typeof(InvoicePayment))!
            .GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Invoice));

        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public async Task Stale_payment_revision_rejects_payment_and_rolls_back_its_row()
    {
        await using var database = await SqliteDatabase.CreateAsync();
        var invoice = CreateInvoice();

        await using (var setupContext = database.CreateContext())
        {
            setupContext.Invoices.Add(invoice);
            await setupContext.SaveChangesAsync();
        }

        await using var firstContext = database.CreateContext();
        await using var staleContext = database.CreateContext();
        var currentInvoice = await firstContext.Invoices.Include(candidate => candidate.Payments).SingleAsync(candidate => candidate.Id == invoice.Id);
        var staleInvoice = await staleContext.Invoices.Include(candidate => candidate.Payments).SingleAsync(candidate => candidate.Id == invoice.Id);

        Assert.False(currentInvoice.RecordPayment(Guid.NewGuid(), 40m, new DateTime(2026, 7, 23), InvoicePaymentMethod.Espece, "PAY-003", null).IsError);
        await firstContext.SaveChangesAsync();

        Assert.False(staleInvoice.RecordPayment(Guid.NewGuid(), 30m, new DateTime(2026, 7, 23), InvoicePaymentMethod.Cheque, "PAY-004", null).IsError);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleContext.SaveChangesAsync());

        await using var verificationContext = database.CreateContext();
        Assert.Single(await verificationContext.Set<InvoicePayment>().Where(payment => payment.InvoiceId == invoice.Id).ToListAsync());
    }

    private static Invoice CreateInvoice()
    {
        var invoiceId = Guid.NewGuid();
        var line = InvoiceLine.Create(
            Guid.NewGuid(),
            invoiceId,
            produitId: null,
            productReference: "SERVICE",
            productName: "Service",
            productFamily: "Services",
            productUnit: "unit",
            quantity: 1m,
            price: 100m,
            tva: 0m).Value;

        return Invoice.Create(
            invoiceId,
            $"FA-{Guid.NewGuid():N}",
            InvoiceType.Facture,
            InvoiceNature.Vente,
            DateTime.UtcNow.Date,
            fournisseurId: null,
            clientId: null,
            total: 100m,
            lines: [line],
            status: InvoiceStatus.Validated).Value;
    }

    private sealed class SqliteDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        private SqliteDatabase(SqliteConnection connection)
        {
            _connection = connection;
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
        }

        public static async Task<SqliteDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            connection.CreateCollation("Latin1_General_100_BIN2", static (left, right) => string.CompareOrdinal(left, right));
            var database = new SqliteDatabase(connection);

            await using var context = database.CreateContext();
            var schema = context.Database.GenerateCreateScript()
                .Replace("nvarchar(max)", "TEXT", StringComparison.Ordinal)
                .Replace(" COLLATE Latin1_General_100_BIN2", string.Empty, StringComparison.Ordinal)
                .Replace("N''", "''", StringComparison.Ordinal);
            await using var command = connection.CreateCommand();
            command.CommandText = schema;
            await command.ExecuteNonQueryAsync();
            return database;
        }

        public AppDbContext CreateContext() => new(_options);

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
}
