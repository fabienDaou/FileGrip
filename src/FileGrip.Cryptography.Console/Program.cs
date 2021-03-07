using Akka.Actor;
using Akka.Routing;
using FileGrip.Actors;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FileGrip.Cryptography.Console
{
    class Program
    {
        private const string workspacePath = "C:\\Users\\Hycariss\\Documents\\Dev\\workspace";
        private const string outputPath = "C:\\Users\\Hycariss\\Documents\\Dev\\output";
        private static ActorSystem actorSystem;
        static void Main(string[] args)
        {
            actorSystem = ActorSystem.Create("ActorSystem");

            // todo
            // change output file name by a guid
            // get back IV and store it in a meta file {guid}.meta.json
            // { "iv": ..., "encryptedFilename": "..." }

            // todo
            // create decrypt actor restoring content and filenames

            // Start a pool of encryptor actor to parallelize work.
            var props = Props.Create<LocalFileEncryptorActor>(workspacePath, outputPath).WithRouter(new RoundRobinPool(5));
            var actorRef = actorSystem.ActorOf(props, "router");

            using var aes = Aes.Create();
            System.Console.WriteLine($"Encrypting files with key '{string.Join(", ", aes.Key)}'");
            foreach (var file in Directory.GetFiles(workspacePath))
            {
                actorRef.Tell(new LocalFileEncryptorActor.Encrypt(file, aes.Key));
            }

            actorSystem.WhenTerminated.Wait();
        }
    }
}
