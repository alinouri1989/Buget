using System.Diagnostics;

namespace NuGetPackagePusher
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Specify the folder path containing .nupkg files  
            string folderPath = @"D:\OfflinePackages";

            // Specify the NuGet server URL and API key  
            string nugetServerUrl = "https://nuget.apnam.ir/v3/index.json";
            string apiKey = "ali_api_key";

            // Ensure the folder exists  
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"Error: The folder path '{folderPath}' does not exist.");
                return;
            }

            // Get all .nupkg files in the folder  
            string[] packageFiles = Directory.GetFiles(folderPath, "*.nupkg");

            if (packageFiles.Length == 0)
            {
                Console.WriteLine("No .nupkg files found in the specified folder.");
                return;
            }

            Console.WriteLine($"Found {packageFiles.Length} package(s) to push.");

            foreach (string packageFile in packageFiles)
            {
                try
                {
                    Console.WriteLine($"Pushing {packageFile}");

                    // Build the push command  
                    string command = $"dotnet nuget push \"{packageFile}\" -s {nugetServerUrl} -k {apiKey}";

                    // Execute the command  
                    ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = Process.Start(processInfo))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        process.WaitForExit();

                        // Output results  
                        if (process.ExitCode == 0)
                        {
                            Console.WriteLine($"Successfully pushed: {packageFile}");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to push: {packageFile}");
                            Console.WriteLine($"Error: {error}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception while pushing {packageFile}: {ex.Message}");
                }
            }

            Console.WriteLine("Done pushing all packages.");
        }
    }
}
