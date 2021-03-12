using Akka.Actor;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FileGrip.Actors
{
    public class LocalFileDecryptorActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly string _authorizedWorkingDirectory;
        private readonly string _outputDirectory;

        private IActorRef _sender;

        public IStash Stash { get; set; }

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

            Become(Ready);

            Log($"{nameof(LocalFileDecryptorActor)} started!");
        }

        private void Ready()
        {
            Receive<Decrypt>(Handle);
        }

        private void Busy()
        {
            Receive<Success>(Handle);
            Receive<Failure>(Handle);
            Receive<Decrypt>(AddToStash);
        }

        private void AddToStash(Decrypt decrypt) => Stash.Stash();

        private void Handle(Success success)
        {
            Become(Ready);
            Stash.Unstash();
            _sender.Tell(success);
        }

        private void Handle(Failure failure)
        {
            Become(Ready);
            Stash.Unstash();
            _sender.Tell(failure);
        }

        private void Handle(Decrypt request)
        {
            Log($"Message received for file {request.RelativeFilePath}.");

            _sender = Sender;

            request.RelativeFilePath.ValidateFilePath(_authorizedWorkingDirectory)
                .Match(
                    filePath => DecryptFile(filePath, request.RelativeFilePath, request.Key, request.IV)
                        .PipeTo(Self, failure: ex => new Failure(request.RelativeFilePath)),
                    error => Sender.Tell(error));

            Become(Busy);
        }

        private Task DecryptFile(string filePath, string relativeFilePath, byte[] key, byte[] iv)
        {
            var self = Self;
            return Task.Run(() =>
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

                self.Tell(new Success(relativeFilePath));
            });
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

        public class Success 
        {
            public Success(string filePath)
            {
                FilePath = filePath;
            }

            public string FilePath { get; }
        }

        public class Failure 
        {
            public Failure(string filePath)
            {
                FilePath = filePath;
            }

            public string FilePath { get; }
        }
    }
}
