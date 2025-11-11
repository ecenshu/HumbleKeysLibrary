using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Models;
using Newtonsoft.Json;

namespace HumbleKeys.Services
{
    public interface IOrderKeysService
    {
        ICollection<string> GetKeys();
        Task<ICollection<string>> GetKeysAsync();
    }

    public interface IOrdersService
    {
        ICollection<Order> GetOrders();
        Task<ICollection<Order>> GetOrdersAsync(CancellationToken cancellationToken);
        
        Order GetOrder(string orderKey);
        Task<Order> GetOrderAsync(string orderKey, CancellationToken cancellationToken);
    }

    public interface IHumbleChoiceService
    {
        ICollection<IChoiceMonth> GetChoiceMonth(OrderKey orderKey);
        Task<ICollection<IChoiceMonth>> GetChoiceMonthAsync(OrderKey orderKey, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Relatively safe to use as struct since the OrderKey is a 16 character string in utf8 format that is only alphanumeric
    /// </summary>
    public struct OrderKey : IEquatable<OrderKey>
    {
        public OrderKey(string orderKey)
        {
            Id = orderKey;
        }
        private string Id {get; set;}
        public override bool Equals(object obj)
        {
            if (obj is string stringValue)
            {
                return Id.Equals(stringValue);
            }
            return base.Equals(obj);
        }

        public bool Equals(OrderKey other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return (Id != null ? Id.GetHashCode() : 0);
        }

        public override string ToString() => Id;
    }
    
    public class OrderKeyConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Serialize the value object as a simple string
            writer.WriteValue(value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer jsonSerializer)
        {
            // Read the string value from JSON and create a new MyValueObject
            if (reader.TokenType == JsonToken.String)
            {
                return new OrderKey(reader.Value?.ToString());
            }
            // Handle other token types or null appropriately
            return null; 
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(OrderKey);
        }
    }
}