namespace FileGrip.Actors
{
    public class OutsideOfWorkingDirectory
    {
        public string FilePath { get; }

        public OutsideOfWorkingDirectory(string filePath)
        {
            FilePath = filePath;
        }
    }
}
