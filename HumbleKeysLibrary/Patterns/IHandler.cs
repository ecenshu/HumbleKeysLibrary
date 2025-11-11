namespace HumbleKeys.Patterns
{
    public interface IHandler<T,TResult>
    {
        TResult Handle(T request);
        IHandler<T,TResult> SetNext(IHandler<T,TResult> handler);
    }
}