using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using FraudEngine.Infrastructure.Data;
using FraudEngine.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FraudEngine.Infrastructure;

public static class IntegrationTestDependencyInjection
{
    public static IServiceCollection AddIntegrationTestInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryFraudStore>();
        services.AddScoped<ITransactionRepository, InMemoryTransactionRepository>();
        services.AddScoped<IEvaluationRepository, InMemoryEvaluationRepository>();
        services.AddScoped<IRuleRepository, InMemoryRuleRepository>();
        services.AddScoped<IVelocityService, InMemoryVelocityService>();
        services.AddScoped<IRulesEngineService, RulesEngineService>();

        services.AddHealthChecks()
            .AddCheck("in-memory", () => HealthCheckResult.Healthy());

        return services;
    }

    private sealed class InMemoryFraudStore
    {
        private readonly Dictionary<string, Queue<DateTimeOffset>> _velocityWindows =
            new(StringComparer.OrdinalIgnoreCase);

        public InMemoryFraudStore()
        {
            Rules = AppDbContextSeed.GetSeedRules().ToList();
        }

        public object SyncRoot { get; } = new();

        public List<Transaction> Transactions { get; } = new();

        public List<FraudEvaluation> Evaluations { get; } = new();

        public List<RuleDefinition> Rules { get; }

        public Dictionary<string, Queue<DateTimeOffset>> VelocityWindows => _velocityWindows;
    }

    private sealed class InMemoryTransactionRepository : ITransactionRepository
    {
        private readonly InMemoryFraudStore _store;

        public InMemoryTransactionRepository(InMemoryFraudStore store)
        {
            _store = store;
        }

        public Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            lock (_store.SyncRoot)
            {
                _store.Transactions.Add(transaction);
            }

            return Task.CompletedTask;
        }

        public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            lock (_store.SyncRoot)
            {
                return Task.FromResult(_store.Transactions.FirstOrDefault(transaction => transaction.Id == id));
            }
        }

        public Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedAsync(
            string? decision, string? accountId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize,
            CancellationToken cancellationToken = default)
        {
            lock (_store.SyncRoot)
            {
                IEnumerable<Transaction> query = _store.Transactions.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(accountId))
                    query = query.Where(transaction => transaction.AccountId == accountId);

                if (from.HasValue)
                    query = query.Where(transaction => transaction.Timestamp >= from.Value);

                if (to.HasValue)
                    query = query.Where(transaction => transaction.Timestamp <= to.Value);

                if (!string.IsNullOrWhiteSpace(decision))
                {
                    string normalizedDecision = decision.Trim().ToUpperInvariant();
                    var transactionIds = _store.Evaluations
                        .Where(evaluation => evaluation.Decision.ToString() == normalizedDecision)
                        .Select(evaluation => evaluation.TransactionId)
                        .ToHashSet();

                    query = query.Where(transaction => transactionIds.Contains(transaction.Id));
                }

                List<Transaction> orderedItems = query
                    .OrderByDescending(transaction => transaction.Timestamp)
                    .ToList();

                int totalCount = orderedItems.Count;
                IEnumerable<Transaction> pageItems = orderedItems
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Task.FromResult((pageItems, totalCount));
            }
        }

        public Task<bool> ExistsRecentDuplicateAsync(string accountId, decimal amount, string merchantName,
            DateTimeOffset since, CancellationToken cancellationToken = default)
        {
            lock (_store.SyncRoot)
            {
                bool isDuplicate = _store.Transactions.Any(transaction =>
                    transaction.AccountId == accountId &&
                    transaction.Amount == amount &&
                    transaction.MerchantName == merchantName &&
                    transaction.Timestamp >= since);

                return Task.FromResult(isDuplicate);
            }
        }
    }

    private sealed class InMemoryEvaluationRepository : IEvaluationRepository
    {
        private readonly InMemoryFraudStore _store;

        public InMemoryEvaluationRepository(InMemoryFraudStore store)
        {
            _store = store;
        }

        public Task AddAsync(FraudEvaluation evaluation, CancellationToken cancellationToken = default)
        {
            lock (_store.SyncRoot)
            {
                _store.Evaluations.Add(evaluation);
            }

            return Task.CompletedTask;
        }

        public Task<(IEnumerable<FraudEvaluation> Items, int TotalCount)> GetPagedAsync(
            Decision? decision, int? minScore, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            lock (_store.SyncRoot)
            {
                IEnumerable<FraudEvaluation> query = _store.Evaluations.AsEnumerable();

                if (decision.HasValue)
                    query = query.Where(evaluation => evaluation.Decision == decision.Value);

                if (minScore.HasValue)
                    query = query.Where(evaluation => evaluation.RiskScore >= minScore.Value);

                List<FraudEvaluation> orderedItems = query
                    .OrderByDescending(evaluation => evaluation.EvaluatedAt)
                    .ToList();

                int totalCount = orderedItems.Count;
                IEnumerable<FraudEvaluation> pageItems = orderedItems
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Task.FromResult((pageItems, totalCount));
            }
        }
    }

    private sealed class InMemoryRuleRepository : IRuleRepository
    {
        private readonly InMemoryFraudStore _store;

        public InMemoryRuleRepository(InMemoryFraudStore store)
        {
            _store = store;
        }

        public Task<IEnumerable<RuleDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            lock (_store.SyncRoot)
            {
                IEnumerable<RuleDefinition> rules = _store.Rules
                    .OrderBy(rule => rule.RuleName)
                    .ToList();

                return Task.FromResult(rules);
            }
        }

        public Task<IEnumerable<RuleDefinition>> GetActiveRulesAsync(CancellationToken cancellationToken = default)
        {
            lock (_store.SyncRoot)
            {
                IEnumerable<RuleDefinition> rules = _store.Rules
                    .Where(rule => rule.IsActive)
                    .ToList();

                return Task.FromResult(rules);
            }
        }

        public Task<RuleDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            lock (_store.SyncRoot)
            {
                return Task.FromResult(_store.Rules.FirstOrDefault(rule => rule.Id == id));
            }
        }

        public Task UpdateAsync(RuleDefinition rule, CancellationToken cancellationToken = default)
        {
            lock (_store.SyncRoot)
            {
                int index = _store.Rules.FindIndex(item => item.Id == rule.Id);
                if (index >= 0)
                    _store.Rules[index] = rule;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryVelocityService : IVelocityService
    {
        private static readonly TimeSpan Window = TimeSpan.FromSeconds(60);
        private readonly InMemoryFraudStore _store;

        public InMemoryVelocityService(InMemoryFraudStore store)
        {
            _store = store;
        }

        public Task<int> GetRecentTransactionCountAsync(string accountId, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            lock (_store.SyncRoot)
            {
                if (!_store.VelocityWindows.TryGetValue(accountId, out Queue<DateTimeOffset>? timestamps))
                {
                    timestamps = new Queue<DateTimeOffset>();
                    _store.VelocityWindows[accountId] = timestamps;
                }

                while (timestamps.Count > 0 && now - timestamps.Peek() > Window)
                    timestamps.Dequeue();

                timestamps.Enqueue(now);
                return Task.FromResult(timestamps.Count);
            }
        }
    }
}
