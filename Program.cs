using System;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureCardReader
{
    class Program
    {
        static string subscriptionKey = Environment.GetEnvironmentVariable("COMPUTER_VISION_SUBSCRIPTION_KEY");
        static string endpoint = Environment.GetEnvironmentVariable("COMPUTER_VISION_ENDPOINT");


        //image URL and filename desired are required arguments  
        static void Main(string[] args)
        {
            Console.WriteLine(args[0]);
            if (args[0] != null && args[1] != null)
            {
                string EXTRACT_TEXT_URL_IMAGE = args[0];
                string write_to_file = args[1];
                ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);

                // Read the batch text from an image (handwriting and/or printed).
                BatchReadFileUrl(client, EXTRACT_TEXT_URL_IMAGE, write_to_file).Wait();
            }
        }

        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }

        /*
 * BATCH READ FILE - URL IMAGE
 * Recognizes handwritten text. 
 * This API call offers an improvement of results over the Recognize Text calls.
 */
        public static async Task BatchReadFileUrl(ComputerVisionClient client, string urlImage, string filename)
        {
            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine("BATCH READ FILE - URL IMAGE");
            Console.WriteLine();

            // Read text from URL
            BatchReadFileHeaders textHeaders = await client.BatchReadFileAsync(urlImage);
            // After the request, get the operation location (operation ID)
            string operationLocation = textHeaders.OperationLocation;
            // Retrieve the URI where the recognized text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            // Extract the text
            // Delay is between iterations and tries a maximum of 10 times.
            int i = 0;
            int maxRetries = 10;
            ReadOperationResult results;
            Console.WriteLine($"Extracting text from URL image {Path.GetFileName(urlImage)}...");
            Console.WriteLine();
            do
            {
                results = await client.GetReadOperationResultAsync(operationId);
                Console.WriteLine("Server status: {0}, waiting {1} seconds...", results.Status, i);
                await Task.Delay(1000);
                if (i == 9)
                {
                    Console.WriteLine("Server timed out.");
                }
            }
            while ((results.Status == TextOperationStatusCodes.Running ||
                results.Status == TextOperationStatusCodes.NotStarted) && i++ < maxRetries);
            // Display the found text, and write to given filename.
            Console.WriteLine();
            var textRecognitionLocalFileResults = results.RecognitionResults;
            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(AppDomain.CurrentDomain.BaseDirectory + filename))
            {
                foreach (TextRecognitionResult recResult in textRecognitionLocalFileResults)
                {
                    foreach (Line line in recResult.Lines)
                    {
                        Console.WriteLine(line.Text);
                        file.WriteLine(line.Text);
                    }
                }
            }
            Console.WriteLine();
        }
    }
}
