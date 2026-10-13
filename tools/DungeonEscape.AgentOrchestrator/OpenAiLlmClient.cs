#pragma warning disable OPENAI001
using System.ClientModel;
using OpenAI.Responses;

namespace DungeonEscape.AgentOrchestrator;

public sealed class OpenAiLlmClient : ILlmClient
{
    private readonly ResponsesClient client;

    public OpenAiLlmClient(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("OPENAI_API_KEY is required for the OpenAI provider.", nameof(apiKey));
        client = new(apiKey);
    }

    internal OpenAiLlmClient(ResponsesClient client) => this.client = client;

    public string ProviderName => "OpenAI";

    public ILlmConversation StartConversation(LlmConversationRequest request) =>
        new OpenAiConversation(client, request);

    private sealed class OpenAiConversation(ResponsesClient client, LlmConversationRequest request)
        : ILlmConversation
    {
        private readonly List<ResponseItem> inputItems = [ResponseItem.CreateUserMessageItem(request.Input)];

        public async Task<LlmResponse> RespondAsync(IReadOnlyList<LlmToolOutput> toolOutputs,
            CancellationToken cancellationToken)
        {
            foreach (var output in toolOutputs)
                inputItems.Add(ResponseItem.CreateFunctionCallOutputItem(output.CallId, output.Content));
            var options = new CreateResponseOptions(request.Model, inputItems)
            {
                Instructions = request.Instructions,
                ParallelToolCallsEnabled = false,
                StoredOutputEnabled = false,
                TextOptions = new ResponseTextOptions
                {
                    TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(request.OutputSchemaName,
                        BinaryData.FromString(request.OutputSchema.GetRawText()), jsonSchemaIsStrict: true)
                }
            };
            foreach (var tool in request.Tools)
                options.Tools.Add(ResponseTool.CreateFunctionTool(functionName: tool.Name,
                    functionDescription: tool.Description,
                    functionParameters: BinaryData.FromString(tool.InputSchema.GetRawText()),
                    strictModeEnabled: false));
            try
            {
                ResponseResult response = await client.CreateResponseAsync(options, cancellationToken);
                if (response.Error is not null || response.IncompleteStatusDetails is not null)
                    throw new LlmProviderException(LlmProviderErrorKind.InvalidResponse,
                        "OpenAI returned an incomplete response.");
                inputItems.AddRange(response.OutputItems);
                var calls = response.OutputItems.OfType<FunctionCallResponseItem>()
                    .Select(call => new LlmToolCall(call.CallId, call.FunctionName,
                        call.FunctionArguments.ToString())).ToArray();
                var refused = response.OutputItems.OfType<MessageResponseItem>()
                    .SelectMany(message => message.Content)
                    .Any(part => part.Kind == ResponseContentPartKind.Refusal);
                var usage = response.Usage is null ? null : new LlmUsage(response.Usage.InputTokenCount,
                    response.Usage.OutputTokenCount, response.Usage.TotalTokenCount);
                return new(response.Id, response.GetOutputText(), calls, refused, usage);
            }
            catch (ClientResultException exception)
            {
                throw new LlmProviderException(Classify(exception.Status),
                    "OpenAI request failed.", exception);
            }
            catch (HttpRequestException exception)
            {
                throw new LlmProviderException(LlmProviderErrorKind.Network,
                    "OpenAI network request failed.", exception);
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private static LlmProviderErrorKind Classify(int status) => status switch
        {
            401 or 403 => LlmProviderErrorKind.Authentication,
            429 => LlmProviderErrorKind.RateLimit,
            >= 500 => LlmProviderErrorKind.Server,
            _ => LlmProviderErrorKind.InvalidResponse
        };
    }
}
#pragma warning restore OPENAI001
