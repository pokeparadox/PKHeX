namespace PKHeX.Rest.Extensions
{
    public static class TupleExtensions
    {
        public static TStatus TryOut<TStatus,TValue>(this ValueTuple<TStatus, TValue> tuple, out TValue value)
        {
            TStatus? status =  default;
            (status, value) = tuple;
            return status;
        }
    }

}
