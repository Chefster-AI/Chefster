using System.Text;
using System.Text.Json;
using Chefster.Common;
using Chefster.Models;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Chefster.Services;

public class GordonService(IHttpClientFactory httpClientFactory)
{
    public readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    public readonly string API_KEY = Environment.GetEnvironmentVariable("API_KEY")!;
    public readonly string ASSIST_ID = Environment.GetEnvironmentVariable("ASSIST_ID")!;

    /*
        Handles the communication with OpenAI and our assistant, Gordon
        Order of operations:
        1. Create a thread
        2. Create a message (what considerations the user has)
        3. Create a run for that message
        4. Retreive the run after completion
    */

    private async Task<string?> CreateThread()
    {
        var httpClient = SetupHttpClient();

        var thread = await httpClient.PostAsync($"https://api.openai.com/v1/threads", null);

        // grab content, parse, and return the run and thread ids
        var content = await thread.Content.ReadAsStringAsync();
        try
        {
            var json = JsonDocument.Parse(content).RootElement;
            var threadId = json.GetProperty("id").GetString();
            return threadId;
        }
        catch (Exception ex)
        {
            // should log this type of stuff
            Console.WriteLine($"Failure to obtain thread Id. Error: {ex}");
            return null;
        }
    }

    private async Task<bool> CreateMessage(string threadId, string considerations)
    {
        var httpClient = SetupHttpClient();

        //create request body
        var jsonBody = new { role = "user", content = considerations };
        var strContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(jsonBody),
            Encoding.UTF8,
            "application/json"
        );

        var message = await httpClient.PostAsync(
            $"https://api.openai.com/v1/threads/{threadId}/messages",
            strContent
        );

        // make sure we get a successful call before continuing
        try
        {
            message.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failure to create message. Error: {ex}");
            return false;
        }
    }

    public async Task<string?> CreateRun(string threadId)
    {
        var httpClient = SetupHttpClient();

        var jsonBody = new { assistant_id = ASSIST_ID };
        var strContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(jsonBody),
            Encoding.UTF8,
            "application/json"
        );

        var run = await httpClient.PostAsync(
            $"https://api.openai.com/v1/threads/{threadId}/runs",
            strContent
        );

        // grab content, parse, and return the run id
        var content = await run.Content.ReadAsStringAsync();
        try
        {
            var json = JsonDocument.Parse(content).RootElement;
            var runId = json.GetProperty("id").GetString();
            return runId;
        }
        catch (Exception ex)
        {
            // should log this type of stuff
            Console.WriteLine($"Failure to obtain run Id. Error: {ex}");
            return null;
        }
    }

    //will return a gordonResponseModel eventually
    public async Task<ServiceResult<GordonResponseModel>> GetMessageResponse(string considerations)
    {
        var httpClient = SetupHttpClient();

        // create a thread, message, and run
        // Make sure all goes well
        var threadId = await CreateThread();
        if (threadId == null)
        {
            return ServiceResult<GordonResponseModel>.ErrorResult(
                "Failed to create threadId. threadId was null"
            );
        }

        var message = await CreateMessage(threadId!, considerations);
        if (!message)
        {
            return ServiceResult<GordonResponseModel>.ErrorResult("Failed to create message");
        }

        var runId = await CreateRun(threadId);
        if (runId == null)
        {
            return ServiceResult<GordonResponseModel>.ErrorResult(
                "Failed to create run. runId was null"
            );
        }

        // loop until we get a response back that contains Gordons Response
        var attempts = 0;
        do
        {
            attempts += 1;

            var successCheck = await httpClient.GetAsync(
                $"https://api.openai.com/v1/threads/{threadId}/runs/{runId}"
            );

            var successCheckContent = await successCheck.Content.ReadAsStringAsync();
            var status = JsonDocument
                .Parse(successCheckContent)
                .RootElement.GetProperty("status")
                .GetString();

            // make sure we catch status's that would result in a bad response
            if (
                status == "requires_action"
                || status == "cancelling"
                || status == "cancelled"
                || status == "failed"
                || status == "incomplete"
                || status == "expired"
            )
            {
                return ServiceResult<GordonResponseModel>.ErrorResult(
                    $"Run loop object had the status code: {status}. Exiting"
                );
            }

            // if all is well exit the loop
            if (status == "completed")
            {
                break;
            }

            // wait a few seconds before trying again
            await Task.Delay(5000);
        } while (attempts != Constants.MAX_ATTEMPTS);

        if (attempts == Constants.MAX_ATTEMPTS)
        {
            Console.WriteLine("Reached the end with no result...");
            return ServiceResult<GordonResponseModel>.ErrorResult(
                $"Run loop has reached max iterations for response. Exiting"
            );
        }

        // try to grab the response
        var response = await httpClient.GetAsync(
            $"https://api.openai.com/v1/threads/{threadId}/messages"
        );

        var content = await response.Content.ReadAsStringAsync();

        var json = JsonConvert.DeserializeObject<JObject>(content);

        var jsonString = json?["data"]?[0]?["content"]?[0]?["text"]?["value"]?.ToString();

        if (jsonString == null)
        {
            return ServiceResult<GordonResponseModel>.ErrorResult(
                "Json Response was invalid and returned null when retreiving Gordon response"
            );
        }

        try
        {
            var gordonResponse = JsonConvert.DeserializeObject<GordonResponseModel>(jsonString);
            return ServiceResult<GordonResponseModel>.SuccessResult(gordonResponse!);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to Deserialize json to GordonResponseModel. Error: {ex}");
            return ServiceResult<GordonResponseModel>.ErrorResult(
                $"Failed to retrieve Gordon response. Error: {ex}"
            );
        }
    }

    private HttpClient SetupHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient();

        httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
        httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Bearer {API_KEY}");

        return httpClient;
    }
}
