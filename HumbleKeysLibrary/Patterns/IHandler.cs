using System;
using System.Collections.Generic;
using System.Threading;
using HumbleKeys.Services;

namespace HumbleKeys.Patterns
{
    public interface IHandler<T, TResult>
    {
        TResult Handle(T request);
        IHandler<T, TResult> SetNext(IHandler<T, TResult> handler);
    }

    public interface IHandler<TResult>
    {
        TResult Handle();
        IHandler<TResult> SetNext(IHandler<TResult> handler);
    }

    public abstract class BaseHandler<T, TResult> : IHandler<T, TResult>
    {
        private IHandler<T, TResult> nextHandler;

        public IHandler<T, TResult> SetNext(IHandler<T, TResult> handler)
        {
            nextHandler = handler;
            return handler;
        }

        public virtual TResult Handle(T request)
        {
            if (nextHandler == null) throw new Exception("Handler not set");
            
            var result = nextHandler.Handle(request); // Or a default/error message
            return result;
        }
    }

    public abstract class BaseHandler<TResult> : IHandler<TResult>
    {
        private IHandler<TResult> nextHandler;

        public IHandler<TResult> SetNext(IHandler<TResult> handler)
        {
            nextHandler = handler;
            return handler;
        }

        public virtual TResult Handle()
        {
            if (nextHandler == null) throw new Exception("Handler not set");
            
            var result = nextHandler.Handle(); // Or a default/error message
            return result;
        }
    }

    public class GetOrderKeysCachedHandler : BaseHandler<ICollection<OrderKey>>
    {
        private readonly IStrategy<ICollection<OrderKey>> strategy;

        public GetOrderKeysCachedHandler(FileSystemCacheStrategy<ICollection<OrderKey>> strategy)
        {
            this.strategy = strategy;
        }
        
        public override ICollection<OrderKey> Handle()
        {
            var response = strategy.Execute();
            if (response != null && response.Count > 0)
            {
                return response;
            }
            return base.Handle();
        }
    }

    public class GetOrderKeysHumbleHandler :  BaseHandler<ICollection<OrderKey>>
    {
        private readonly IDataProvider<ICollection<OrderKey>> dataProvider;

        public GetOrderKeysHumbleHandler(IDataProvider<ICollection<OrderKey>> dataProvider)
        {
            this.dataProvider = dataProvider;
        }

        public override ICollection<OrderKey> Handle()
        {
            var result = dataProvider.GetData();
            if (result != null && result.Count > 0)
            {
                return result;
            }
            return base.Handle();
        }
    }
}