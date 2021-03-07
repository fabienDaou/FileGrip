using LanguageExt;
using System;
using System.IO;

namespace FileGrip.Actors
{
    public static class StringExtensions
    {
        public static Either<object, string> ValidateFilePath(this string filePath, string parentDirectory)
        {
            var absoluteFilePath = Path.GetFullPath(Path.Combine(parentDirectory, filePath));
            if (!absoluteFilePath.StartsWith(parentDirectory, StringComparison.Ordinal))
            {
                return new OutsideOfWorkingDirectory(filePath);
            }
            else if (!File.Exists(absoluteFilePath))
            {
                return new FileDoesNotExist(filePath);
            }

            return absoluteFilePath;
        }
    }
}
