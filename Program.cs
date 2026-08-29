using System;
using MimeDetective;
using System.IO;
using System.Linq;
using SharpFuzz;

using System.Text;

namespace Semgrep_v1
{


    /*public class FileValidator
    {
        // Build and cache the inspector for reuse
        private static readonly MimeDetective.IContentInspector Inspector = new ContentInspectorBuilder()
        {
            Definitions = MimeDetective.Definitions.DefaultDefinitions.All()
        }.Build();

        // Whitelist of safe MIME types allowed by your app
        private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "application/pdf" };

        public static bool IsValidFile(Stream fileStream)
        {
            if (fileStream == null || fileStream.Length == 0) return false;

            // Inspect the stream without destroying/moving its position permanently
            long originalPosition = fileStream.Position;
            var results = Inspector.Inspect(fileStream);
            fileStream.Position = originalPosition;

            // If no match found against known definitions
            if (results == null || !results.Any()) return false;

            // Grab the highest confidence match
            var bestMatch = results.First();

            Console.WriteLine($"Detected MIME: {bestMatch.MimeType} (Extension: {bestMatch.Extension})");

            // Validate against your allowed whitelist
            return AllowedMimeTypes.Contains(bestMatch.MimeType, StringComparer.OrdinalIgnoreCase);
        }
    }*/

    /*public class CustomBinaryParser
    {
        public static void ParsePayload(ReadOnlySpan<byte> data)
        {
            if (data.Length < 4) return;

            byte header = data[0];
            int lengthIndicator = data[1];

            if (header == 0x7F)
            {
                // Vulnerability: Assumes data array is large enough based on index 1 indicator
                byte criticalFlag = data[lengthIndicator + 2];

                if (criticalFlag == 0xFF)
                {
                    throw new InvalidOperationException("Critical system failure state triggered!");
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            // SharpFuzz hooks the execution path monitoring engine here
            Fuzzer.Run(stream =>
            {
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    byte[] buffer = ms.ToArray();

                    // Call our target parser method with the generated fuzz data
                    CustomBinaryParser.ParsePayload(buffer);
                }
            });
        }
    }*/

    

    
        // 1. THE VULNERABLE CODE (The Student Target)
        public class NetworkPacketParser
        {
            public static void ProcessPacket(byte[] payload)
            {
                if (payload == null || payload.Length == 0) return;

                // Packet structure: [Header Byte] [Data Length Byte] [Raw Data Bytes...]
                byte header = payload[0];

                if (header == 0x0A) // 0x0A means "Text Message Packet"
                {
                    int declaredLength = payload[1];

                    // CRITICAL DRAWBACK: The code trusts declaredLength without checking payload.Length!
                    // If declaredLength is 50, but payload only has 3 bytes, this will crash.
                    string message = Encoding.UTF8.GetString(payload, 2, declaredLength);

                    Console.WriteLine($"      [Parser Success] Read message: {message}");
                }
            }
        }

    // 2. THE FUZZ SIMULATOR & LOGGER
    class Program
    {
        static void Main(string[] args)
        {
            
            Console.WriteLine("=== STARTING AUTOMATED INPUT TESTING SIMULATION ===\n");

            // Define various bad inputs to test our function's resilience
            byte[][] testInputs = new byte[][]
            {
                new byte[] { 0x0A, 5, 65, 66, 67, 68, 69 }, // Input 1: Valid Packet ("ABCDE")
                new byte[] { 0x0A, 99 },                    // Input 2: Bad Length (Claims 99 bytes, only provides 2)
                null,                                       // Input 3: Null Input
                new byte[] { 0x99, 2, 11, 22 }              // Input 4: Unknown Header
            };

            int testCaseNumber = 1;

            foreach (var input in testInputs)
            {
                Console.WriteLine($"-------------------------------------------------- Built-in Test #{testCaseNumber}");
                Console.WriteLine($"[Input Data]: {DisplayBytes(input)}");

                try
                {
                    // Attempt to run the student's code with this input
                    NetworkPacketParser.ProcessPacket(input);
                    Console.WriteLine("[Test Result]: ✅ PASSED (Handled safely or processed correctly)");
                }
                catch (Exception ex)
                {
                    // RECORD LOGS TO DETERMINE THE DRAWBACKS
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Test Result]: ❌ CRASHED!");
                    Console.WriteLine($"[Exception Type]: {ex.GetType().Name}");
                    Console.WriteLine($"[Error Message] : {ex.Message}");
                    Console.ResetColor();

                    Console.WriteLine("\n💡 TEACHABLE MOMENT FOR STUDENTS:");
                    Console.WriteLine("   Drawback: The code trusted user input. It assumed the array was as big");
                    Console.WriteLine("             as the packet header claimed it was. Always validate bounds!");
                }

                testCaseNumber++;
                Console.WriteLine();
            }

            Console.WriteLine("=== SIMULATION COMPLETE ===");
            
            //RunDynamicFuzzerSimulation();
        }

        // Helper method to visually display byte arrays to students
        private static string DisplayBytes(byte[] bytes)
        {
            if (bytes == null) return "NULL";
            if (bytes.Length == 0) return "EMPTY ARRAY";
            return string.Join(", ", Array.ConvertAll(bytes, b => $"0x{b:X2}"));
        }


        static void RunDynamicFuzzerSimulation()
        {
            Console.WriteLine("=== STARTING GENERATION-BASED DYNAMIC FUZZER (10,000 Iterations) ===\n");

            Random rand = new Random();
            int crashCount = 0;
            int executionCount = 10000;

            for (int i = 1; i <= executionCount; i++)
            {
                // 1. Generate an input of a random size (0 to 10 bytes)
                int randomSize = rand.Next(0, 11);
                byte[] fuzzInput = new byte[randomSize];

                // 2. Fill the array with completely random byte values (0x00 to 0xFF)
                rand.NextBytes(fuzzInput);

                // Optional: Forcefully seed some 0x0A headers occasionally to test deeper logic branches
                if (rand.Next(0, 4) == 0 && fuzzInput.Length > 0)
                {
                    fuzzInput[0] = 0x0A;
                }

                try
                {
                    // 3. Pounding our original vulnerable parser with random bytes
                    NetworkPacketParser.ProcessPacket(fuzzInput);
                }
                catch (Exception ex)
                {
                    crashCount++;

                    // Log only the first few crashes to avoid overwhelming the classroom terminal screen
                    if (crashCount <= 3)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"💥 CRASH DETECTED at Iteration #{i}!");
                        Console.WriteLine($"   Input Mutated: {DisplayBytes(fuzzInput)}");
                        Console.WriteLine($"   Exception Type: {ex.GetType().Name} -> {ex.Message}\n");
                        Console.ResetColor();
                    }
                }
            }

            Console.WriteLine("==================================================");
            Console.WriteLine($"Fuzzing Completed. Total Tests: {executionCount:N0} | Total Application Crashes: {crashCount}");
            Console.WriteLine("==================================================");
        }

    }


    public class SecureNetworkPacketParser
    {
        public static void ProcessPacket(byte[] payload)
        {
            // 1. Guard against null or completely empty inputs
            if (payload == null || payload.Length < 2) return;

            byte header = payload[0];

            if (header == 0x0A)
            {
                int declaredLength = payload[1];

                // 2. CRITICAL SECURE FIX: Check if the array actually contains the claimed data bytes
                // Ensure index 2 exists, length is positive, and index 2 + declaredLength stays inside the array boundary
                if (declaredLength < 0 || (2 + declaredLength) > payload.Length)
                {
                    Console.WriteLine("      [Parser Blocked] Malformed packet: Declared length exceeds actual data bounds.");
                    return; // Gracefully drop the packet without throwing an unhandled exception
                }

                string message = Encoding.UTF8.GetString(payload, 2, declaredLength);
                Console.WriteLine($"      [Parser Success] Read message: {message}");
            }
        }
    }





}
