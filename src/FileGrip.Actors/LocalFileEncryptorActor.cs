using Akka.Actor;
using System;
using System.IO;
using System.Security.Cryptography;

namespace FileGrip.Actors
{
    public class LocalFileEncryptorActor : ReceiveActor
    {
        private readonly string _authorizedWorkingDirectory;
        private readonly string _outputDirectory;

        public LocalFileEncryptorActor(string authorizedWorkingDirectory, string outputDirectory)
        {
            if (authorizedWorkingDirectory is null)
            {
                throw new ArgumentNullException(nameof(authorizedWorkingDirectory));
            }

            _authorizedWorkingDirectory = Path.GetFullPath(authorizedWorkingDirectory + Path.DirectorySeparatorChar);
            if (!Directory.Exists(_authorizedWorkingDirectory))
            {
                throw new ArgumentException($"{nameof(authorizedWorkingDirectory)} should point to an existing directory.");
            }
            _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
            if (!Directory.Exists(outputDirectory))
            {
                throw new ArgumentException($"{nameof(outputDirectory)} should point to an existing directory.");
            }

            Receive<Encrypt>(Handle);

            Console.WriteLine($"{nameof(LocalFileEncryptorActor)} started!");
        }

        private void Handle(Encrypt request)
        {
            Console.WriteLine($"Message received for file {request.RelativeFilePath}.");
            var absoluteFilePath = Path.GetFullPath(Path.Combine(_authorizedWorkingDirectory, request.RelativeFilePath));
            if (!absoluteFilePath.StartsWith(_authorizedWorkingDirectory, StringComparison.Ordinal))
            {
                Sender.Tell(new OutsideOfWorkingDirectory(request.RelativeFilePath));
                return;
            }
            else if (!File.Exists(absoluteFilePath))
            {
                Sender.Tell(new FileDoesNotExist(request.RelativeFilePath));
                return;
            }

            using var aes = Aes.Create();
            aes.Key = request.Key;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var outputFileStream = File.Create(Path.Combine(_outputDirectory, Path.GetFileName(absoluteFilePath)));
            using var cryptoStream = new CryptoStream(outputFileStream, encryptor, CryptoStreamMode.Write);
            using var streamWriter = new BinaryWriter(cryptoStream);
            using var inputFileStream = File.Open(absoluteFilePath, FileMode.Open, FileAccess.Read, FileShare.None);

            var fileLength = inputFileStream.Length;
            var buffer = new byte[1024];
            while (true)
            {
                var lengthRead = inputFileStream.Read(buffer, 0, buffer.Length);
                if (lengthRead == 0)
                {
                    break;
                }
                streamWriter.Write(buffer.AsSpan(0, lengthRead));
            }

            Sender.Tell(new Success(aes.IV));
            Console.WriteLine($"Done encrypting {request.RelativeFilePath}.");

            return;
        }

        public class Encrypt
        {
            public Encrypt(string filePath, byte[] key)
            {
                RelativeFilePath = filePath;
                Key = key;
            }

            public string RelativeFilePath { get; }

            public byte[] Key { get; }
        }

        public class Success
        {
            public Success(byte[] iV)
            {
                IV = iV;
            }

            public byte[] IV { get; }
        }

        public class FileDoesNotExist
        {
            public string FilePath { get; }

            public FileDoesNotExist(string filePath)
            {
                FilePath = filePath;
            }
        }

        public class OutsideOfWorkingDirectory
        {
            public string FilePath { get; }

            public OutsideOfWorkingDirectory(string filePath)
            {
                FilePath = filePath;
            }
        }
    }
}
