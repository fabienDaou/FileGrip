using Akka.Actor;
using Akka.Routing;
using FileGrip.Actors;
using System.IO;
using System.Security.Cryptography;

namespace FileGrip.Cryptography.Console
{
    class Program
    {
        private const string _workspacePath = "C:\\Users\\Hycariss\\Documents\\Dev\\workspace";
        private const string _outputEncryptPath = "C:\\Users\\Hycariss\\Documents\\Dev\\output-encrypt";
        private const string _outputDecryptPath = "C:\\Users\\Hycariss\\Documents\\Dev\\output-decrypt";

        private static ActorSystem _actorSystem;
        static void Main(string[] args)
        {
            _actorSystem = ActorSystem.Create("ActorSystem");

            // todo
            // change output file name by a guid
            // get back IV and store it in a meta file {guid}.meta.json
            // { "iv": ..., "encryptedFilename": "..." }
            // handle encoding

            // Start a pool of encryptor actor to parallelize work.
            var encryptProps = Props.Create<LocalFileEncryptorActor>(_workspacePath, _outputEncryptPath).WithRouter(new RoundRobinPool(5));
            var decryptProps = Props.Create<LocalFileDecryptorActor>(_outputEncryptPath, _outputDecryptPath).WithRouter(new RoundRobinPool(3));
            var encryptRouterActorRef = _actorSystem.ActorOf(encryptProps, "encryptRouter");
            var decryptRouterActorRef = _actorSystem.ActorOf(decryptProps, "decryptRouter");

            var coordinatorProps = Props.Create<EncryptDecryptCoordinatorActor>(encryptRouterActorRef, decryptRouterActorRef);
            var coordinatorActorRef = _actorSystem.ActorOf(coordinatorProps, "coordinator");

            coordinatorActorRef.Tell(new EncryptDecryptCoordinatorActor.Process(_workspacePath, _outputEncryptPath, _outputDecryptPath));

            _actorSystem.WhenTerminated.Wait();
        }
    }

    public class EncryptDecryptCoordinatorActor : ReceiveActor
    {
        private readonly IActorRef _encryptRouterActor;
        private readonly IActorRef _decryptRouterActor;

        private byte[] _key;

        public EncryptDecryptCoordinatorActor(IActorRef encryptRouterActor, IActorRef decryptRouterActor)
        {
            _encryptRouterActor = encryptRouterActor;
            _decryptRouterActor = decryptRouterActor;
            Receive<Process>(Handle);
        }

        private void Handle(Process process)
        {
            Become(Busy);

            using var aes = Aes.Create();
            _key = aes.Key;
            Log($"Encrypting files with key '{string.Join(", ", _key)}'.");

            foreach (var absoluteFilePath in Directory.GetFiles(process.Workspace))
            {
                var relativeFilePath = Path.GetRelativePath(process.Workspace, absoluteFilePath);
                _encryptRouterActor.Tell(new LocalFileEncryptorActor.Encrypt(relativeFilePath, _key));
            }
        }

        private void Busy()
        {
            Receive<LocalFileEncryptorActor.Success>(Handle);
            Receive<LocalFileDecryptorActor.Success>(Handle);
            Receive<LocalFileEncryptorActor.Failure>(Handle);
            Receive<LocalFileDecryptorActor.Failure>(Handle);
            Receive<FileDoesNotExist>(Handle);
            Receive<OutsideOfWorkingDirectory>(Handle);
        }

        private void Handle(LocalFileEncryptorActor.Success success)
        {
            Log($"File successfully encrypted: {success.FilePath}");
            _decryptRouterActor.Tell(new LocalFileDecryptorActor.Decrypt(success.FilePath, _key, success.IV));
        }

        private void Handle(LocalFileDecryptorActor.Success success) => Log($"File successfully decrypted: {success.FilePath}");

        private void Handle(LocalFileDecryptorActor.Failure failure) => Log($"Failed to decrypt file: {failure.FilePath}");

        private void Handle(LocalFileEncryptorActor.Failure failure) => Log($"Failed to decrypte file: {failure.FilePath}");

        private void Handle(FileDoesNotExist fileDoesNotExist) => Log($"File does not exist: {fileDoesNotExist.FilePath}");

        private void Handle(OutsideOfWorkingDirectory outsideOfWorkingDirectory) 
            => Log($"File outside of working directory: {outsideOfWorkingDirectory.FilePath}");

        private static void Log(string message) => System.Console.WriteLine($"[{nameof(EncryptDecryptCoordinatorActor)}] {message}");

        public class Process 
        {
            public Process(string workspace, string encryptOutput, string decryptOutput)
            {
                Workspace = workspace;
                EncryptOutput = encryptOutput;
                DecryptOutput = decryptOutput;
            }

            public string Workspace { get; set; }
            public string EncryptOutput { get; set; }
            public string DecryptOutput { get; set; }
        }
    }
}
