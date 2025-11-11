namespace HumbleKeys.Patterns
{
    public interface IStrategy<in T, out TResult>
    {
        TResult Execute(T data); 
    }
}