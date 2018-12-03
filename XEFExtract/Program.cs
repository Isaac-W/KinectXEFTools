using System;
using System.IO;
using System.Linq;

namespace XEFExtract
{
    class Program
    {
        static bool videoFlag = false;
        static bool skeletonFlag = false;
        static bool depthFlag = false;
        static bool resumeFlag = false;
        static bool stdinFlag = false;

        static void ParseDirectory(DirectoryInfo di)
        {
            Console.WriteLine("Parsing Directory {0}", di.Name);

            var files = from f in di.EnumerateFiles()
                        where f.Extension == ".xef"
                        select f;

            foreach (FileInfo f in files)
            {
                Console.WriteLine("Processing {0}", f.Name);

                // Convert the file
                using (XEFDataConverter xdc = new XEFDataConverter()
                {
                    UseVideo = videoFlag,
                    UseSkeleton = skeletonFlag,
                    UseDepth = depthFlag,
                    ResumeConversion = resumeFlag
                })
                {
                    xdc.ConvertFile(f.FullName);
                }

                Console.WriteLine("Done.");
                Console.WriteLine();
            }

            foreach (DirectoryInfo d in di.EnumerateDirectories())
            {
                ParseDirectory(d);
            }
        }

        static void WriteUsage()
        {
            Console.WriteLine("Usage:\txefextract [-v] [-s] [-d] [-resume] path/file");
            Console.WriteLine("\t-v : Output video file");
            Console.WriteLine("\t-s : Output skeleton file");
            Console.WriteLine("\t-d : Output depth data");
            Console.WriteLine();
            Console.WriteLine("\t-resume : Resume extraction (skips existing files)");
            Console.WriteLine();
            Console.WriteLine("\t-i : Read from stdin (still requires path for output)");
            Console.WriteLine();
            Console.WriteLine("\tpath/file : Directory or File to convert");

        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No input file/directory provided.");
                WriteUsage();
                return;
            }
            if (args.Length > 1)
            {
                //set flags to false
                videoFlag = skeletonFlag = false;
                for (int i = 0; i < args.Length - 1; i++)
                { //Last arg is directory
                    switch (args[i])
                    {
                        case "-v":
                            videoFlag = true;
                            break;
                        case "-s":
                            skeletonFlag = true;
                            break;
                        case "-d":
                            depthFlag = true;
                            break;
                        case "-resume":
                            resumeFlag = true;
                            break;
                        case "-i":
                            stdinFlag = true;
                            break;
                        default:
                            Console.WriteLine("Illegal argument: " + args[i]);
                            WriteUsage();
                            return;
                    }
                }
            }

            // Check if no output set
            if (!(videoFlag || skeletonFlag || depthFlag))
            {
                // By default, set everything to true
                videoFlag = true;
                skeletonFlag = true;
                depthFlag = true;
            }

            var path = args[args.Length - 1];
            if (path.StartsWith("-"))
            {
                Console.WriteLine("No input file/directory provided.");
                WriteUsage();
            }
            else if (stdinFlag)
            {
                using (XEFDataConverter xdc = new XEFDataConverter()
                {
                    UseVideo = videoFlag,
                    UseSkeleton = skeletonFlag,
                    UseDepth = depthFlag,
                    ResumeConversion = resumeFlag
                })
                {
                    using (Stream stdin = Console.OpenStandardInput())
                    {
                        xdc.ConvertFile(path, stdin);
                    }
                }
            }
            else if (Directory.Exists(@path))
            {
                DirectoryInfo di = new DirectoryInfo(path);
                ParseDirectory(di);
            }
            else if (File.Exists(@path))
            {
                FileInfo file = new FileInfo(@path);
                if (file.Extension != ".xef")
                {
                    Console.WriteLine("File {0} is not a .xef file.", path);
                    return;
                }
                Console.WriteLine("Processing {0}", file.Name);

                // Convert the file
                using (XEFDataConverter xdc = new XEFDataConverter()
                {
                    UseVideo = videoFlag,
                    UseSkeleton = skeletonFlag,
                    UseDepth = depthFlag,
                    ResumeConversion = resumeFlag
                })
                {
                    xdc.ConvertFile(file.FullName);
                }

                Console.WriteLine("\nDone.");
            }
            else
            {
                Console.WriteLine("Input {0} Does not Exist", path);
            }
        }
    }
}
