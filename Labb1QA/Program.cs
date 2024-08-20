using Azure;
using Azure.AI.Language.QuestionAnswering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;
using Azure.AI.TextAnalytics;


namespace Labb1QA
{
    internal class Program
    {
        private static string translatorEndpoint = "https://api.cognitive.microsofttranslator.com";
        private static string cogSvcKey;
        private static string cogSvcRegion;
        private static string cogQnaQendpoint;
        private static string cogQnaKey;
        private static string cogProjName;
        private static string cogDepName;
        private static string textAnalyticsEndpoint;
        private static string textAnalyticsKey;
        private static string translatorKey;
        static async Task Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            //endpoints for configuration
            cogSvcKey = configuration["CognitiveServiceKey"];
            cogSvcRegion = configuration["CognitiveServiceRegion"];
            cogQnaQendpoint = configuration["QnaEndpoint"];
            cogQnaKey = configuration["QnaKey"];
            cogProjName = configuration["ProjectName"];
            cogDepName = configuration["DeployName"];
            textAnalyticsEndpoint = configuration["TextAnalyticsEndpoint"];
            textAnalyticsKey = configuration["TextAnalyticsKey"];
            translatorEndpoint = configuration["TranslatorEndpoint"];
            translatorKey = configuration["TranslatorKey"];

            TextAnalyticsClient textAnalyticsClient = new TextAnalyticsClient(new Uri(textAnalyticsEndpoint), new AzureKeyCredential(textAnalyticsKey));
            //HttpClient httpClient = new HttpClient();
            Console.WriteLine("Hello, and welcome to our QNA AI robot!");
            using (var client = new HttpClient()) {
                while (true)
                {


                    Console.WriteLine("Our FAQ options are: \n" +
                    "(1): How do i reset my password?\n(2): How can I update my billing information?\n(3): What is the delivery time for my order?\n(4): How can I cancel my order?\n" +
                    "(5): What payment methods do you accept?\n(6): If you have any other question\n(7) Exit program\nSelect any choise by pressing that number.");
                    int selection = Convert.ToInt32(Console.ReadLine());

                    string question = "";
                    switch (selection)
                    {
                        case 1:
                            question = "How do i reset my password?";
                            break;
                        case 2:
                            question = "How can I update my billing information?";
                            break;
                        case 3:
                            question = "What is the delivery time for my order?";
                            break;
                        case 4:
                            question = "How can I cancel my order?";
                            break;
                        case 5:
                            question = "What payment methods do you accept?";
                            break;
                        case 6:
                            Console.WriteLine("Please type your question:");
                            question = Console.ReadLine();
                            break;
                        case 7:
                            Environment.Exit(0);
                            break;
                        default:
                            Console.WriteLine("Sorry invalid selection");
                            break;
                    }
                    string detectedLanguage = DetectLanguage(textAnalyticsClient, question);
                    Console.WriteLine($"Detected Language: {detectedLanguage}");

                    if (detectedLanguage != "English")
                    {
                        question = await TranslateTextToEnglish(client, question);
                        Console.WriteLine($"Translated Question: {question}");
                    }
                    try
                    {
                        QuestionAnsweringClient qnaclient = new QuestionAnsweringClient(new Uri(cogQnaQendpoint), new AzureKeyCredential(cogQnaKey));
                        QuestionAnsweringProject project = new QuestionAnsweringProject(cogProjName, cogDepName);

                        Response<AnswersResult> response = qnaclient.GetAnswers(question, project);

                        foreach (KnowledgeBaseAnswer answer in response.Value.Answers)
                        {
                            Console.WriteLine($"Q: {question}");
                            Console.WriteLine($"A: {answer.Answer}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }
        private static string DetectLanguage(TextAnalyticsClient client, string text)
        {
            DetectedLanguage detectedLanguage = client.DetectLanguage(text);
            return detectedLanguage.Name;
        }
        private static async Task<string> TranslateTextToEnglish(HttpClient client, string text)
        {
            // Creates URI for translating text
            string route = "/translate?api-version=3.0&to=en";
            string url = translatorEndpoint + route;

           
            object[] body = new object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(url);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", translatorKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", "westeurope");

                HttpResponseMessage response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<dynamic>(responseBody);
                return result[0].translations[0].text;
            }
        }

    }
}
