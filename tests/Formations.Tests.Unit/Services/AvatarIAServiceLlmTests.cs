using System.Net;
using System.Text;
using FluentAssertions;
using Formations.Application.Common.Interfaces;
using Formations.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formations.Tests.Unit.Services;

// DETTE-01 / BUG-03 : vérifie la construction des requêtes et le parsing des réponses
// pour chaque fournisseur LLM, sans appel réseau réel (HttpMessageHandler simulé).
public class AvatarIAServiceLlmTests
{
    // Handler qui capture la requête sortante et renvoie une réponse JSON fixe.
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _json;
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }
        public int CallCount { get; private set; }

        public StubHandler(string json) => _json = json;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CallCount++;
            LastRequest = request;
            if (request.Content is not null) LastBody = await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static IConfiguration Config(params (string Key, string Value)[] kv)
    {
        var dict = kv.ToDictionary(x => x.Key, x => (string?)x.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static AvatarIAService Build(StubHandler handler, IConfiguration config) =>
        new(new HttpClient(handler), config, NullLogger<AvatarIAService>.Instance);

    [Fact]
    public async Task GetAIResponse_WithoutApiKey_ReturnsMockWithoutHttpCall()
    {
        var handler = new StubHandler("{}");
        var svc = Build(handler, Config(("LLM:Provider", "claude"))); // pas de clé

        var reply = await svc.GetAIResponseAsync("Bonjour ?", "Module X", Array.Empty<ChatMessage>());

        reply.Should().Contain("simulée");
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAIResponse_Claude_PostsToMessagesEndpointWithApiKeyHeader()
    {
        var handler = new StubHandler("""{"content":[{"text":"Réponse Claude"}]}""");
        var svc = Build(handler, Config(
            ("LLM:Provider", "claude"), ("LLM:ApiKey", "sk-claude"), ("LLM:Model", "claude-x")));

        var reply = await svc.GetAIResponseAsync("Q ?", "Module X", Array.Empty<ChatMessage>());

        reply.Should().Be("Réponse Claude");
        handler.LastRequest!.RequestUri!.AbsoluteUri.Should().EndWith("/v1/messages");
        handler.LastRequest.Headers.GetValues("x-api-key").Should().ContainSingle().Which.Should().Be("sk-claude");
        handler.LastRequest.Headers.GetValues("anthropic-version").Should().ContainSingle();
        handler.LastBody.Should().Contain("\"model\":\"claude-x\"").And.Contain("\"system\"");
    }

    [Fact]
    public async Task GetAIResponse_OpenAi_PostsChatCompletionsWithBearer()
    {
        var handler = new StubHandler("""{"choices":[{"message":{"content":"Réponse OpenAI"}}]}""");
        var svc = Build(handler, Config(("LLM:Provider", "openai"), ("LLM:ApiKey", "sk-oai")));

        var reply = await svc.GetAIResponseAsync("Q ?", "Module", Array.Empty<ChatMessage>());

        reply.Should().Be("Réponse OpenAI");
        handler.LastRequest!.RequestUri!.AbsoluteUri.Should().Be("https://api.openai.com/v1/chat/completions");
        handler.LastRequest.Headers.Authorization!.ToString().Should().Be("Bearer sk-oai");
    }

    [Fact]
    public async Task GetAIResponse_Groq_UsesGroqBaseUrl()
    {
        var handler = new StubHandler("""{"choices":[{"message":{"content":"ok"}}]}""");
        var svc = Build(handler, Config(("LLM:Provider", "groq"), ("LLM:ApiKey", "gsk")));

        await svc.GetAIResponseAsync("Q ?", "Module", Array.Empty<ChatMessage>());

        handler.LastRequest!.RequestUri!.Host.Should().Be("api.groq.com");
    }

    [Fact]
    public async Task GetAIResponse_Google_HitsGenerateContentWithKeyQuery()
    {
        var handler = new StubHandler("""{"candidates":[{"content":{"parts":[{"text":"Réponse Gemini"}]}}]}""");
        var svc = Build(handler, Config(("LLM:Provider", "google"), ("LLM:ApiKey", "g-key")));

        var reply = await svc.GetAIResponseAsync("Q ?", "Module", Array.Empty<ChatMessage>());

        reply.Should().Be("Réponse Gemini");
        handler.LastRequest!.RequestUri!.AbsoluteUri.Should().Contain(":generateContent").And.Contain("key=g-key");
    }

    [Fact]
    public async Task GetAIResponse_UnknownProvider_Throws()
    {
        var svc = Build(new StubHandler("{}"), Config(("LLM:Provider", "banana"), ("LLM:ApiKey", "x")));

        var act = () => svc.GetAIResponseAsync("Q ?", "Module", Array.Empty<ChatMessage>());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
