namespace PKHeX.Rest.Models
{
    public class FileModel
    {
        public string FileName { get; set; }
        public int FileSize => FileData?.Length ?? 0;
        public string FileHash { get; set; }
        public byte[] FileData { get; set; }
    }
}
