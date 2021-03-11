using Akka.Actor;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace FileGrip.Actors
{
    public class LocalFileDecryptorActor : ReceiveActor
    {
        private readonly string _authorizedWorkingDirectory;
        private readonly string _outputDirectory;

        public LocalFileDecryptorActor(string authorizedWorkingDirectory, string outputDirectory)
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

            Receive<Decrypt>(Handle);

            Log($"{nameof(LocalFileDecryptorActor)} started!");
        }

        private void Handle(Decrypt request)
        {
            Log($"Message received for file {request.RelativeFilePath}.");

            request.RelativeFilePath.ValidateFilePath(_authorizedWorkingDirectory)
                .Match(filePath => DecryptFile(filePath, request.Key, request.IV), error => Sender.Tell(error));
        }

        private void DecryptFile(string filePath, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            using (var outputFileStream = File.Create(Path.Combine(_outputDirectory, Path.GetFileName(filePath))))
            {
                using var cryptoStream = new CryptoStream(outputFileStream, decryptor, CryptoStreamMode.Write);
                using var streamWriter = new BinaryWriter(cryptoStream);
                using (var inputFileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
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
                }
            }

            stopWatch.Stop();

            Log($"Done decrypting {filePath} in {stopWatch.Elapsed.TotalSeconds} seconds.");

            Sender.Tell(new Success());
        }

        private static void Log(string message) => Console.WriteLine($"[{nameof(LocalFileDecryptorActor)}] {message}");

        public class Decrypt
        {
            public Decrypt(string filePath, byte[] key, byte[] iv)
            {
                RelativeFilePath = filePath;
                Key = key;
                IV = iv;
            }

            public string RelativeFilePath { get; }

            public byte[] Key { get; }
            public byte[] IV { get; }
        }

        public class Success { }
    }
}
