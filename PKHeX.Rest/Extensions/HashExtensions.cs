using System.Security.Cryptography;

namespace PKHeX.Rest.Extensions
{
    public static class HashExtensions
    {
        public static async Task<string> HashStringAsync(this byte[] dataBytes, CancellationToken cancel = default)
        {
            MemoryStream memoryStream = new(dataBytes);
            var hashedBytes = await SHA256.HashDataAsync(memoryStream, cancel).ConfigureAwait(false);
            return BitConverter.ToString(hashedBytes).Replace("-", "");
        }
    }
}
