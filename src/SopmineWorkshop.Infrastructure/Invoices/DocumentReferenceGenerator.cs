using System.Data;
using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using SopmineWorkshop.Application.Common.Interfaces;
using SopmineWorkshop.Domain.Enums;
using SopmineWorkshop.Domain.Settings;
using SopmineWorkshop.Infrastructure.Data;

namespace SopmineWorkshop.Infrastructure.Invoices;

public sealed class DocumentReferenceGenerator(IDbContextFactory<AppDbContext> contextFactory) : IDocumentReferenceGenerator
{
    public async Task<string> GenerateAsync(InvoiceNature nature, InvoiceType type, DateTime documentDate, CancellationToken cancellationToken)
    {
        var definition = DocumentNominationCatalog.Find(nature, type)
            ?? throw new InvalidOperationException($"Unsupported document type: {nature}/{type}.");

        await using var strategyContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = strategyContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            var setting = await context.DocumentNominations.AsNoTracking().FirstOrDefaultAsync(item =>
                item.Nature == nature && item.Type == type, cancellationToken);
            var root = string.IsNullOrWhiteSpace(setting?.Root)
                ? DocumentNominationCatalog.DefaultRoot(definition, DateTime.UtcNow)
                : setting.Root.Trim();
            var prefix = DocumentReferenceFormat.BuildPrefix(root, setting?.DateFormat ?? "MM", documentDate);
            await AcquireApplicationLockAsync(context, prefix, cancellationToken);

            var sequence = await context.DocumentReferenceSequences.FirstOrDefaultAsync(item => item.Scope == prefix, cancellationToken);
            var legacyMax = 0L;
            if (sequence is null)
            {
                sequence = new DocumentReferenceSequence(prefix);
                context.DocumentReferenceSequences.Add(sequence);

                var references = await context.Invoices.AsNoTracking()
                    .Where(invoice => invoice.Reference.StartsWith(prefix + "-"))
                    .Select(invoice => invoice.Reference)
                    .ToListAsync(cancellationToken);
                legacyMax = references.Select(reference => DocumentReferenceFormat.TryReadSequence(reference, prefix))
                    .Where(value => value.HasValue)
                    .Select(value => value!.Value)
                    .DefaultIfEmpty(0L)
                    .Max();
            }

            var next = Math.Max(sequence.LastSequence, legacyMax) + 1;
            sequence.SetLastSequence(next);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return DocumentReferenceFormat.BuildReference(prefix, next, setting?.IncrementSize ?? 3);
        });
    }

    private static async Task AcquireApplicationLockAsync(AppDbContext context, string prefix, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "EXEC @result = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction'; SELECT @result;";
        var result = command.CreateParameter();
        result.ParameterName = "@result";
        result.Direction = ParameterDirection.Output;
        result.DbType = DbType.Int32;
        command.Parameters.Add(result);
        var resource = command.CreateParameter();
        resource.ParameterName = "@resource";
        resource.Value = $"DocumentReference:{prefix}";
        resource.DbType = DbType.String;
        resource.Size = 255;
        command.Parameters.Add(resource);
        await command.ExecuteNonQueryAsync(cancellationToken);
        if (Convert.ToInt32(result.Value) < 0)
            throw new InvalidOperationException($"Could not acquire document reference lock for '{prefix}'.");
    }
}
