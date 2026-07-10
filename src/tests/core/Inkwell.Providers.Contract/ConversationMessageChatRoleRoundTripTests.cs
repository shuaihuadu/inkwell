using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testcontainers.PostgreSql;
using Inkwell;
using Inkwell.Persistence.EFCore;
using Inkwell.Persistence.EFCore.Postgres.DependencyInjection;

namespace Inkwell.Providers.Contract;

/// <summary>
/// 验证 <c>AgentConversationMessageMappingExtensions.SelectAsModel</c> 中 <c>new ChatRole(entity.Role)</c>
/// 这种在服务端翻译的 <see cref="IQueryable{T}"/> 投影里包装标量列值的写法，在真实 PostgreSQL 上能否被
/// EF Core 正确翻译执行。
/// </summary>
[TestClass]
public sealed class ConversationMessageChatRoleRoundTripTests
{
    private static PostgreSqlContainer? s_container;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext _)
    {
        s_container = new PostgreSqlBuilder("postgres:17-alpine").Build();

        await s_container.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        if (s_container is not null)
        {
            await s_container.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task ListMessagesByConversation_Roundtrips_ChatRole_Through_SelectAsModel_Projection()
    {
        ServiceProvider provider = BuildServiceProvider();

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<InkwellDbContext>().Database.EnsureCreatedAsync();
        }

        Guid agentId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        Guid conversationId = Guid.CreateVersion7();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            IAgentRepository agents = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
            IAgentConversationRepository conversations = scope.ServiceProvider.GetRequiredService<IAgentConversationRepository>();

            await agents.AddAgent(new AgentDefinition
            {
                Id = agentId,
                OwnerUserId = ownerUserId,
                Name = "chatrole-roundtrip-agent",
                CreatedTime = now,
                UpdatedTime = now,
            });

            await conversations.AddConversation(new AgentConversation
            {
                Id = conversationId,
                AgentId = agentId,
                OwnerUserId = ownerUserId,
                Title = null,
                CreatedTime = now,
                UpdatedTime = now,
            });
        }

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            IAgentConversationMessageRepository messages = scope.ServiceProvider.GetRequiredService<IAgentConversationMessageRepository>();

            await messages.AddMessage(new AgentConversationMessage
            {
                Id = Guid.CreateVersion7(),
                ConversationId = conversationId,
                Role = ChatRole.User,
                ContentJson = "[]",
                SequenceNumber = 0,
                CreatedTime = now,
                UpdatedTime = now,
            });

            await messages.AddMessage(new AgentConversationMessage
            {
                Id = Guid.CreateVersion7(),
                ConversationId = conversationId,
                Role = ChatRole.Assistant,
                ContentJson = "[]",
                SequenceNumber = 1,
                CreatedTime = now,
                UpdatedTime = now,
            });
        }

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            IAgentConversationMessageRepository messages = scope.ServiceProvider.GetRequiredService<IAgentConversationMessageRepository>();

            PagedResult<AgentConversationMessage> page = await messages.ListMessagesByConversation(
                conversationId,
                new Pagination(1, 10),
                new SortOrder(nameof(AgentConversationMessage.SequenceNumber), SortDirection.Ascending));

            Assert.AreEqual(2, page.Items.Count);
            Assert.AreEqual(ChatRole.User, page.Items[0].Role);
            Assert.AreEqual(ChatRole.Assistant, page.Items[1].Role);
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        IInkwellBuilder builder = services.AddInkwell(new ConfigurationBuilder().Build());

        builder.UsePostgres(s_container!.GetConnectionString());

        return builder.Services.BuildServiceProvider();
    }
}
