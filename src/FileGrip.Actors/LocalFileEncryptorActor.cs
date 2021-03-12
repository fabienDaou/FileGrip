using Akka.Actor;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FileGrip.Actors
{
    public class LocalFileEncryptorActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly string _authorizedWorkingDirectory;
        private readonly string _outputDirectory;

        private IActorRef _sender;

        public IStash Stash { get; set; }

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

            Become(Ready);

            Log($"{nameof(LocalFileEncryptorActor)} started!");
        }

        private void Ready()
        {
            Receive<Encrypt>(Handle);
        }

        private void Busy()
        {
            Receive<Success>(Handle);
            Receive<Failure>(Handle);
            Receive<Encrypt>(AddToStash);
        }

        private void AddToStash(Encrypt encrypt) => Stash.Stash();

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

        private void Handle(Encrypt request)
        {
            Log($"Message received for file {request.RelativeFilePath}.");

            _sender = Sender;

            request.RelativeFilePath.ValidateFilePath(_authorizedWorkingDirectory)
                .Match(
                    absoluteFilePath => EncryptFile(absoluteFilePath, request.RelativeFilePath, request.Key)
                        .PipeTo(Self, failure: ex => new Failure(request.RelativeFilePath)),
                    error => Sender.Tell(error));

            Become(Busy);
        }

        private Task EncryptFile(string absoluteFilePath, string relativeFilePath, byte[] key)
        {
            var self = Self;
            return Task.Run(() =>
            {
                using var aes = Aes.Create();
                aes.Key = key;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                var stopWatch = new Stopwatch();
                stopWatch.Start();

                using (var outputFileStream = File.Create(Path.Combine(_outputDirectory, Path.GetFileName(absoluteFilePath))))
                {
                    using var cryptoStream = new CryptoStream(outputFileStream, encryptor, CryptoStreamMode.Write);
                    using var streamWriter = new BinaryWriter(cryptoStream);
                    using (var inputFileStream = File.Open(absoluteFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
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

                Log($"Done encrypting {absoluteFilePath} in {stopWatch.Elapsed.TotalSeconds} seconds.");

                self.Tell(new Success(relativeFilePath, aes.IV));
            });
        }

        private static void Log(string message) => Console.WriteLine($"[{nameof(LocalFileEncryptorActor)}] {message}");

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
            public Success(string filePath, byte[] iV)
            {
                FilePath = filePath;
                IV = iV;
            }

            public string FilePath { get; }
            public byte[] IV { get; }
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
