using OrderApi.Functions.Models;
using System.Collections.Concurrent;

namespace OrderApi.Functions.Idempotency
{
    // We support at-least-once delivery by using an Idempotency-Key.
    // If a request with the same key is repeated, we return the cached response and avoid duplicate processing.

    public interface IIdempotencyStore
    {
        bool TryGet(string key, out CreateOrderAcceptedResponse response);
        void Set(string key, CreateOrderAcceptedResponse response);
    }

    public sealed class InMemoryIdempotencyStore : IIdempotencyStore
    {
        private readonly ConcurrentDictionary<string, CreateOrderAcceptedResponse> _store = new();

        public bool TryGet(string key, out CreateOrderAcceptedResponse response)
            => _store.TryGetValue(key, out response!);

        public void Set(string key, CreateOrderAcceptedResponse response)
            => _store.TryAdd(key, response);
    }
}
