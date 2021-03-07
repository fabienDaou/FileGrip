namespace FileGrip.Actors
{
    public class FileDoesNotExist
    {
        public string FilePath { get; }

        public FileDoesNotExist(string filePath)
        {
            FilePath = filePath;
        }
    }
}
